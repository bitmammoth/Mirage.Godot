using System.Runtime.CompilerServices;

namespace Mirage.Godot.Example1.Mirage.Godot.Serialization;

public static class BitMask
{
    /// <summary>
    /// Creates mask for <paramref name="bits"/>
    /// <para>
    /// (showing 32 bits for simplify, result is 64 bit)
    /// <br/>
    /// Example bits = 4 => mask = 00000000_00000000_00000000_00001111
    /// <br/>
    /// Example bits = 10 => mask = 00000000_00000000_00000011_11111111
    /// </para>
    /// </summary>
    /// <param name="bits"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Mask(int bits)
    {
        return bits == 0 ? 0 : ulong.MaxValue >> (64 - bits);
    }

    /// <summary>
    /// Creates Mask either side of start and end
    /// <para>Note this mask is only valid for start [0..63] and end [0..64]</para>
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong OuterMask(int start, int end)
    {
        return (ulong.MaxValue << start) ^ (ulong.MaxValue >> (64 - end));
    }
}
