using Mirage.CodeGen.Weaver.Godot.NetworkBehaviour;
using Mirage.CodeGen.Weaver.Serialization;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Attributes;
using Mirage.Godot.Scripts.Objects;
using Mirage.Godot.Scripts.Syncing;
using Mirage.Weaver.SyncVars;
using Mono.Cecil;

namespace Mirage.CodeGen.Weaver.Godot.SyncVars;

internal class FoundSyncVar(ModuleDefinition module, FoundNetworkBehaviour behaviour, FieldDefinition fieldDefinition, int dirtyIndex)
{
    public readonly ModuleDefinition Module = module;
    public readonly FoundNetworkBehaviour Behaviour = behaviour;
    public readonly FieldDefinition FieldDefinition = fieldDefinition;
    public readonly int DirtyIndex = dirtyIndex;
    public long DirtyBit => 1L << DirtyIndex;

    /// <summary>
    /// Flag to say if the sync var was successfully processed or not.
    /// We can check this else where in the code to so we dont throw extra errors when syncvar is invalid
    /// </summary>
    public bool HasProcessed { get; set; } = false;

    public ValueSerializer ValueSerializer { get; private set; }

    public string OriginalName { get; private set; }
    public TypeReference OriginalType { get; private set; }
    public bool IsWrapped { get; private set; }


    public bool HasHook { get; private set; }
    public SyncVarHook Hook { get; private set; }
    public bool InitialOnly { get; private set; }
    public bool InvokeHookOnServer { get; private set; }
    public bool InvokeHookOnOwner { get; private set; }

    /// <summary>
    /// Changing the type of the field to the wrapper type, if one exists
    /// </summary>
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
            // change the type of the field to a wrapper NetworkIdentitySyncvar
            wrapType = Module.ImportReference<NetworkIdentitySyncvar>();
            return true;
        }

        if (typeReference.Resolve().IsDerivedFrom<INetworkNode>())
        {
            wrapType = Module.ImportReference<NetworkBehaviorSyncvar>();
            return true;
        }

        wrapType = null;
        return false;
    }


    /// <summary>
    /// Finds any attribute values needed for this syncvar
    /// </summary>
    /// <param name="module"></param>
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
        return attr.GetField(nameof(SyncVarAttribute.InitialOnly), false);
    }

    private static bool GetFireOnServer(FieldDefinition fieldDefinition)
    {
        var attr = fieldDefinition.GetCustomAttribute<SyncVarAttribute>();
        return attr.GetField(nameof(SyncVarAttribute.InvokeHookOnServer), false);
    }

    private static bool GetFireOnOwner(FieldDefinition fieldDefinition)
    {
        var attr = fieldDefinition.GetCustomAttribute<SyncVarAttribute>();
        return attr.GetField(nameof(SyncVarAttribute.InvokeHookOnOwner), false);
    }
}
