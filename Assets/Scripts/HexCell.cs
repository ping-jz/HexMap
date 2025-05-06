using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class HexCell : MonoBehaviour
{
    [SerializeField]
    private HexCoordinates coordinates;
    [SerializeField]
    private HexCell[] neighbors;
    public Color color;
    private int elevation;
    private int chunkIndex;
    private HexCellFlags flags;
    private HexDirection incomingRiver, outgoingRiver;
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

    public IEnumerable<HexCell> removeInvalidRiver()
    {
        IEnumerable<HexCell> affeceted = default;
        if (
            flags.Has(HexCellFlags.OutgoingRiver) &&
            elevation < GetNeighbor(outgoingRiver).elevation)
        {
            affeceted = affeceted.Concat(RemoveOutgoingRiver());
        }

        if (
            flags.Has(HexCellFlags.IncomingRvier) &&
            elevation < GetNeighbor(incomingRiver).elevation)
        {
            affeceted = affeceted.Concat(RemoveIncomingRiver());
        }

        return affeceted;
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

    public HexDirection IncomingRiver
    {
        get
        {
            return incomingRiver;
        }
        set
        {
            incomingRiver = value;
        }
    }

    public HexDirection OutgoingRiver
    {
        get
        {
            return outgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd
    {
        get
        {
            return flags.Has(HexCellFlags.IncomingRvier) !=
                flags.Has(HexCellFlags.OutgoingRiver);
        }
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            flags.Has(HexCellFlags.IncomingRvier) && incomingRiver == direction ||
            flags.Has(HexCellFlags.OutgoingRiver) && outgoingRiver == direction;
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

    /// <summary>
    /// 
    /// </summary>
    /// <returns>发生了改变的网格</returns>
    public IEnumerable<HexCell> RemoveOutgoingRiver()
    {
        if (flags.HasNot(HexCellFlags.OutgoingRiver))
        {
            return defaultEnumer;
        }

        flags = flags.Without(HexCellFlags.OutgoingRiver);
        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.flags = neighbor.flags.Without(HexCellFlags.OutgoingRiver);
        return new HexCell[] { this, neighbor };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>发生了改变的网格</returns>
    public IEnumerable<HexCell> RemoveIncomingRiver()
    {
        if (flags.HasNot(HexCellFlags.IncomingRvier))
        {
            return defaultEnumer;
        }

        flags = flags.Without(HexCellFlags.IncomingRvier);
        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.flags = neighbor.flags.Without(HexCellFlags.IncomingRvier);
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
        if (flags.Has(HexCellFlags.OutgoingRiver) && outgoingRiver == direction)
        {
            return defaultEnumer;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (!neighbor || elevation < neighbor.elevation)
        {
            return defaultEnumer;
        }

        IEnumerable<HexCell> affecetd = defaultEnumer;
        affecetd = affecetd.Concat(RemoveOutgoingRiver());
        if (flags.Has(HexCellFlags.IncomingRvier) && incomingRiver == direction)
        {
            affecetd = affecetd.Concat(RemoveIncomingRiver());
        }

        flags = flags.With(HexCellFlags.OutgoingRiver);
        outgoingRiver = direction;

        affecetd = affecetd.Concat(neighbor.RemoveIncomingRiver());
        neighbor.flags = neighbor.flags.With(HexCellFlags.IncomingRvier);
        neighbor.IncomingRiver = direction.Opposite();

        affecetd = affecetd.Concat(new HexCell[] { this, neighbor });

        return affecetd;
    }

    IEnumerable<HexCell> defaultEnumer
    {
        get
        {
            return new HexCell[0];
        }
    }


}