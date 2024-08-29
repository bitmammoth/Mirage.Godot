using System.Collections.Generic;
using Mirage.CodeGen.Weaver.Godot.SyncVars;
using Mono.Cecil;

namespace Mirage.CodeGen.Weaver.Godot.NetworkBehaviour;

internal class FoundNetworkBehaviour(ModuleDefinition module, TypeDefinition td)
{
    public readonly ModuleDefinition Module = module;
    public readonly TypeDefinition TypeDefinition = td;
    public readonly ConstFieldTracker SyncVarCounter = new ConstFieldTracker("SYNC_VAR_COUNT", td, 64, "[SyncVar]");

    public List<FoundSyncVar> SyncVars { get; private set; } = [];

    public FoundSyncVar AddSyncVar(FieldDefinition fd)
    {
        var dirtyIndex = SyncVarCounter.GetInBase() + SyncVars.Count;
        var syncVar = new FoundSyncVar(Module, this, fd, dirtyIndex);
        SyncVars.Add(syncVar);
        return syncVar;
    }

    public void SetSyncVarCount()
    {
        SyncVarCounter.Set(SyncVars.Count);
    }
}
