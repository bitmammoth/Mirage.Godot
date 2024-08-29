using Mirage.CodeGen;
using Mirage.Godot.Scripts.Serialization.Packers;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.CodeGen.Weaver.Serialization
{
    internal class BlockSizeSerializer(int blockSize, OpCode? typeConverter) : ValueSerializer
    {
        public override bool IsIntType => true;

        private readonly int blockSize = blockSize;
        private readonly OpCode? typeConverter = typeConverter;

        public override void AppendWriteField(ModuleDefinition module, ILProcessor worker, ParameterDefinition writerParameter, ParameterDefinition typeParameter, FieldReference fieldReference)
        {
            var writeWithBlockSize = module.ImportReference(() => VarIntBlocksPacker.Pack(default, default, default));

            worker.Append(LoadParamOrArg0(worker, writerParameter));
            worker.Append(LoadParamOrArg0(worker, typeParameter));
            worker.Append(worker.Create(OpCodes.Ldfld, fieldReference));
            worker.Append(worker.Create(OpCodes.Conv_U8));
            worker.Append(worker.Create(OpCodes.Ldc_I4, blockSize));
            worker.Append(worker.Create(OpCodes.Call, writeWithBlockSize));
        }

        public override void AppendWriteParameter(ModuleDefinition module, ILProcessor worker, VariableDefinition writer, ParameterDefinition valueParameter)
        {
            var writeWithBlockSize = module.ImportReference(() => VarIntBlocksPacker.Pack(default, default, default));

            worker.Append(worker.Create(OpCodes.Ldloc, writer));
            worker.Append(worker.Create(OpCodes.Ldarg, valueParameter));
            worker.Append(worker.Create(OpCodes.Conv_U8));
            worker.Append(worker.Create(OpCodes.Ldc_I4, blockSize));
            worker.Append(worker.Create(OpCodes.Call, writeWithBlockSize));
        }

        public override void AppendRead(ModuleDefinition module, ILProcessor worker, ParameterDefinition readerParameter, TypeReference fieldType)
        {
            var writeWithBlockSize = module.ImportReference(() => VarIntBlocksPacker.Unpack(default, default));

            worker.Append(worker.Create(OpCodes.Ldarg, readerParameter));
            worker.Append(worker.Create(OpCodes.Ldc_I4, blockSize));
            worker.Append(worker.Create(OpCodes.Call, writeWithBlockSize));

            // convert result to correct size if needed
            if (typeConverter.HasValue)
                worker.Append(worker.Create(typeConverter.Value));
        }
    }
}
