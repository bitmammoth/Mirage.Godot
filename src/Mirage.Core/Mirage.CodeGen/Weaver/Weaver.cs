using System;
using System.Collections.Generic;
using Godot;
using Mirage.CodeGen.Mirage.CecilExtensions.Logging;
using Mirage.CodeGen.Weaver.Godot;
using Mirage.CodeGen.Weaver.Processors;
using Mirage.CodeGen.Weaver.Serialization;
using Mirage.Godot.Scripts.Objects;
using Mirage.Weaver;
using Mono.Cecil;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

namespace Mirage.CodeGen.Weaver
{
    /// <summary>
    /// Weaves an Assembly
    /// <para>
    /// Debug Defines:<br />
    /// - <c>WEAVER_DEBUG_LOGS</c><br />
    /// - <c>WEAVER_DEBUG_TIMER</c><br />
    /// </para>
    /// </summary>
    public class Weaver(IWeaverLogger logger) : WeaverBase(logger)
    {
        private Readers _readers;
        private Writers _writers;
        private PropertySiteProcessor _propertySiteProcessor;

        [Conditional("WEAVER_DEBUG_LOGS")]
        public static void DebugLog(TypeDefinition td, string message)
        {
            Console.WriteLine($"Weaver[{td.Name}] {message}");
        }

        private static void Log(string msg)
        {
            Console.WriteLine($"[Weaver] {msg}");
        }

        protected override ResultType Process(AssemblyDefinition assembly, CompiledAssembly compiledAssembly)
        {
            Log($"Starting weaver on {compiledAssembly.Name}");
            try
            {
                var module = assembly.MainModule;
                if (module.HasGeneratedClass())
                {
                    logger.Warning("GeneratedClass already exists. stopping weaver. (this normally happens if weaver runs on the same DLL more than once");
                    return ResultType.NoChanges;
                }

                _readers = new Readers(module, logger);
                _writers = new Writers(module, logger);
                _propertySiteProcessor = new PropertySiteProcessor(logger);
                var rwProcessor = new ReaderWriterProcessor(module, _readers, _writers, logger);

                var modified = false;
                using (timer.Sample("ReaderWriterProcessor"))
                {
                    modified = rwProcessor.Process();
                }

                var foundTypes = FindAllClasses(module);

                using (timer.Sample("AttributeProcessor"))
                {
                    var attributeProcessor = new AttributeProcessor(module, logger);
                    modified |= attributeProcessor.ProcessTypes(foundTypes);
                }

                using (timer.Sample("WeaveNetworkBehavior"))
                {
                    foreach (var foundType in foundTypes)
                    {
                        if (foundType.IsNetworkBehaviour)
                            modified |= WeaveNetworkBehavior(foundType);
                    }
                }

                if (modified)
                {
                    using (timer.Sample("propertySiteProcessor"))
                    {
                        _propertySiteProcessor.Process(module);
                    }

                    using (timer.Sample("InitializeReaderAndWriters"))
                    {
                        rwProcessor.InitializeReaderAndWriters();
                    }
                }

                return ResultType.Success;
            }
            catch (Exception e)
            {
                logger.Error("Exception :" + e);
                // write line too because the error about doesn't show stacktrace
                Console.WriteLine("[WeaverException] :" + e);
                return ResultType.Failed;
            }
            finally
            {
                Log($"Finished weaver on {compiledAssembly.Name}");
            }
        }

        private IReadOnlyList<FoundType> FindAllClasses(ModuleDefinition module)
        {
            using (timer.Sample("FindAllClasses"))
            {
                var foundTypes = new List<FoundType>();
                foreach (var type in module.Types)
                {
                    ProcessType(type, foundTypes);

                    foreach (var nested in type.NestedTypes)
                    {
                        ProcessType(nested, foundTypes);
                    }
                }

                return foundTypes;
            }
        }

        private static void ProcessType(TypeDefinition type, List<FoundType> foundTypes)
        {
            if (!type.IsClass) return;

            var parent = type.BaseType;
            var isNetworkBehaviour = false;
            var isMonoBehaviour = false;
            while (parent != null)
            {
                if (parent.Is<NetworkBehaviour>())
                {
                    isNetworkBehaviour = true;
                    isMonoBehaviour = true;
                    break;
                }
                if (parent.Is<Node>())
                {
                    isMonoBehaviour = true;
                    break;
                }

                parent = parent.TryResolveParent();
            }

            foundTypes.Add(new FoundType(type, isNetworkBehaviour, isMonoBehaviour));
        }

        private bool WeaveNetworkBehavior(FoundType foundType)
        {
            var behaviourClasses = FindAllBaseTypes(foundType);

            var modified = false;
            // process this and base classes from parent to child order
            for (var i = behaviourClasses.Count - 1; i >= 0; i--)
            {
                var behaviour = behaviourClasses[i];
                if (NetworkBehaviourProcessor.WasProcessed(behaviour)) continue;
                modified |= new NetworkBehaviourProcessor(behaviour, _readers, _writers, _propertySiteProcessor, logger).Process();
            }
            return modified;
        }

        /// <summary>
        /// Returns all base types that are between the type and NetworkBehaviour
        /// </summary>
        /// <param name="foundType"></param>
        /// <returns></returns>
        private static List<TypeDefinition> FindAllBaseTypes(FoundType foundType)
        {
            var behaviourClasses = new List<TypeDefinition>();

            var type = foundType.TypeDefinition;
            while (type != null)
            {
                if (type.Is<NetworkBehaviour>())
                    break;

                behaviourClasses.Add(type);
                type = type.BaseType.TryResolve();
            }

            return behaviourClasses;
        }
    }

    public class FoundType(TypeDefinition typeDefinition, bool isNetworkBehaviour, bool isMonoBehaviour)
    {
        public readonly TypeDefinition TypeDefinition = typeDefinition;

        /// <summary>
        /// Is Derived From NetworkBehaviour
        /// </summary>
        public readonly bool IsNetworkBehaviour = isNetworkBehaviour;

        public readonly bool IsMonoBehaviour = isMonoBehaviour;

        public override string ToString()
        {
            return TypeDefinition.ToString();
        }
    }
}
