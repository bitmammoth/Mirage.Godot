

using Godot;

namespace Mirage.Godot.Scripts.Serialization.Packers;

public sealed class Vector2Packer
{
    private readonly FloatPacker _xPacker;
    private readonly FloatPacker _yPacker;

    public Vector2Packer(float xMax, float yMax, int xBitCount, int yBitCount)
    {
        _xPacker = new FloatPacker(xMax, xBitCount);
        _yPacker = new FloatPacker(yMax, yBitCount);
    }
    public Vector2Packer(float xMax, float yMax, float xPrecision, float yPrecision)
    {
        _xPacker = new FloatPacker(xMax, xPrecision);
        _yPacker = new FloatPacker(yMax, yPrecision);
    }
    public Vector2Packer(Vector2 max, Vector2 precision)
    {
        _xPacker = new FloatPacker(max.X, precision.X);
        _yPacker = new FloatPacker(max.Y, precision.Y);
    }

    public void Pack(NetworkWriter writer, Vector2 value)
    {
        _xPacker.Pack(writer, value.X);
        _yPacker.Pack(writer, value.Y);
    }

    public Vector2 Unpack(NetworkReader reader)
    {
        Vector2 value = default;
        value.X = _xPacker.Unpack(reader);
        value.Y = _yPacker.Unpack(reader);
        return value;
    }
}
