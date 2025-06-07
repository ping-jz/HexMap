using TMPro;
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using UnityEditor.Search;
using System.Collections.Generic;

[Flags]
public enum GizmoMode
{
    Nothing = 0,
    Terrian = 0b0001,
    River = 0b0010,
    Road = 0b0100,
    Wall = 0b1000,
    All = 0b1111
}

public class HexGrid : MonoBehaviour
{

    [SerializeField]
    private HexCell cellPrefab;
    [SerializeField]
    private HexGridChunk chunkPrefab;
    [SerializeField]
    private TextMeshPro cellLabelPrefab;
    [SerializeField]
    private Texture2D noiseSource;
    [SerializeField]
    private int cellCountX = 20, cellCountZ = 15;
    [SerializeField]
    GizmoMode gizmos;
    [SerializeField]
    private int seed;
    private int chunkCountX, chunkCountZ;
    HexCell[] cells;
    HexGridChunk[] chunks;

    public int ChunkCountX
    {
        get { return chunkCountX; }
    }

    public int ChunkCountZ
    {
        get { return chunkCountZ; }
    }

    public int CellCountX
    {
        get { return cellCountX; }
    }

    public int CellCountZ
    {
        get { return cellCountZ; }
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);

        CreateMap(cellCountX, cellCountZ);
    }

    public bool CreateMap(int x, int z)
    {
        if (
            x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.chunkSizeZ != 0
        )
        {
            Debug.LogError("Unsupported map size.");
            return false;
        }
        if (chunks != null)
        {
            foreach (HexGridChunk chunk in chunks)
            {
                Destroy(chunk.gameObject);
            }
        }
        cellCountX = x;
        cellCountZ = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCell();
        ShowUI(false);
        return true;
    }

    void OnEnable()
    {
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int i = 0; i < chunks.Length; i++)
        {
            HexGridChunk chunk = chunks[i] = Instantiate(chunkPrefab);
            chunk.transform.SetParent(transform, false);
        }
    }

    private void CreateCell()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);


        HexCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        TextMeshPro label = Instantiate(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        cell.uiRect = label.rectTransform;
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.Left, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.BottomRight, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.BottomLeft, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.BottomLeft, cells[i - cellCountX]);
                //基数行最后一个网格没有SE方向的邻居
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.BottomRight, cells[i - cellCountX + 1]);
                }
            }
        }
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        int chunkIdx = chunkX + chunkZ * chunkCountX;
        HexGridChunk chunk = chunks[chunkIdx];

        //这个本地坐标转换不错的，计算x,z经过了多少个网格块
        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        cell.ChunkIdx = chunkIdx;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    //从屏幕坐标转换世界坐标，然后转换为本地坐标
    //遇到问题之后多回来看看
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        int x = coordinates.X + z / 2;
        if (z < 0 || z >= cellCountZ)
        {
            return null;
        }
        if (x < 0 || x >= cellCountX)
        {
            return null;
        }
        return cells[x + z * cellCountX];
    }

    public HexGridChunk GetChunk(HexCell cell)
    {
        return chunks[cell.ChunkIdx];
    }

    public void ShowUI(bool visible)
    {
        foreach (HexGridChunk chunk in chunks)
        {
            chunk.ShowUI(visible);
        }
    }

    public void FindDistancesTo(HexCell cell)
    {
        StopAllCoroutines();
        StartCoroutine(search(cell));
    }

    private IEnumerator search(HexCell cell)
    {
        foreach (HexCell c in cells)
        {
            c.Distance = int.MaxValue;
        }

        WaitForSeconds delay = new WaitForSeconds(1 / 1000f);
        Queue<HexCell> frontier = new Queue<HexCell>();
        cell.Distance = 0;
        frontier.Enqueue(cell);
        while (frontier.Count > 0)
        {
            yield return delay;
            HexCell current = frontier.Dequeue();
            for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (!neighbor || neighbor.Distance != int.MaxValue)
                {
                    continue;
                }

                if (neighbor.IsUnderwater)
                {
                    continue;
                }

                HexEdgeType edgeType = current.GetEdgeType(neighbor);

                if (edgeType == HexEdgeType.Cliff)
                {
                    continue;
                }

                int distance = current.Distance;
                if (current.HasRoadThroughEdge(d))
                {
                    distance += 1;
                }
                else if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    distance += edgeType == HexEdgeType.Flat ? 5 : 10;
                    distance += neighbor.UrbanLevel + neighbor.FarmLevel +
                        neighbor.PlantLevel;
                }

                neighbor.Distance = distance;
                frontier.Enqueue(neighbor);
            }
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        foreach (HexCell cell in cells)
        {
            cell.Save(writer);
        }
    }

    public void Load(BinaryReader reader)
    {
        StopAllCoroutines();
        if (!CreateMap(reader.ReadInt32(), reader.ReadInt32()))
        {
            return;
        }

        foreach (HexCell cell in cells)
        {
            cell.Load(reader);
        }

        foreach (HexGridChunk chunk in chunks)
        {
            chunk.Refresh();
        }
    }

    void OnDrawGizmos()
    {
        if (gizmos == GizmoMode.Nothing)
        {
            return;
        }

        foreach (HexGridChunk chunk in chunks)
        {
            chunk.DrawGizmos(gizmos);
        }
    }
}