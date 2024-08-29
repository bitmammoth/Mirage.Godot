using System.Linq;
using Mirage.CodeGen;
using Mirage.CodeGen.Mirage.CecilExtensions.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.Weaver
{
    /// <summary>
    /// Thrown when can't generate read or write for a type
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    /// <param name="message">Reason method could not be generated</param>
    /// <param name="typeRef">Type that read or write could not be generated for</param>
    internal class SerializeFunctionException(string message, TypeReference typeRef) : WeaverException(message, typeRef, null)
    {
    }

    internal class NetworkBehaviourException : WeaverException
    {
        public NetworkBehaviourException(string message, TypeDefinition type, SequencePoint sequencePoint = null) : base(message, type, sequencePoint) { }
        public NetworkBehaviourException(string message, MemberReference memberReference, SequencePoint sequencePoint = null) : base(message, memberReference, sequencePoint) { }
    }

    internal class SyncVarException : WeaverException
    {
        public SyncVarException(string message, MemberReference memberReference) : base(message, memberReference, null) { }
        public SyncVarException(string message, MemberReference memberReference, SequencePoint sequencePoint) : base(message, memberReference, sequencePoint) { }
    }

    internal class RpcException(string message, MethodReference rpcMethod) : WeaverException(message, rpcMethod, rpcMethod.Resolve().DebugInformation.SequencePoints.FirstOrDefault())
    {
    }
}


namespace Mirage.Weaver.SyncVars
{
    internal class HookMethodException : SyncVarException
    {
        public HookMethodException(string message, MemberReference memberReference) : base(message, memberReference) { }
        public HookMethodException(string message, MemberReference memberReference, MethodDefinition method) : base(message, memberReference, method.GetFirstSequencePoint()) { }
    }
}

namespace Mirage.Weaver.Serialization
{
    internal abstract class ValueSerializerException(string message) : WeaverException(message, null, null)
    {
    }

    internal class BitCountException(string message) : ValueSerializerException(message)
    {
    }
    internal class VarIntException(string message) : ValueSerializerException(message)
    {
    }
    internal class VarIntBlocksException(string message) : ValueSerializerException(message)
    {
    }
    internal class ZigZagException(string message) : ValueSerializerException(message)
    {
    }
    internal class BitCountFromRangeException(string message) : ValueSerializerException(message)
    {
    }
    internal class FloatPackException(string message) : ValueSerializerException(message)
    {
    }
    internal class Vector3PackException(string message) : ValueSerializerException(message)
    {
    }
    internal class Vector2PackException(string message) : ValueSerializerException(message)
    {
    }
    internal class QuaternionPackException(string message) : ValueSerializerException(message)
    {
    }
}
