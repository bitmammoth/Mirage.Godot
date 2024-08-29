

using Godot;

namespace Mirage.Godot.Scripts.Serialization.Packers;

public sealed class Vector3Packer
{
    private readonly FloatPacker _xPacker;
    private readonly FloatPacker _yPacker;
    private readonly FloatPacker _zPacker;

    public Vector3Packer(float xMax, float yMax, float zMax, int xBitCount, int yBitCount, int zBitCount)
    {
        _xPacker = new FloatPacker(xMax, xBitCount);
        _yPacker = new FloatPacker(yMax, yBitCount);
        _zPacker = new FloatPacker(zMax, zBitCount);
    }
    public Vector3Packer(float xMax, float yMax, float zMax, float xPrecision, float yPrecision, float zPrecision)
    {
        _xPacker = new FloatPacker(xMax, xPrecision);
        _yPacker = new FloatPacker(yMax, yPrecision);
        _zPacker = new FloatPacker(zMax, zPrecision);
    }
    public Vector3Packer(Vector3 max, Vector3 precision)
    {
        _xPacker = new FloatPacker(max.X, precision.X);
        _yPacker = new FloatPacker(max.Y, precision.Y);
        _zPacker = new FloatPacker(max.Z, precision.Z);
    }

    public void Pack(NetworkWriter writer, Vector3 value)
    {
        _xPacker.Pack(writer, value.X);
        _yPacker.Pack(writer, value.Y);
        _zPacker.Pack(writer, value.Z);
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
