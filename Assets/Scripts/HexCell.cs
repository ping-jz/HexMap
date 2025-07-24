using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine.UI;
using System;

[Serializable]
public class HexCell : IEquatable<HexCell>
{
    [SerializeField]
    private HexCoordinates coordinates;
    [SerializeField]
    private int[] neighborsIndex = { -1, -1, -1, -1, -1, -1 };
    [SerializeField]
    private HexCellFlags flags;
    [SerializeField]
    private HexValues values;
    public RectTransform uiRect;
    int elevation, waterLevel;

    public void MarkAsExplored() => flags = flags.With(HexCellFlags.Explored);

    public HexCell GetNeighbor(HexGrid grid, HexDirection direction)
    {
        int idx = neighborsIndex[(int)direction];
        return idx >= 0 ? grid.GetCell(idx) : null;
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighborsIndex[(int)direction] = cell.Index;
        cell.neighborsIndex[(int)direction.Opposite()] = Index;
    }

    public HexEdgeType GetEdgeType(HexGrid grid, HexDirection direction)
    {
        return HexMetrics.GetEdgeType(Elevation, grid.GetCell(neighborsIndex[(int)direction]).Elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(
            Elevation, otherCell.Elevation
        );
    }

    public int Elevation
    {
        get
        {
            return values.elevation;
        }
        set
        {
            values.elevation = value;
            Vector3 position = Position;
            position.y = value * HexMetrics.elevationStep;
            position.y +=
                (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.elevationPerturbStrength;
            Position = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
        }
    }

    public IEnumerable<HexCell> RemoveInvalidRiver(HexGrid grid)
    {
        IEnumerable<HexCell> affeceted = defaultEnumer;
        if (
            flags.HasAny(HexCellFlags.RiverOut) &&
             !IsValidRiverDestination(GetNeighbor(grid, flags.RiverOutToDirection()))
        )
        {
            affeceted = affeceted.Concat(RemoveOutgoingRiver(grid));
        }

        if (
            flags.HasAny(HexCellFlags.RiverIn) &&
            !IsValidRiverDestination(GetNeighbor(grid, flags.RiverInToDirection()))
            )
        {
            affeceted = affeceted.Concat(RemoveIncomingRiver(grid));
        }

        return affeceted;
    }

    public IEnumerable<HexCell> RemoveRoad(HexGrid grid, HexDirection d)
    {
        flags = flags.WithoutRoad(d);
        HexCell neighbor = GetNeighbor(grid, d);
        if (neighbor)
        {
            neighbor.flags = neighbor.flags.WithoutRoad(d.Opposite());
            return new HexCell[] { this, neighbor };
        }
        else
        {
            return new HexCell[] { this };
        }
    }

    public int GetElevationDifference(HexGrid grid, HexDirection direction)
    {
        HexCell neighbor = GetNeighbor(grid, direction);
        int difference = Elevation - (neighbor ? neighbor.Elevation : 0);
        return difference >= 0 ? difference : -difference;
    }

    public IEnumerable<HexCell> AddRoad(HexGrid grid, HexDirection d)
    {
        if (HasRiverThroughEdge(d) || GetElevationDifference(grid, d) > 1)
        {
            return defaultEnumer;
        }


        HexCell neighbor = GetNeighbor(grid, d);
        if (neighbor == null)
        {
            return defaultEnumer;
        }

        if (IsSpecial || neighbor.IsSpecial)
        {
            return defaultEnumer;
        }

        flags = flags.WithRoad(d);
        neighbor.flags = neighbor.flags.WithRoad(d.Opposite());
        return new HexCell[] { this, neighbor };

    }


    public IEnumerable<HexCell> RemoveRoads(HexGrid grid)
    {
        IEnumerable<HexCell> affected = defaultEnumer;
        for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
        {
            if (HasRoadThroughEdge(d))
            {
                affected = affected.Concat(RemoveRoad(grid, d));
            }
        }

        return affected;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>发生了改变的网格</returns>
    public IEnumerable<HexCell> RemoveOutgoingRiver(HexGrid grid)
    {
        if (!flags.HasAny(HexCellFlags.RiverOut))
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(grid, flags.RiverOutToDirection());
        flags = flags.Without(HexCellFlags.RiverOut);
        neighbor.flags = neighbor.flags.Without(HexCellFlags.RiverIn);
        return new HexCell[] { this, neighbor };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>发生了改变的网格</returns>
    public IEnumerable<HexCell> RemoveIncomingRiver(HexGrid grid)
    {
        if (!flags.HasAny(HexCellFlags.RiverIn))
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(grid, flags.RiverInToDirection());
        flags = flags.Without(HexCellFlags.RiverIn);
        neighbor.flags = neighbor.flags.Without(HexCellFlags.RiverOut);
        return new HexCell[] { this, neighbor };
    }

    public IEnumerable<HexCell> RemoveRivers(HexGrid grid)
    {
        IEnumerable<HexCell> affected1 = RemoveOutgoingRiver(grid);
        IEnumerable<HexCell> affecetd2 = RemoveIncomingRiver(grid);

        return affected1.Concat(affecetd2);
    }

    public IEnumerable<HexCell> SetOutgoingRiver(HexGrid grid, HexDirection direction)
    {
        if (flags.HasRiverOut(direction))
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(grid, direction);
        if (!IsValidRiverDestination(neighbor))
        {
            return defaultEnumer;
        }

        IEnumerable<HexCell> affecetd = defaultEnumer;
        affecetd = affecetd.Concat(RemoveOutgoingRiver(grid));
        if (flags.HasRiverIn(direction))
        {
            affecetd = affecetd.Concat(RemoveIncomingRiver(grid));
        }

        flags = flags.WithRiverOut(direction);
        SpecialIndex = 0;

        affecetd = affecetd.Concat(neighbor.RemoveIncomingRiver(grid));
        neighbor.flags = neighbor.flags.WithRiverIn(direction.Opposite());
        neighbor.SpecialIndex = 0;

        affecetd = affecetd.Concat(RemoveRoad(grid, direction));
        return affecetd;
    }

    IEnumerable<HexCell> defaultEnumer
    {
        get
        {
            return new HexCell[0];
        }
    }

    public float RiverSurfaceY
    {
        get
        {
            return
                (Elevation + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public float WaterSurfaceY
    {
        get
        {
            return
                (WaterLevel + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public bool HasIncomingRiver
    {
        get
        {
            return flags.HasAny(HexCellFlags.RiverIn);
        }
    }

    public bool HasIncomingRiverOf(HexDirection direction)
    {
        return HasIncomingRiver && direction == flags.RiverInToDirection();
    }


    public bool HasOutgoingRiver
    {
        get
        {
            return flags.HasAny(HexCellFlags.RiverOut);
        }
    }

    public bool HasRiverBeginOrEnd
    {
        get
        {
            return flags.HasAny(HexCellFlags.RiverIn) !=
                flags.HasAny(HexCellFlags.RiverOut);
        }
    }

    public bool HasRiver
    {
        get
        {
            return flags.HasAny(HexCellFlags.River);
        }
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return flags.HasRiverIn(direction) || flags.HasRiverOut(direction);
    }

    public float StreamBedY
    {
        get
        {
            return
                (Elevation + HexMetrics.streamBedElevationOffset) *
                HexMetrics.elevationStep;
        }
    }


    public Vector3 Position
    {
        get; set;
    }

    public int ChunkIdx
    {
        get; set;
    }

    public HexCoordinates Coordinates
    {
        get
        {
            return coordinates;
        }
        set
        {
            coordinates = value;
        }
    }

    public int WaterLevel
    {
        get
        {
            return values.waterLevel;
        }
        set
        {
            values.waterLevel = value;
        }
    }

    public bool IsUnderwater
    {
        get
        {
            return WaterLevel > Elevation;
        }
    }

    public int[] Neighbors
    {
        get
        {
            return neighborsIndex;
        }
    }

    public HexCellFlags Flags
    {
        get
        {
            return flags;
        }
        set
        {
            flags = value;
        }
    }

    public bool HasRoads
    {
        get
        {
            return flags.HasAny(HexCellFlags.Road);
        }
    }

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return flags.HasRoad(direction);
    }

    /// <summary>
    /// call {@link HexCell.HasRiverBeginOrEnd} before this method
    /// </summary>
    /// <returns></returns>
    public HexDirection RiverBeginOrEndDirection
    {
        get
        {
            return flags.HasAny(HexCellFlags.RiverIn) ?
            flags.RiverInToDirection() : flags.RiverOutToDirection();
        }
    }

    public byte TerrainTypeIndex
    {
        get
        {
            return values.terrainTypeIndex;
        }
        set
        {
            values.terrainTypeIndex = value;
        }
    }

    public void SetTerrainTypeIndex(HexGrid grid, byte value)
    {
        if (TerrainTypeIndex != value)
        {
            TerrainTypeIndex = value;
            grid.ShaderData.RefreshTerrain(this);
        }
    }

    public byte UrbanLevel
    {
        get
        {
            return values.urbanLevel;
        }
        set
        {
            values.urbanLevel = value;
        }
    }

    public byte FarmLevel
    {
        get
        {
            return values.farmLevel;
        }
        set
        {
            values.farmLevel = value;
        }
    }

    public byte PlantLevel
    {
        get
        {
            return values.plantLevel;
        }
        set
        {
            values.plantLevel = value;
        }
    }

    public bool Walled
    {
        get
        {
            return flags.Has(HexCellFlags.Wall);
        }
        set
        {
            flags = value ? flags.With(HexCellFlags.Wall) : flags.Without(HexCellFlags.Wall);
        }
    }

    public byte SpecialIndex
    {
        get
        {
            return values.specialIndex;
        }
        set
        {
            values.specialIndex = value;
        }
    }

    public bool IsSpecial
    {
        get
        {
            return SpecialIndex > 0;
        }
    }


    public bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (Elevation >= neighbor.Elevation || WaterLevel == neighbor.Elevation);
    }

    public void SetLabel(string text)
    {
        TextMeshPro label = uiRect.GetComponent<TextMeshPro>();
        label.text = text;
    }

    public void DisableHighlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = true;
        highlight.color = color;
    }

    public int Index { get; set; }

    public HexUnit Unit { get; set; }

    public int ColumnIndex { get; set; }

    public bool IsExplored
    {
        get
        {
            return flags.Has(HexCellFlags.Explored) && flags.Has(HexCellFlags.Explorable);
        }
        private set
        {
            flags = flags.With(HexCellFlags.Explored);
        }
    }

    public bool Explorable
    {
        get
        {
            return flags.Has(HexCellFlags.Explorable);
        }
        set
        {
            if (value)
            {
                flags = flags.With(HexCellFlags.Explorable);
            }
            else
            {
                flags = flags.Without(HexCellFlags.Explorable);
            }
        }
    }

    public int ViewElevation
    {
        get
        {
            return Elevation >= WaterLevel ? Elevation : WaterLevel;
        }
    }

    public void SetMapData(HexGrid grid, float data)
    {
        grid.ShaderData.SetMapData(this, data);
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((int)flags);
        values.Save(writer);
    }

    public void Load(HexGrid grid, BinaryReader reader)
    {

        flags = (HexCellFlags)reader.ReadInt32();
        values = HexValues.Load(reader);
        //20250724忘记刷新位置了，耗费了那么久。哎。。。。
        Elevation = values.elevation;
        grid.ShaderData.RefreshTerrain(this);
        grid.ShaderData.RefreshVisibility(this);
    }

    public bool Equals(HexCell other)
    {
        return this == other;
    }

    public static implicit operator bool(HexCell cell) => cell != null;
}