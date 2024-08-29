using System;
using System.Runtime.CompilerServices;
using Mirage.Godot.Example1.Mirage.Godot.Serialization;

namespace Mirage.Godot.Scripts.Serialization.Packers;

public sealed class VarIntPacker
{
    // todo needs doc comments
    // todo need attribute to validate large bits based on pack type (eg if packing ushort, make sure largebits is 16 or less)

    private readonly int _smallBitCount;
    private readonly int _mediumBitsCount;
    private readonly int _largeBitsCount;
    private readonly ulong _smallValue;
    private readonly ulong _mediumValue;
    private readonly ulong _largeValue;
    private readonly bool _throwIfOverLarge;

    public VarIntPacker(ulong smallValue, ulong mediumValue)
        : this(BitHelper.BitCount(smallValue), BitHelper.BitCount(mediumValue), 64, false) { }
    public VarIntPacker(ulong smallValue, ulong mediumValue, ulong largeValue, bool throwIfOverLarge = true)
        : this(BitHelper.BitCount(smallValue), BitHelper.BitCount(mediumValue), BitHelper.BitCount(largeValue), throwIfOverLarge) { }

    public static VarIntPacker FromBitCount(int smallBits, int mediumBits)
        => FromBitCount(smallBits, mediumBits, 64, false);
    public static VarIntPacker FromBitCount(int smallBits, int mediumBits, int largeBits, bool throwIfOverLarge = true)
        => new VarIntPacker(smallBits, mediumBits, largeBits, throwIfOverLarge);

    private VarIntPacker(int smallBits, int mediumBits, int largeBits, bool throwIfOverLarge)
    {
        _throwIfOverLarge = throwIfOverLarge;
        if (smallBits == 0) throw new ArgumentException("Small value can not be zero", nameof(smallBits));
        if (smallBits >= mediumBits) throw new ArgumentException("Medium value must be greater than small value", nameof(mediumBits));
        if (mediumBits >= largeBits) throw new ArgumentException("Large value must be greater than medium value", nameof(largeBits));
        if (largeBits > 64) throw new ArgumentException("Large bits must be 64 or less", nameof(largeBits));
        // force medium to also be 62 or less so we can use 1 write call (2 bits to say its medium + 62 value bits
        if (mediumBits > 62) throw new ArgumentException("Medium bits must be 62 or less", nameof(mediumBits));

        _smallBitCount = smallBits;
        _mediumBitsCount = mediumBits;
        _largeBitsCount = largeBits;

        // mask is also max value for n bits
        _smallValue = BitMask.Mask(smallBits);
        _mediumValue = BitMask.Mask(mediumBits);
        _largeValue = BitMask.Mask(largeBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PackUlong(NetworkWriter writer, ulong value)
    {
        pack(writer, value, 64);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PackUint(NetworkWriter writer, uint value)
    {
        pack(writer, value, 32);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PackUshort(NetworkWriter writer, ushort value)
    {
        pack(writer, value, 16);
    }

    private void pack(NetworkWriter writer, ulong value, int maxBits)
    {
        if (value <= _smallValue)
            // start with b0 to say small, then value
            writer.Write(value << 1, _smallBitCount + 1);
        else if (value <= _mediumValue)
        {
            // start with b01 to say medium, then value
            writer.Write((value << 2) | 0b01, _mediumBitsCount + 2);
        }
        else if (value <= _largeValue)
        {
            // start with b11 to say large, then value
            // use 2 write calls here because bitCount could be 64
            writer.Write(0b11, 2);
            writer.Write(value, Math.Min(maxBits, _largeBitsCount));
        }
        else
        {
            if (_throwIfOverLarge)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Value is over max of {_largeValue}");
            else
            {
                // if no throw write MaxValue
                // we dont want to write value here because it will be masked and lose some high bits
                // need 2 write calls here because max is 64+2 bits
                writer.Write(0b11, 2);
                writer.Write(ulong.MaxValue, Math.Min(maxBits, _largeBitsCount));
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong UnpackUlong(NetworkReader reader)
    {
        return unpack(reader, 64);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint UnpackUint(NetworkReader reader)
    {
        return (uint)unpack(reader, 32);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort UnpackUshort(NetworkReader reader)
    {
        return (ushort)unpack(reader, 16);
    }

    private ulong unpack(NetworkReader reader, int maxBits)
    {
        if (!reader.ReadBoolean())
            return reader.Read(_smallBitCount);
        else
        {
            return !reader.ReadBoolean() ? reader.Read(_mediumBitsCount) : reader.Read(Math.Min(_largeBitsCount, maxBits));
        }
    }
}
