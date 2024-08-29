using System;
using System.Runtime.InteropServices;

namespace Mirage.Godot.Scripts.Serialization;

public static class SystemTypesExtensions
{
    // todo benchmark converters 
    /// <summary>
    /// Converts between uint and float without allocations
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntFloat
    {
        [FieldOffset(0)]
        public float FloatValue;

        [FieldOffset(0)]
        public uint IntValue;
    }

    /// <summary>
    /// Converts between ulong and double without allocations
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntDouble
    {
        [FieldOffset(0)]
        public double DoubleValue;

        [FieldOffset(0)]
        public ulong LongValue;
    }

    /// <summary>
    /// Converts between ulong and decimal without allocations
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntDecimal
    {
        [FieldOffset(0)]
        public ulong LongValue1;

        [FieldOffset(8)]
        public ulong LongValue2;

        [FieldOffset(0)]
        public decimal DecimalValue;
    }

    public static void WriteByteExtension(this NetworkWriter writer, byte value) => writer.WriteByte(value);

    public static void WriteSByteExtension(this NetworkWriter writer, sbyte value) => writer.WriteSByte(value);

    public static void WriteChar(this NetworkWriter writer, char value) => writer.WriteUInt16(value);

    public static void WriteBooleanExtension(this NetworkWriter writer, bool value) => writer.WriteBoolean(value);

    public static void WriteUInt16Extension(this NetworkWriter writer, ushort value)
    {
        writer.WriteUInt16(value);
    }

    public static void WriteInt16Extension(this NetworkWriter writer, short value) => writer.WriteInt16(value);

    public static void WriteSingleConverter(this NetworkWriter writer, float value)
    {
        var converter = new UIntFloat
        {
            FloatValue = value
        };
        writer.WriteUInt32(converter.IntValue);
    }
    public static void WriteDoubleConverter(this NetworkWriter writer, double value)
    {
        var converter = new UIntDouble
        {
            DoubleValue = value
        };
        writer.WriteUInt64(converter.LongValue);
    }
    public static void WriteDecimalConverter(this NetworkWriter writer, decimal value)
    {
        // the only way to read it without allocations is to both read and
        // write it with the FloatConverter (which is not binary compatible
        // to writer.Write(decimal), hence why we use it here too)
        var converter = new UIntDecimal
        {
            DecimalValue = value
        };
        writer.WriteUInt64(converter.LongValue1);
        writer.WriteUInt64(converter.LongValue2);
    }

    public static void WriteGuid(this NetworkWriter writer, Guid value)
    {
        var data = value.ToByteArray();
        writer.WriteBytes(data, 0, data.Length);
    }

    [WeaverSerializeCollection]
    public static void WriteNullable<T>(this NetworkWriter writer, T? nullable) where T : struct
    {
        var hasValue = nullable.HasValue;
        writer.WriteBoolean(hasValue);
        if (hasValue)
            writer.Write(nullable.Value);
    }




    public static byte ReadByteExtension(this NetworkReader reader) => reader.ReadByte();
    public static sbyte ReadSByteExtension(this NetworkReader reader) => reader.ReadSByte();
    public static char ReadChar(this NetworkReader reader) => (char)reader.ReadUInt16();
    public static bool ReadBooleanExtension(this NetworkReader reader) => reader.ReadBoolean();
    public static short ReadInt16Extension(this NetworkReader reader) => reader.ReadInt16();
    public static ushort ReadUInt16Extension(this NetworkReader reader) => reader.ReadUInt16();
    public static float ReadSingleConverter(this NetworkReader reader)
    {
        var converter = new UIntFloat
        {
            IntValue = reader.ReadUInt32()
        };
        return converter.FloatValue;
    }
    public static double ReadDoubleConverter(this NetworkReader reader)
    {
        var converter = new UIntDouble
        {
            LongValue = reader.ReadUInt64()
        };
        return converter.DoubleValue;
    }
    public static decimal ReadDecimalConverter(this NetworkReader reader)
    {
        var converter = new UIntDecimal
        {
            LongValue1 = reader.ReadUInt64(),
            LongValue2 = reader.ReadUInt64()
        };
        return converter.DecimalValue;
    }
    public static Guid ReadGuid(this NetworkReader reader) => new Guid(reader.ReadBytes(16));

    [WeaverSerializeCollection]
    public static T? ReadNullable<T>(this NetworkReader reader) where T : struct
    {
        var hasValue = reader.ReadBoolean();
        return hasValue ? reader.Read<T>() : null;
    }
}
