using System;

[Flags]
public enum HexCellFlags
{
    Nothing = 0,
    IncomingRvier = 0b00_01,
    OutgoingRiver = 0b00_10,
    River = 0b00_11,
    RoadNE = 0b000001_00,
    RoadE = 0b000010_00,
    RoadSE = 0b000100_00,
    RoadSW = 0b001000_00,
    RoadW = 0b010000_00,
    RoadNW = 0b100000_00,
    Road = 0b111111_00,
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