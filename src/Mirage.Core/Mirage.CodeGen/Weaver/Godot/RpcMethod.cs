using Mirage.Godot.Scripts.Attributes;
using Mono.Cecil;

namespace Mirage.CodeGen.Weaver.Godot;

public abstract class RpcMethod
{
    /// <summary>Original method created by user, body replaced with code that serializes params and sends message</summary>
    public MethodDefinition Stub;
    /// <summary>Method that receives the call and deserialize parmas</summary>
    public MethodDefinition Skeleton;
    /// <summary>Hash given to method in order to call it over the network. Should be unqiue.</summary>
    public int Index;

    public ReturnType ReturnType;
}

public class ServerRpcMethod : RpcMethod
{
    public bool RequireAuthority;
}

public class ClientRpcMethod : RpcMethod
{
    public RpcTarget Target;
    public bool ExcludeOwner;
}
