using Mirage.CodeGen;
using Mirage.Godot.Scripts.Objects;
using Mirage.Godot.Scripts.RemoteCalls;
using Mono.Cecil;

namespace Mirage.CodeGen.Weaver.Godot.NetworkBehaviour;

internal class RegisterRpcHelper(ModuleDefinition module, TypeDefinition typeDefinition) : BaseMethodHelper(module, typeDefinition)
{
    public override string MethodName => nameof(RegisterRpc);

    protected override void AddParameters()
    {
        Method.AddParam<RemoteCallCollection>("collection");
    }

    protected override void AddLocals()
    {
    }
}
