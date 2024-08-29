using System.Collections.Generic;
using Mirage.CodeGen;
using Mirage.CodeGen.Mirage.CecilExtensions.Logging;
using Mirage.CodeGen.Weaver.Serialization;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Syncing;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.CodeGen.Weaver.Godot
{
    public class SyncObjectProcessor(Readers readers, Writers writers, IWeaverLogger logger)
    {
        private readonly List<FieldDefinition> _syncObjects = [];

        private readonly Readers _readers = readers;
        private readonly Writers _writers = writers;
        private readonly IWeaverLogger _logger = logger;

        /// <summary>
        /// Finds SyncObjects fields in a type
        /// <para>Type should be a NetworkBehaviour</para>
        /// </summary>
        /// <param name="td"></param>
        /// <returns></returns>
        public void ProcessSyncObjects(TypeDefinition td)
        {
            foreach (var fd in td.Fields)
            {
                if (fd.FieldType.IsGenericParameter || fd.ContainsGenericParameter) // Just ignore all generic objects.
                    continue;

                var tf = fd.FieldType.Resolve();
                if (tf == null)
                    continue;

                if (tf.Implements<ISyncObject>())
                {
                    if (fd.IsStatic)
                    {
                        _logger.Error($"{fd.Name} cannot be static", fd);
                        continue;
                    }

                    GenerateReadersAndWriters(fd.FieldType);

                    _syncObjects.Add(fd);
                }
            }

            RegisterSyncObjects(td);
        }

        /// <summary>
        /// Generates serialization methods for synclists
        /// </summary>
        /// <param name="td">The synclist class</param>
        /// <param name="mirrorBaseType">the base SyncObject td inherits from</param>
        private void GenerateReadersAndWriters(TypeReference tr)
        {
            if (tr is GenericInstanceType genericInstance)
            {
                foreach (var argument in genericInstance.GenericArguments)
                {
                    if (!argument.IsGenericParameter)
                    {
                        _readers.TryGetFunction(argument, null);
                        _writers.TryGetFunction(argument, null);
                    }
                }
            }

            var baseType = tr?.Resolve()?.BaseType;
            if (baseType != null)
                GenerateReadersAndWriters(baseType);
        }

        private void RegisterSyncObjects(TypeDefinition netBehaviourSubclass)
        {
            Weaver.DebugLog(netBehaviourSubclass, "GenerateConstants ");

            netBehaviourSubclass.AddToConstructor(_logger, (worker) =>
            {
                foreach (var fd in _syncObjects)
                {
                    GenerateSyncObjectRegistration(worker, fd);
                }
            });
        }

        public static bool ImplementsSyncObject(TypeReference typeRef)
        {
            try
            {
                // value types cant inherit from SyncObject
                if (typeRef.IsValueType)
                    return false;

                return typeRef.Resolve().Implements<ISyncObject>();
            }
            catch
            {
                // sometimes this will fail if we reference a weird library that can't be resolved, so we just swallow that exception and return false
            }

            return false;
        }

        /*
            // generates code like:
            this.InitSyncObject(m_sizes);
        */
        private static void GenerateSyncObjectRegistration(ILProcessor worker, FieldDefinition fd)
        {
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Ldfld, fd));

            worker.Append(worker.Create(OpCodes.Call, () => NetworkNodeExtensions.InitSyncObject(default, default)));
        }
    }
}
