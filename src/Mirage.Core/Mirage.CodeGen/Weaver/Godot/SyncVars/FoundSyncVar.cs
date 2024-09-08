using Mirage.CodeGen;
using Mirage.Weaver.NetworkBehaviours;
using Mirage.Weaver.Serialization;
using Mono.Cecil;

namespace Mirage.Weaver.SyncVars
{
    internal class FoundSyncVar
    {
        public readonly ModuleDefinition Module;
        public readonly FoundNetworkBehaviour Behaviour;
        public readonly FieldDefinition FieldDefinition;
        public readonly int DirtyIndex;
        public long DirtyBit => 1L << DirtyIndex;

        public bool HasProcessed { get; set; } = false;

        public FoundSyncVar(ModuleDefinition module, FoundNetworkBehaviour behaviour, FieldDefinition fieldDefinition, int dirtyIndex)
        {
            Module = module;
            Behaviour = behaviour;
            FieldDefinition = fieldDefinition;
            DirtyIndex = dirtyIndex;
        }

        public ValueSerializer ValueSerializer { get; private set; }

        public string OriginalName { get; private set; }
        public TypeReference OriginalType { get; private set; }
        public bool IsWrapped { get; private set; }

        public bool HasHook { get; private set; }
        public SyncVarHook Hook { get; private set; }
        public bool InitialOnly { get; private set; }
        public bool InvokeHookOnServer { get; private set; }
        public bool InvokeHookOnOwner { get; private set; }

        public void SetWrapType()
        {
            OriginalName = FieldDefinition.Name;
            OriginalType = FieldDefinition.FieldType;

            if (CheckWrapType(OriginalType, out var wrapType))
            {
                IsWrapped = true;
                FieldDefinition.FieldType = wrapType;
            }
        }

        private bool CheckWrapType(TypeReference originalType, out TypeReference wrapType)
        {
            var typeReference = originalType;

            if (typeReference.Is<NetworkIdentity>())
            {
                wrapType = Module.ImportReference<NetworkIdentitySyncvar>();
                return true;
            }

            if (typeReference.Resolve().IsDerivedFrom<NetworkBehaviour>())
            {
                wrapType = Module.ImportReference<NetworkBehaviorSyncvar>();
                return true;
            }

            wrapType = null;
            return false;
        }

        public void ProcessAttributes(Writers writers, Readers readers)
        {
            var hook = HookMethodFinder.GetHookMethod(FieldDefinition, OriginalType);
            Hook = hook;
            HasHook = hook != null;

            InitialOnly = GetInitialOnly(FieldDefinition);
            InvokeHookOnServer = GetFireOnServer(FieldDefinition);
            InvokeHookOnOwner = GetFireOnOwner(FieldDefinition);

            ValueSerializer = ValueSerializerFinder.GetSerializer(this, writers, readers);

            if (!HasHook && (InvokeHookOnServer || InvokeHookOnOwner))
                throw new HookMethodException("'invokeHookOnServer' or 'InvokeHookOnOwner' is set to true but no hook was implemented. Please implement hook or set 'invokeHookOnServer' back to false or remove for default false.", FieldDefinition);
        }

        private static bool GetInitialOnly(FieldDefinition fieldDefinition)
        {
            var attr = fieldDefinition.GetCustomAttribute<SyncVarAttribute>();
            return attr.GetField(nameof(SyncVarAttribute.initialOnly), false);
        }

        private static bool GetFireOnServer(FieldDefinition fieldDefinition)
        {
            var attr = fieldDefinition.GetCustomAttribute<SyncVarAttribute>();
            return attr.GetField(nameof(SyncVarAttribute.invokeHookOnServer), false);
        }

        private static bool GetFireOnOwner(FieldDefinition fieldDefinition)
        {
            var attr = fieldDefinition.GetCustomAttribute<SyncVarAttribute>();
            return attr.GetField(nameof(SyncVarAttribute.invokeHookOnOwner), false);
        }
    }
}
