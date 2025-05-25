using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class HexCell : MonoBehaviour
{
    [SerializeField]
    private HexCoordinates coordinates;
    [SerializeField]
    private HexCell[] neighbors;
    private int terrainTypeIndex;
    private int elevation;
    private int chunkIndex;
    private int waterLevel;
    private HexCellFlags flags;
    private int urbanLevel;
    public RectTransform uiRect;

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(
            elevation, otherCell.elevation
        );
    }

    public int Elevation
    {
        get
        {
            return elevation;
        }
        set
        {
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y +=
                (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
        }
    }

    public IEnumerable<HexCell> RemoveInvalidRiver()
    {
        IEnumerable<HexCell> affeceted = defaultEnumer;
        if (
            flags.HasAny(HexCellFlags.RiverOut) &&
             !IsValidRiverDestination(GetNeighbor(flags.RiverOutToDirection()))
        )
        {
            affeceted = affeceted.Concat(RemoveOutgoingRiver());
        }

        if (
            flags.HasAny(HexCellFlags.RiverIn) &&
            !IsValidRiverDestination(GetNeighbor(flags.RiverInToDirection()))
            )
        {
            affeceted = affeceted.Concat(RemoveIncomingRiver());
        }

        return affeceted;
    }

    public IEnumerable<HexCell> RemoveRoad(HexDirection d)
    {
        flags = flags.WithoutRoad(d);
        HexCell neighbor = neighbors[(int)d];
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

    public int GetElevationDifference(HexDirection direction)
    {
        HexCell neighbor = GetNeighbor(direction);
        int difference = elevation - (neighbor ? neighbor.elevation : 0);
        return difference >= 0 ? difference : -difference;
    }

    public IEnumerable<HexCell> AddRoad(HexDirection d)
    {
        if (HasRiverThroughEdge(d) || GetElevationDifference(d) > 1)
        {
            return defaultEnumer;
        }

        flags = flags.WithRoad(d);
        HexCell neighbor = neighbors[(int)d];
        if (neighbor)
        {
            neighbor.flags = neighbor.flags.WithRoad(d.Opposite());
            return new HexCell[] { this, neighbor };
        }
        else
        {
            return new HexCell[] { this };
        }
    }


    public IEnumerable<HexCell> RemoveRoads()
    {
        IEnumerable<HexCell> affected = defaultEnumer;
        for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
        {
            if (HasRoadThroughEdge(d))
            {
                affected = affected.Concat(RemoveRoad(d));
            }
        }

        return affected;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>发生了改变的网格</returns>
    public IEnumerable<HexCell> RemoveOutgoingRiver()
    {
        if (!flags.HasAny(HexCellFlags.RiverOut))
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(flags.RiverOutToDirection());
        flags = flags.Without(HexCellFlags.RiverOut);
        neighbor.flags = neighbor.flags.Without(HexCellFlags.RiverIn);
        return new HexCell[] { this, neighbor };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>发生了改变的网格</returns>
    public IEnumerable<HexCell> RemoveIncomingRiver()
    {
        if (!flags.HasAny(HexCellFlags.RiverIn))
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(flags.RiverInToDirection());
        flags = flags.Without(HexCellFlags.RiverIn);
        neighbor.flags = neighbor.flags.Without(HexCellFlags.RiverOut);
        return new HexCell[] { this, neighbor };
    }

    public IEnumerable<HexCell> RemoveRivers()
    {
        IEnumerable<HexCell> affected1 = RemoveOutgoingRiver();
        IEnumerable<HexCell> affecetd2 = RemoveIncomingRiver();

        return affected1.Concat(affecetd2);
    }

    public IEnumerable<HexCell> SetOutgoingRiver(HexDirection direction)
    {
        if (flags.HasRiverOut(direction))
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
        {
            return defaultEnumer;
        }

        IEnumerable<HexCell> affecetd = defaultEnumer;
        affecetd = affecetd.Concat(RemoveOutgoingRiver());
        if (flags.HasRiverIn(direction))
        {
            affecetd = affecetd.Concat(RemoveIncomingRiver());
        }

        flags = flags.WithRiverOut(direction);

        affecetd = affecetd.Concat(neighbor.RemoveIncomingRiver());
        neighbor.flags = neighbor.flags.WithRiverIn(direction.Opposite());

        affecetd = affecetd.Concat(RemoveRoad(direction));
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
                (elevation + HexMetrics.waterElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public float WaterSurfaceY
    {
        get
        {
            return
                (waterLevel + HexMetrics.waterElevationOffset) *
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
                (elevation + HexMetrics.streamBedElevationOffset) *
                HexMetrics.elevationStep;
        }
    }


    public Vector3 Position
    {
        get
        {
            return transform.localPosition;
        }
    }

    public int ChunkIdx
    {
        get
        {
            return chunkIndex;
        }
        set
        {
            chunkIndex = value;
        }
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
            return waterLevel;
        }
        set
        {
            if (waterLevel == value)
            {
                return;
            }
            waterLevel = value;
        }
    }

    public bool IsUnderwater
    {
        get
        {
            return waterLevel > elevation;
        }
    }

    public HexCell[] Neighbors
    {
        get
        {
            return neighbors;
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

    public Color Color
    {
        get
        {
            return HexMetrics.colors[terrainTypeIndex];
        }
    }

    public int TerrainTypeIndex
    {
        get
        {
            return terrainTypeIndex;
        }
        set
        {

            terrainTypeIndex = value;
        }
    }

    public int UrbanLevel
    {
        get
        {
            return urbanLevel;
        }
        set
        {
            if (urbanLevel != value)
            {
                urbanLevel = value;
            }
        }
    }


    public bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(terrainTypeIndex);
        writer.Write(Elevation);
        writer.Write(waterLevel);
        writer.Write((int)flags);
        writer.Write(urbanLevel);
    }

    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadInt32();
        Elevation = reader.ReadInt32();
        waterLevel = reader.ReadInt32();
        flags = (HexCellFlags)reader.ReadInt32();
        urbanLevel = reader.ReadInt32();
    }
}