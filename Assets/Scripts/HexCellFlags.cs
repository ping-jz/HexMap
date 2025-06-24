using System;
using UnityEngine.Rendering.Universal;

[Flags]
public enum HexCellFlags
{
    Nothing = 0,
    RoadTR = 0b000001,
    RoadR = 0b000010,
    RoadBR = 0b000100,
    RoadBL = 0b001000,
    RoadL = 0b010000,
    RoadTL = 0b100000,
    Road = 0b111111,

    RiverInTR = 0b000001_000000,
    RiverInR = 0b000010_000000,
    RiverInBR = 0b000100_000000,
    RiverInBL = 0b001000_000000,
    RiverInL = 0b010000_000000,
    RiverInTL = 0b100000_000000,

    RiverIn = 0b111111_000000,

    RiverOutTR = 0b000001_000000_000000,
    RiverOutR = 0b000010_000000_000000,
    RiverOutBR = 0b000100_000000_000000,
    RiverOutBL = 0b001000_000000_000000,
    RiverOutL = 0b010000_000000_000000,
    RiverOutTL = 0b100000_000000_000000,
    RiverOut = 0b111111_000000_000000,
    River = 0b111111_111111_000000,

    Wall = 0b1_000000_000000_000000,
    Explored = 0b10_000000_000000_000000,
}

public static class HexCellFlagsExtensions
{
    public static bool Has(this HexCellFlags flags, HexCellFlags mask) =>
        (flags & mask) == mask;

    static bool Has(
        this HexCellFlags flags, HexCellFlags start, HexDirection direction) =>
        ((int)flags & ((int)start << (int)direction)) != 0;

    public static bool HasRoad(this HexCellFlags flags, HexDirection direction) =>
        flags.Has(HexCellFlags.RoadTR, direction);

    public static bool HasRiverIn(this HexCellFlags flags, HexDirection direction) =>
        flags.Has(HexCellFlags.RiverInTR, direction);

    public static bool HasRiverOut(this HexCellFlags flags, HexDirection direction) =>
        flags.Has(HexCellFlags.RiverOutTR, direction);

    public static bool HasAny(this HexCellFlags flags, HexCellFlags mask) =>
        (flags & mask) != 0;
    public static bool HasNot(this HexCellFlags flags, HexCellFlags mask) =>
        (flags & mask) != mask;


    public static HexCellFlags With(this HexCellFlags flags, HexCellFlags mask) =>
        flags | mask;

    static HexCellFlags With(
        this HexCellFlags flags, HexCellFlags start, HexDirection direction) =>
        flags | (HexCellFlags)((int)start << (int)direction);

    static HexCellFlags Without(
        this HexCellFlags flags, HexCellFlags start, HexDirection direction) =>
        flags & ~(HexCellFlags)((int)start << (int)direction);

    public static HexCellFlags Without(this HexCellFlags flags, HexCellFlags mask) =>
        flags & ~mask;

    public static HexCellFlags WithRoad(
        this HexCellFlags flags, HexDirection direction) =>
        flags.With(HexCellFlags.RoadTR, direction);

    public static HexCellFlags WithoutRoad(
        this HexCellFlags flags, HexDirection direction) =>
        flags.Without(HexCellFlags.RoadTR, direction);

    public static HexCellFlags WithRiverIn(
        this HexCellFlags flags, HexDirection direction) =>
        flags.With(HexCellFlags.RiverInTR, direction);

    public static HexCellFlags WithOutRiverIn(
        this HexCellFlags flags, HexDirection direction) =>
        flags.Without(HexCellFlags.RiverInTR, direction);

    public static HexCellFlags WithRiverOut(
        this HexCellFlags flags, HexDirection direction) =>
        flags.With(HexCellFlags.RiverOutTR, direction);

    public static HexCellFlags WithOutRiverOut(
        this HexCellFlags flags, HexDirection direction) =>
        flags.Without(HexCellFlags.RiverOutTR, direction);

    public static HexDirection RoadToDirection(this HexCellFlags flags) =>
       flags.ToDirection(0);

    public static HexDirection RiverInToDirection(this HexCellFlags flags) =>
        flags.ToDirection(6);

    public static HexDirection RiverOutToDirection(this HexCellFlags flags) =>
        flags.ToDirection(12);

    static HexDirection ToDirection(this HexCellFlags flags, int shift) =>
        (((int)flags >> shift) & 0b111111) switch
        {
            0b000001 => HexDirection.TopRight,
            0b000010 => HexDirection.Right,
            0b000100 => HexDirection.BottomRight,
            0b001000 => HexDirection.BottomLeft,
            0b010000 => HexDirection.Left,
            0b100000 => HexDirection.TopLeft,
            _ => throw new IndexOutOfRangeException($"Invalid HexCellFlags value: {flags}"),
        };

}