using System;
using UnityEngine;

[Flags]
public enum HexCellFlags
{
    Nothing = 0,
    IncomingRvier = 0b0001,
    OutgoingRiver = 0b0010,
    River = 0b0011
}

public static class HexCellFlagsExtensions
{
    public static bool Has(this HexCellFlags flags, HexCellFlags mask) =>
        (flags & mask) == mask;

    public static bool HasAny(this HexCellFlags flags, HexCellFlags mask) =>
        (flags & mask) != 0;
    public static bool HasNot(this HexCellFlags flags, HexCellFlags mask) =>
        (flags & mask) != mask;


    public static HexCellFlags With(this HexCellFlags flags, HexCellFlags mask) =>
        flags | mask;
    public static HexCellFlags Without(this HexCellFlags flags, HexCellFlags mask) =>
        flags & ~mask;
}