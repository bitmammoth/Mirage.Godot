using System;

namespace Mirage.Godot.Scripts.Serialization.Packers;

/// <summary>
/// Packs a float using <see cref="ZigZag"/> and <see cref="VarIntBlocksPacker"/>
/// </summary>
public sealed class VarFloatPacker(float precision, int blockSize)
{
    private readonly int _blockSize = blockSize;
    private readonly float _precision = precision;
    private readonly float _inversePrecision = 1 / precision;

    public void Pack(NetworkWriter writer, float value)
    {
        var scaled = (int)Math.Round(value * _inversePrecision);
        var zig = ZigZag.Encode(scaled);
        VarIntBlocksPacker.Pack(writer, zig, _blockSize);
    }

    public float Unpack(NetworkReader reader)
    {
        var zig = (uint)VarIntBlocksPacker.Unpack(reader, _blockSize);
        var scaled = ZigZag.Decode(zig);
        return scaled * _precision;
    }
}
