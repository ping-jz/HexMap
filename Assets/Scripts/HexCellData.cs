[System.Serializable]
public struct HexCellData
{
	public HexCellFlags flags;

	public HexValues values;

	public HexCoordinates coordinates;

	public readonly int Elevation => values.elevation;

	public readonly int WaterLevel => values.waterLevel;

	public readonly int TerrainTypeIndex => values.terrainTypeIndex;

	public readonly int UrbanLevel => values.urbanLevel;

	public readonly int FarmLevel => values.farmLevel;

	public readonly int PlantLevel => values.plantLevel;

	public readonly int SpecialIndex => values.specialIndex;

	public readonly bool Walled => flags.HasAny(HexCellFlags.Wall);

	public readonly bool HasRoads => flags.HasAny(HexCellFlags.Road);

	public readonly bool IsExplored =>
		flags.Has(HexCellFlags.Explored | HexCellFlags.Explorable);

	public readonly bool IsSpecial => values.specialIndex > 0;

	public readonly bool IsUnderwater => values.waterLevel > values.elevation;

	public readonly bool HasIncomingRiver => flags.HasAny(HexCellFlags.RiverIn);

	public readonly bool HasOutgoingRiver => flags.HasAny(HexCellFlags.RiverOut);

	public readonly bool HasRiver => flags.HasAny(HexCellFlags.River);

	public readonly bool HasRiverBeginOrEnd =>
		HasIncomingRiver != HasOutgoingRiver;

	public HexDirection RiverBeginOrEndDirection
	{
		get
		{
			return flags.HasAny(HexCellFlags.RiverIn) ?
			   IncomingRiver : OutgoingRiver;
		}
	}

	public readonly HexDirection IncomingRiver => flags.RiverInDirection();

	public readonly HexDirection OutgoingRiver => flags.RiverOutDirection();

	public readonly bool Explorable
	{
		get
		{
			return flags.Has(HexCellFlags.Explorable);
		}
	}

	public void MarkAsExplored() => flags = flags.With(HexCellFlags.Explored);

	public readonly float StreamBedY =>
		(values.elevation + HexMetrics.streamBedElevationOffset) *
		HexMetrics.elevationStep;

	public readonly float RiverSurfaceY =>
		(values.elevation + HexMetrics.waterElevationOffset) *
		HexMetrics.elevationStep;

	public readonly float WaterSurfaceY =>
		(values.waterLevel + HexMetrics.waterElevationOffset) *
		HexMetrics.elevationStep;

	public readonly int ViewElevation =>
		Elevation >= WaterLevel ? Elevation : WaterLevel;

	public readonly HexEdgeType GetEdgeType(HexCellData otherCell) =>
		HexMetrics.GetEdgeType(values.elevation, otherCell.values.elevation);

	public readonly bool HasIncomingRiverThroughEdge(HexDirection direction) =>
		flags.HasRiverIn(direction);

	public readonly bool HasRiverThroughEdge(HexDirection direction) =>
		flags.HasRiverIn(direction) || flags.HasRiverOut(direction);

	public readonly bool HasRoadThroughEdge(HexDirection direction) =>
		flags.HasRoad(direction);
}