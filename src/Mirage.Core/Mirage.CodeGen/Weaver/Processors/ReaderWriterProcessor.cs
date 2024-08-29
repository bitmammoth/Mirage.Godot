using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mirage.CodeGen.Mirage.CecilExtensions.Logging;
using Mirage.CodeGen.Weaver.Serialization;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Attributes;
using Mirage.Godot.Scripts.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.CodeGen.Weaver.Processors
{
    public class ReaderWriterProcessor
    {
        [Conditional("WEAVER_DEBUG_LOGS")]
        internal static void Log(string msg)
        {
            Console.Write($"[Weaver.ReaderWriterProcessor] {msg}\n");
        }

        private readonly HashSet<TypeReference> _messages = new HashSet<TypeReference>(new TypeReferenceComparer());

        private readonly IWeaverLogger _logger;
        private readonly ModuleDefinition _module;
        private readonly Readers _readers;
        private readonly Writers _writers;
        private readonly SerailizeExtensionHelper _extensionHelper;
        private readonly ModuleDefinition _mirageModule;

        /// <summary>
        /// Mirage's main module used to find built in extension methods and messages
        /// </summary>

        public ReaderWriterProcessor(ModuleDefinition module, Readers readers, Writers writers, IWeaverLogger logger)
        {
            this._module = module;
            this._readers = readers;
            this._writers = writers;
            this._logger = logger;
            _extensionHelper = new SerailizeExtensionHelper(module, readers, writers);

            var typeInMirage = module.ImportReference(typeof(NetworkWriter));

            // have to resolve to get typedef, then get the module
            var resolved = typeInMirage.Resolve() ?? throw new Exception("Could not find Mirage main module");
            _mirageModule = resolved.Module;
        }

        public bool Process()
        {
            _messages.Clear();

            var processed = FindAllExtensionMethods();

            // built in message must be done first,
            // otherwise other writers will try to create function for primitize types and fail
            LoadBuiltinMessages();

            // store how many writers are found, we need to check if currentModule adds any
            var writeCount = _writers.Count;
            var readCount = _readers.Count;
            ProcessModule();

            // we need to check if any methods are created from FindAllExtensionMethods or ProcessModule
            return true; // todo use force flag
            //return processed || writers.Count != writeCount || readers.Count != readCount;
        }

        /// <summary>
        /// Gets all extension methods in current assembly and all references
        /// </summary>
        private bool FindAllExtensionMethods()
        {
            var references = new List<AssemblyDefinition>();
            // load all references
            foreach (var reference in _module.AssemblyReferences)
            {
                var assembly = _module.AssemblyResolver.Resolve(reference);
                if (assembly == null)
                {
                    _logger.Warning($"Failed to resolve assembly reference: {reference}");
                    continue;
                }

                references.Add(assembly);
            }

            // check current module first, then check other modules
            // the order shouldn't matter because we just register function here we do not generate anything new

            var tracker = new CountTracker(this);

            FindExtensionMethods(_module.Assembly);
            // have any been added in the dll we are weaving?
            var processed = tracker.AnyNew();
            tracker.LogCount("Main Module");

            // we have to find extensions in mirage manually, it seems that for some versions of unity Mirage.dll isn't referenced by the
            FindExtensionMethods(_mirageModule);
            tracker.LogCount("Mirage");

            // process all references
            foreach (var assembly in references)
            {
                tracker.LogCount(assembly.Name.Name);
                FindExtensionMethods(assembly);
            }

            return processed;
        }

        private struct CountTracker
        {
            public int WriteCount;
            public int ReadCount;
            private readonly ReaderWriterProcessor _processor;

            public CountTracker(ReaderWriterProcessor processor) : this()
            {
                _processor = processor;
                ReadCount = processor._readers.Count;
                WriteCount = processor._writers.Count;
            }

            public readonly bool AnyNew()
            {
                return _processor._writers.Count != WriteCount || _processor._readers.Count != ReadCount;
            }
            public void LogCount(string label)
            {
                Log($"Functions in {label}: {_processor._writers.Count - WriteCount} writers, {_processor._readers.Count - ReadCount} readers");
                // store values again so we can log new count
                WriteCount = _processor._writers.Count;
                ReadCount = _processor._readers.Count;
            }
        }

        private void FindExtensionMethods(AssemblyDefinition assembly)
        {
            Log($"Looking for extension methods in {assembly.FullName}");
            foreach (var module in assembly.Modules)
            {
                // skip mirage for here, we process it manually
                if (module == _mirageModule)
                    continue;

                FindExtensionMethods(module);
            }
        }

        private void FindExtensionMethods(ModuleDefinition module)
        {
            foreach (var type in module.Types)
            {
                var resolved = type.Resolve();
                _extensionHelper.RegisterExtensionMethodsInType(resolved);
            }
        }

        /// <summary>
        /// Find NetworkMessage in Mirage.dll and ensure that they have serailize functions
        /// </summary>
        private void LoadBuiltinMessages()
        {
            var types = _mirageModule.GetTypes().Where(t => t.GetCustomAttribute<NetworkMessageAttribute>() != null);
            foreach (var type in types)
            {
                Log($"Loading built-in message: {type.FullName}");

                var typeReference = _module.ImportReference(type);
                // these can use the throw version, because if they break Mirage/weaver is broken
                _writers.GetFunction_Throws(typeReference);
                _readers.GetFunction_Throws(typeReference);
                _messages.Add(typeReference);
            }
        }

        private void ProcessModule()
        {
            // create copy incase we modify types
            var types = new List<TypeDefinition>(_module.Types);

            // find NetworkMessages
            foreach (var klass in types)
                CheckForNetworkMessage(klass);

            // Generate readers and writers
            // find all the Send<> and Register<> calls and generate
            // readers and writers for them.
            CodePass.ForEachInstruction(_module, GenerateReadersWriters);
        }

        private void CheckForNetworkMessage(TypeDefinition klass)
        {
            if (klass.HasCustomAttribute<NetworkMessageAttribute>())
            {
                Log($"Loading message: {klass.FullName}");
                _readers.TryGetFunction(klass, null);
                _writers.TryGetFunction(klass, null);
                _messages.Add(klass);
            }

            foreach (var nestedClass in klass.NestedTypes)
            {
                CheckForNetworkMessage(nestedClass);
            }
        }

        private Instruction GenerateReadersWriters(MethodDefinition _, Instruction instruction, SequencePoint sequencePoint)
        {
            if (instruction.OpCode == OpCodes.Ldsfld)
                GenerateReadersWriters((FieldReference)instruction.Operand, sequencePoint);

            // We are looking for calls to some specific types
            if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                GenerateReadersWriters((MethodReference)instruction.Operand, sequencePoint);

            return instruction;
        }

        private void GenerateReadersWriters(FieldReference field, SequencePoint sequencePoint)
        {
            var type = field.DeclaringType;

            if (type.Is(typeof(Writer<>)) || type.Is(typeof(Reader<>)) && type.IsGenericInstance)
            {
                var typeGenericInstance = (GenericInstanceType)type;

                var parameterType = typeGenericInstance.GenericArguments[0];

                GenerateReadersWriters(parameterType, sequencePoint);
            }
        }

        private void GenerateReadersWriters(MethodReference method, SequencePoint sequencePoint)
        {
            if (!method.IsGenericInstance)
                return;

            // generate methods for message or types used by generic read/write
            var isMessage = IsMessageMethod(method);

            var generate = isMessage ||
                IsReadWriteMethod(method);

            if (generate)
            {
                var instanceMethod = (GenericInstanceMethod)method;
                var parameterType = instanceMethod.GenericArguments[0];

                if (parameterType.IsGenericParameter)
                    return;

                GenerateReadersWriters(parameterType, sequencePoint);
                if (isMessage)
                    _messages.Add(parameterType);
            }
        }

        private void GenerateReadersWriters(TypeReference parameterType, SequencePoint sequencePoint)
        {
            if (!parameterType.IsGenericParameter && parameterType.CanBeResolved())
            {
                var typeDefinition = parameterType.Resolve();

                if (typeDefinition.IsClass && !typeDefinition.IsValueType)
                {
                    var constructor = typeDefinition.GetMethod(".ctor");

                    var hasAccess = constructor.IsPublic
                        || constructor.IsAssembly && typeDefinition.Module == _module;

                    if (!hasAccess)
                        return;
                }

                Log($"Generating Serialize for type used in generic: {parameterType.FullName}");
                _writers.TryGetFunction(parameterType, sequencePoint);
                _readers.TryGetFunction(parameterType, sequencePoint);
            }
        }

        /// <summary>
        /// is method used to send a message? if it use then T is a message and needs read/write functions
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private static bool IsMessageMethod(MethodReference method)
        {
            return
                method.Is(typeof(MessagePacker), nameof(MessagePacker.Pack)) ||
                method.Is(typeof(MessagePacker), nameof(MessagePacker.GetId)) ||
                method.Is(typeof(MessagePacker), nameof(MessagePacker.Unpack)) ||
                method.Is<IMessageSender>(nameof(IMessageSender.Send)) ||
                method.Is<IMessageReceiver>(nameof(IMessageReceiver.RegisterHandler)) ||
                method.Is<IMessageReceiver>(nameof(IMessageReceiver.UnregisterHandler)) ||
                method.Is<NetworkPlayer>(nameof(NetworkPlayer.Send)) ||
                method.Is<MessageHandler>(nameof(MessageHandler.RegisterHandler)) ||
                method.Is<MessageHandler>(nameof(MessageHandler.UnregisterHandler)) ||
                method.Is<NetworkClient>(nameof(NetworkClient.Send)) ||
                method.Is<NetworkServer>(nameof(NetworkServer.SendToAll)) ||
                method.Is<NetworkServer>(nameof(NetworkServer.SendToMany));
        }

        private static bool IsReadWriteMethod(MethodReference method)
        {
            return
                method.Is(typeof(GenericTypesSerializationExtensions), nameof(GenericTypesSerializationExtensions.Write)) ||
                method.Is(typeof(GenericTypesSerializationExtensions), nameof(GenericTypesSerializationExtensions.Read));
        }



        private static bool IsEditorAssembly(ModuleDefinition module)
        {
            return module.AssemblyReferences.Any(assemblyReference =>
                assemblyReference.Name == "Mirage.Editor"
                );
        }

        /// <summary>
        /// Creates a method that will store all the readers and writers into
        /// <see cref="Writer{T}.Write"/> and <see cref="Reader{T}.Read"/>
        ///
        /// The method will be marked InitializeOnLoadMethodAttribute so it gets
        /// executed before mirror runtime code
        /// </summary>
        /// <param name="currentAssembly"></param>
        public void InitializeReaderAndWriters()
        {
            var rwInitializer = _module.GeneratedClass().AddMethod(
                GeneratedCode.INIT_METHOD,
                Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static);

            var worker = rwInitializer.Body.GetILProcessor();

            _writers.InitializeWriters(worker);
            _readers.InitializeReaders(worker);

            RegisterMessages(worker);

            worker.Append(worker.Create(OpCodes.Ret));
        }

        private void RegisterMessages(ILProcessor worker)
        {
            var method = typeof(MessagePacker).GetMethod(nameof(MessagePacker.RegisterMessage));
            var registerMethod = _module.ImportReference(method);

            foreach (var message in _messages)
            {
                var genericMethodCall = new GenericInstanceMethod(registerMethod);
                genericMethodCall.GenericArguments.Add(_module.ImportReference(message));
                worker.Append(worker.Create(OpCodes.Call, genericMethodCall));
            }
        }
    }

    /// <summary>
    /// Helps get Extension methods using either reflection or cecil
    /// </summary>
    public class SerailizeExtensionHelper(ModuleDefinition module, Readers readers, Writers writers)
    {
        private readonly ModuleDefinition module = module;
        private readonly Readers readers = readers;
        private readonly Writers writers = writers;

        // todo can this be removed, doesn't seem to be used any more
        public void RegisterExtensionMethodsInType(Type type)
        {
            // only check static types
            if (!IsStatic(type))
                return;

            var extensionMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public)
                   .Where(IsExtension)
                   .Where(NotIgnored);

            var methods = extensionMethods.Where(NotGeneric);
            var collectionMethods = extensionMethods.Where(IsCollectionMethod);

            foreach (var method in methods)
            {
                if (IsWriterMethod(method))
                {
                    var dataType = GetWriterDataType(method);
                    writers.Register(module.ImportReference(dataType), module.ImportReference(method));
                }

                if (IsReaderMethod(method))
                {
                    var dataType = GetReaderDataType(method);
                    readers.Register(module.ImportReference(dataType), module.ImportReference(method));
                }
            }

            foreach (var method in collectionMethods)
            {
                if (IsWriterMethod(method))
                {
                    var dataType = GetWriterDataType(method);
                    writers.RegisterCollectionMethod(dataType.Resolve(), module.ImportReference(method));
                }

                if (IsReaderMethod(method))
                {
                    var dataType = GetReaderDataType(method);
                    readers.RegisterCollectionMethod(dataType.Resolve(), module.ImportReference(method));
                }
            }
        }
        public void RegisterExtensionMethodsInType(TypeDefinition type)
        {
            // only check static types
            if (!IsStatic(type))
                return;

            var extensionMethods = type.Methods
                   .Where(IsExtension)
                   .Where(NotIgnored);

            var methods = extensionMethods.Where(NotGeneric);
            var collectionMethods = extensionMethods.Where(IsCollectionMethod);

            foreach (var method in methods)
            {
                if (IsWriterMethod(method))
                {
                    var dataType = GetWriterDataType(method);
                    writers.Register(module.ImportReference(dataType), module.ImportReference(method));
                }

                if (IsReaderMethod(method))
                {
                    var dataType = GetReaderDataType(method);
                    readers.Register(module.ImportReference(dataType), module.ImportReference(method));
                }
            }

            foreach (var method in collectionMethods)
            {
                if (IsWriterMethod(method))
                {
                    var dataType = GetWriterDataType(method);
                    writers.RegisterCollectionMethod(dataType.Resolve(), module.ImportReference(method));
                }

                if (IsReaderMethod(method))
                {
                    var dataType = GetReaderDataType(method);
                    readers.RegisterCollectionMethod(dataType.Resolve(), module.ImportReference(method));
                }
            }
        }

        /// <summary>
        /// static classes are declared abstract and sealed at the IL level.
        /// <see href="https://stackoverflow.com/a/1175901/8479976"/>
        /// </summary>
        private static bool IsStatic(Type t) => t.IsSealed && t.IsAbstract;
        private static bool IsStatic(TypeDefinition t) => t.IsSealed && t.IsAbstract;

        private static bool IsExtension(MethodInfo method) => Attribute.IsDefined(method, typeof(ExtensionAttribute));
        private static bool IsExtension(MethodDefinition method) => method.HasCustomAttribute<ExtensionAttribute>();
        private static bool NotGeneric(MethodInfo method) => !method.IsGenericMethod;
        private static bool NotGeneric(MethodDefinition method) => !method.IsGenericInstance && !method.HasGenericParameters;
        private static bool NotIgnored(MethodInfo method) => !Attribute.IsDefined(method, typeof(WeaverIgnoreAttribute));
        private static bool NotIgnored(MethodDefinition method) => !method.HasCustomAttribute<WeaverIgnoreAttribute>();
        private static bool IsCollectionMethod(MethodInfo method) => Attribute.IsDefined(method, typeof(WeaverSerializeCollectionAttribute));
        private static bool IsCollectionMethod(MethodDefinition method) => method.HasCustomAttribute<WeaverSerializeCollectionAttribute>();


        private static bool IsWriterMethod(MethodInfo method)
        {
            if (method.GetParameters().Length != 2)
                return false;

            if (method.GetParameters()[0].ParameterType.FullName != typeof(NetworkWriter).FullName)
                return false;

            if (method.ReturnType != typeof(void))
                return false;

            return true;
        }
        private static bool IsWriterMethod(MethodDefinition method)
        {
            if (method.Parameters.Count != 2)
                return false;

            if (method.Parameters[0].ParameterType.FullName != typeof(NetworkWriter).FullName)
                return false;

            if (!method.ReturnType.Is(typeof(void)))
                return false;

            return true;
        }

        private static bool IsReaderMethod(MethodInfo method)
        {
            if (method.GetParameters().Length != 1)
                return false;

            if (method.GetParameters()[0].ParameterType.FullName != typeof(NetworkReader).FullName)
                return false;

            if (method.ReturnType == typeof(void))
                return false;

            return true;
        }
        private static bool IsReaderMethod(MethodDefinition method)
        {
            if (method.Parameters.Count != 1)
                return false;

            if (method.Parameters[0].ParameterType.FullName != typeof(NetworkReader).FullName)
                return false;

            if (method.ReturnType.Is(typeof(void)))
                return false;

            return true;
        }

        private TypeReference GetWriterDataType(MethodInfo method)
        {
            ReaderWriterProcessor.Log($"Found writer extension methods: {method.Name}");

            var dataType = method.GetParameters()[1].ParameterType;
            return module.ImportReference(dataType);
        }
        private static TypeReference GetWriterDataType(MethodDefinition method)
        {
            ReaderWriterProcessor.Log($"Found writer extension methods: {method.Name}");

            return method.Parameters[1].ParameterType;
        }


        private TypeReference GetReaderDataType(MethodInfo method)
        {
            ReaderWriterProcessor.Log($"Found reader extension methods: {method.Name}");
            return module.ImportReference(method.ReturnType);
        }
        private static TypeReference GetReaderDataType(MethodDefinition method)
        {
            ReaderWriterProcessor.Log($"Found reader extension methods: {method.Name}");
            return method.ReturnType;
        }
    }
}
