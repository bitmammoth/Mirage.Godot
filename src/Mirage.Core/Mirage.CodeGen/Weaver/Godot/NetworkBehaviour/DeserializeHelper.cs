using System;
using Mirage.CodeGen.Weaver.Godot.SyncVars;
using Mirage.Godot.Scripts;
using Mirage.Godot.Scripts.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.CodeGen.Weaver.Godot.NetworkBehaviour;

internal class DeserializeHelper(ModuleDefinition module, FoundNetworkBehaviour behaviour) : BaseMethodHelper(module, behaviour.TypeDefinition)
{
    private FoundNetworkBehaviour _behaviour = behaviour;

    public ParameterDefinition ReaderParameter { get; private set; }
    public ParameterDefinition InitializeParameter { get; private set; }
    /// <summary>
    /// IMPORTANT: this mask is only for this NB, it is not shifted based on base class
    /// </summary>
    public VariableDefinition DirtyBitsLocal { get; private set; }

    public override string MethodName => nameof(INetworkNodeWithSyncVar.DeserializeSyncVars);

    protected override void AddParameters()
    {
        ReaderParameter = Method.AddParam<NetworkReader>("reader");
        InitializeParameter = Method.AddParam<bool>("initialState");
    }

    protected override void AddLocals()
    {
        DirtyBitsLocal = Method.AddLocal<ulong>();
    }


    public void WriteIfInitial(Action body)
    {
        // Generates: if (initial)
        var initialStateLabel = Worker.Create(OpCodes.Nop);

        Worker.Append(Worker.Create(OpCodes.Ldarg, InitializeParameter));
        Worker.Append(Worker.Create(OpCodes.Brfalse, initialStateLabel));

        body.Invoke();

        Worker.Append(Worker.Create(OpCodes.Ret));

        // Generates: end if (initial)
        Worker.Append(initialStateLabel);
    }

    /// <summary>
    /// Writes Reads dirty bit mask for this NB,
    /// <para>Shifts by number of syncvars in base class, then writes number of bits in this class</para>
    /// </summary>
    public void ReadDirtyBitMask()
    {
        var readBitsMethod = _module.ImportReference(ReaderParameter.ParameterType.Resolve().GetMethod(nameof(NetworkReader.Read)));

        // Generates: reader.Read(n)
        // n is syncvars in this

        // get dirty bits
        Worker.Append(Worker.Create(OpCodes.Ldarg, ReaderParameter));
        Worker.Append(Worker.Create(OpCodes.Ldc_I4, _behaviour.SyncVars.Count));
        Worker.Append(Worker.Create(OpCodes.Call, readBitsMethod));
        Worker.Append(Worker.Create(OpCodes.Stloc, DirtyBitsLocal));

        SetDeserializeMask();
    }

    private void SetDeserializeMask()
    {
        // Generates: SetDeserializeMask(mask, n)
        // n is syncvars in base class
        Worker.Append(Worker.Create(OpCodes.Ldarg_0));
        Worker.Append(Worker.Create(OpCodes.Ldloc, DirtyBitsLocal));
        Worker.Append(Worker.Create(OpCodes.Ldc_I4, _behaviour.SyncVarCounter.GetInBase()));
        Worker.Append(Worker.Create(OpCodes.Call, () => NetworkNodeExtensions.SetDeserializeMask(default, default, default)));
    }

    internal void WriteIfSyncVarDirty(FoundSyncVar syncVar, Action body)
    {
        var endIf = Worker.Create(OpCodes.Nop);

        // we dont shift read bits, so we have to shift dirty bit here
        var syncVarIndex = syncVar.DirtyBit >> _behaviour.SyncVarCounter.GetInBase();

        // check if dirty bit is set
        Worker.Append(Worker.Create(OpCodes.Ldloc, DirtyBitsLocal));
        Worker.Append(Worker.Create(OpCodes.Ldc_I8, syncVarIndex));
        Worker.Append(Worker.Create(OpCodes.And));
        Worker.Append(Worker.Create(OpCodes.Brfalse, endIf));

        body.Invoke();

        Worker.Append(endIf);
    }
}
