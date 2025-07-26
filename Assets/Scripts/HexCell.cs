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
    private int[] neighborsIndex = { -1, -1, -1, -1, -1, -1 };

    public HexCell(HexGrid grid, int index)
    {
        Grid = grid;
        Index = index;
    }

    public int ChunkIdx
    {
        get; set;
    }

    public int Index { get; private set; }

    public HexUnit Unit { get; set; }

    public HexGrid Grid { get; private set; }

    public int ColumnIndex { get; set; }

    public void MarkAsExplored() => Flags = Flags.With(HexCellFlags.Explored);

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
            return Values.elevation;
        }
        set
        {
            Grid.CellData[Index].values.elevation = value;
        }
    }

    public void RefreshPosition()
    {
        Grid.RefreshPosition(Index);
    }

    public IEnumerable<HexCell> RemoveInvalidRiver(HexGrid grid)
    {
        IEnumerable<HexCell> affeceted = defaultEnumer;
        if (
            Flags.HasAny(HexCellFlags.RiverOut) &&
             !IsValidRiverDestination(GetNeighbor(grid, Flags.RiverOutDirection()))
        )
        {
            affeceted = affeceted.Concat(RemoveOutgoingRiver(grid));
        }

        if (
            Flags.HasAny(HexCellFlags.RiverIn) &&
            !IsValidRiverDestination(GetNeighbor(grid, Flags.RiverInDirection()))
            )
        {
            affeceted = affeceted.Concat(RemoveIncomingRiver(grid));
        }

        return affeceted;
    }

    public IEnumerable<HexCell> RemoveRoad(HexGrid grid, HexDirection d)
    {
        Flags = Flags.WithoutRoad(d);
        HexCell neighbor = GetNeighbor(grid, d);
        if (neighbor)
        {
            neighbor.Flags = neighbor.Flags.WithoutRoad(d.Opposite());
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

        Flags = Flags.WithRoad(d);
        neighbor.Flags = neighbor.Flags.WithRoad(d.Opposite());
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
        if (!Flags.HasAny(HexCellFlags.RiverOut))
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(grid, Flags.RiverOutDirection());
        Flags = Flags.Without(HexCellFlags.RiverOut);
        neighbor.Flags = neighbor.Flags.Without(HexCellFlags.RiverIn);
        return new HexCell[] { this, neighbor };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>发生了改变的网格</returns>
    public IEnumerable<HexCell> RemoveIncomingRiver(HexGrid grid)
    {
        if (!Flags.HasAny(HexCellFlags.RiverIn))
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(grid, Flags.RiverInDirection());
        Flags = Flags.Without(HexCellFlags.RiverIn);
        neighbor.Flags = neighbor.Flags.Without(HexCellFlags.RiverOut);
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
        if (Flags.HasRiverOut(direction))
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
        if (Flags.HasRiverIn(direction))
        {
            affecetd = affecetd.Concat(RemoveIncomingRiver(grid));
        }

        Flags = Flags.WithRiverOut(direction);
        SpecialIndex = 0;

        affecetd = affecetd.Concat(neighbor.RemoveIncomingRiver(grid));
        neighbor.Flags = neighbor.Flags.WithRiverIn(direction.Opposite());
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

    public bool HasRiver
    {
        get
        {
            return Flags.HasAny(HexCellFlags.River);
        }
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return Flags.HasRiverIn(direction) || Flags.HasRiverOut(direction);
    }


    public Vector3 Position
    {
        get
        {
            return Grid.CellPositions[Index];
        }
        set
        {
            Grid.CellPositions[Index] = value;
        }
    }

    public HexCoordinates Coordinates => Grid.CellData[Index].coordinates;


    public int WaterLevel
    {
        get
        {
            return Values.waterLevel;
        }
        set
        {
            Grid.CellData[Index].values.waterLevel = value;
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

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return Flags.HasRoad(direction);
    }

    public byte TerrainTypeIndex
    {
        get
        {
            return Values.terrainTypeIndex;
        }
        set
        {
            Grid.CellData[Index].values.terrainTypeIndex = value;
        }
    }

    public void SetTerrainTypeIndex(HexGrid grid, byte value)
    {
        if (TerrainTypeIndex != value)
        {
            TerrainTypeIndex = value;
            grid.ShaderData.RefreshTerrain(Index);
        }
    }

    public byte UrbanLevel
    {
        get
        {
            return Values.urbanLevel;
        }
        set
        {
            Grid.CellData[Index].values.urbanLevel = value;
        }
    }

    public byte FarmLevel
    {
        get
        {
            return Values.farmLevel;
        }
        set
        {
            Grid.CellData[Index].values.farmLevel = value;
        }
    }

    public byte PlantLevel
    {
        get
        {
            return Values.plantLevel;
        }
        set
        {
            Grid.CellData[Index].values.plantLevel = value;
        }
    }

    public bool Walled
    {
        get
        {
            return Flags.Has(HexCellFlags.Wall);
        }
        set
        {
            Flags = value ? Flags.With(HexCellFlags.Wall) : Flags.Without(HexCellFlags.Wall);
        }
    }

    public byte SpecialIndex
    {
        get
        {
            return Values.specialIndex;
        }
        set
        {
            Grid.CellData[Index].values.specialIndex = value;
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
        TextMeshPro label = Grid.UiRects[Index].GetComponent<TextMeshPro>();
        label.text = text;
    }

    public void DisableHighlight()
    {
        Image highlight = Grid.UiRects[Index].rectTransform.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color)
    {
        Image highlight = Grid.UiRects[Index].rectTransform.GetChild(0).GetComponent<Image>();
        highlight.enabled = true;
        highlight.color = color;
    }

    public void RefreshAll()
    {
        RefreshPosition();
        Grid.ShaderData.RefreshTerrain(Index);
        Grid.ShaderData.RefreshVisibility(Index);
    }

    public HexCellFlags Flags
    {
        get => Grid.CellData[Index].flags;
        private set => Grid.CellData[Index].flags = value;
    }

    HexValues Values
    {
        get => Grid.CellData[Index].values;
    }

    public bool IsExplored
    {
        get
        {
            return Flags.Has(HexCellFlags.Explored) && Flags.Has(HexCellFlags.Explorable);
        }
        private set
        {
            Flags = Flags.With(HexCellFlags.Explored);
        }
    }

    public bool Explorable
    {
        get
        {
            return Flags.Has(HexCellFlags.Explorable);
        }
        set
        {
            if (value)
            {
                Flags = Flags.With(HexCellFlags.Explorable);
            }
            else
            {
                Flags = Flags.Without(HexCellFlags.Explorable);
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

    public void Save(BinaryWriter writer)
    {
        writer.Write((int)Flags);
        Values.Save(writer);
    }

    public void Load(HexGrid grid, BinaryReader reader)
    {

        Flags = (HexCellFlags)reader.ReadInt32();
        Grid.CellData[Index].values = HexValues.Load(reader);
        //20250724忘记刷新位置了，耗费了那么久。哎。。。。
        RefreshPosition();
        grid.ShaderData.RefreshTerrain(Index);
        grid.ShaderData.RefreshVisibility(Index);
    }

    public bool Equals(HexCell other)
    {
        return this == other;
    }

    public static implicit operator bool(HexCell cell) => cell != null;
}