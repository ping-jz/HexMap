using System;

[Flags]
public enum HexCellFlags
{
    Nothing = 0,
    IncomingRvier = 0b00_01,
    OutgoingRiver = 0b00_10,
    River = 0b00_11,
    RoadTR = 0b000001_00,
    RoadR = 0b000010_00,
    RoadBR = 0b000100_00,
    RoadBL = 0b001000_00,
    RoadL = 0b010000_00,
    RoadTL = 0b100000_00,
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