

using Godot;

namespace Mirage.Godot.Scripts.Serialization.Packers;

/// <summary>
/// Packs a vector3 using <see cref="ZigZag"/> and <see cref="VarIntBlocksPacker"/>
/// </summary>
public sealed class VarVector2Packer(Vector2 precision, int blocksize)
{
    private readonly VarFloatPacker _xPacker = new VarFloatPacker(precision.X, blocksize);
    private readonly VarFloatPacker _yPacker = new VarFloatPacker(precision.Y, blocksize);

    public void Pack(NetworkWriter writer, Vector2 position)
    {
        _xPacker.Pack(writer, position.X);
        _yPacker.Pack(writer, position.Y);
    }

    public Vector2 Unpack(NetworkReader reader)
    {
        Vector2 value = default;
        value.X = _xPacker.Unpack(reader);
        value.Y = _yPacker.Unpack(reader);
        return value;
    }
}
