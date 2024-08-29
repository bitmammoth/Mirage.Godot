

using Godot;

namespace Mirage.Godot.Scripts.Serialization.Packers;

/// <summary>
/// Packs a vector3 using <see cref="ZigZag"/> and <see cref="VarIntBlocksPacker"/>
/// </summary>
public sealed class VarVector3Packer(Vector3 precision, int blocksize)
{
    private readonly VarFloatPacker _xPacker = new VarFloatPacker(precision.X, blocksize);
    private readonly VarFloatPacker _yPacker = new VarFloatPacker(precision.Y, blocksize);
    private readonly VarFloatPacker _zPacker = new VarFloatPacker(precision.Z, blocksize);

    public void Pack(NetworkWriter writer, Vector3 position)
    {
        _xPacker.Pack(writer, position.X);
        _yPacker.Pack(writer, position.Y);
        _zPacker.Pack(writer, position.Z);
    }

    public Vector3 Unpack(NetworkReader reader)
    {
        Vector3 value = default;
        value.X = _xPacker.Unpack(reader);
        value.Y = _yPacker.Unpack(reader);
        value.Z = _zPacker.Unpack(reader);
        return value;
    }
}
