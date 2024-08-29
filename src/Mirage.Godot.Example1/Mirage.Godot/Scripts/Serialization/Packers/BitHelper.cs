using System;
using System.Runtime.CompilerServices;

namespace Mirage.Godot.Scripts.Serialization.Packers;

public static class BitHelper
{
    /// <summary>
    /// Gets the number of bits need for <paramref name="precision"/> in range negative to positive <paramref name="max"/>
    /// <para>
    /// WARNING: these methods are not fast, dont use in hotpath
    /// </para>
    /// </summary>
    /// <param name="max"></param>
    /// <param name="precision">lowest precision required, bit count will round up so real precision might be higher</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BitCount(float max, float precision)
    {
        return BitCount(max, precision, true);
    }

    /// <summary>
    /// Gets the number of bits need for <paramref name="precision"/> in range <paramref name="max"/>
    /// <para>If signed then range is negative max to positive max, If unsigned then 0 to max</para>
    /// <para>
    /// WARNING: these methods are not fast, dont use in hotpath
    /// </para>
    /// </summary>
    /// <param name="max"></param>
    /// <param name="precision">lowest precision required, bit count will round up so real precision might be higher</param>
    /// <returns></returns>
    public static int BitCount(float max, float precision, bool signed)
    {
        float multiplier = signed ? 2 : 1;
        return (int)Math.Floor(Math.Log(multiplier * max / precision, 2)) + 1;
    }

    /// <summary>
    /// Gets the number of bits need for <paramref name="max"/>
    /// <para>
    /// WARNING: these methods are not fast, dont use in hotpath
    /// </para>
    /// </summary>
    /// <param name="max"></param>
    /// <param name="precision">lowest precision required, bit count will round up so real precision might be higher</param>
    /// <returns></returns>
    public static int BitCount(ulong max)
    {
        return (int)Math.Floor(Math.Log(max, 2)) + 1;
    }
}
