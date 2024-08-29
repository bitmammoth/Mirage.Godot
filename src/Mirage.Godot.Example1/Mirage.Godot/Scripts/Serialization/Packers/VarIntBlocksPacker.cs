using System;
using System.Runtime.CompilerServices;

namespace Mirage.Godot.Scripts.Serialization.Packers;

public static class VarIntBlocksPacker
{
    // todo needs doc comments
    // todo neeeds tests

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Pack(NetworkWriter writer, ulong value, int blockSize)
    {
        // always writes atleast 1 block
        var count = 1;
        var checkValue = value >> blockSize;
        while (checkValue != 0)
        {
            count++;
            checkValue >>= blockSize;
        }
        // count = 1, write = b0, (1<<(1-1) -1 => 1<<0 -1) => 1 -1 => 0)
        // count = 2, write = b01
        // count = 3, write = b011, (1<<(3-1) -1 => 1<<2 -1) => 100 - 1 => 011)
        writer.Write((1ul << (count - 1)) - 1, count);
        writer.Write(value, Math.Min(64, blockSize * count));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Unpack(NetworkReader reader, int blockSize)
    {
        var blocks = 1;
        // read bits till we see a zero
        while (reader.ReadBoolean())
        {
            blocks++;
        }

        return reader.Read(Math.Min(64, blocks * blockSize));
    }
}
