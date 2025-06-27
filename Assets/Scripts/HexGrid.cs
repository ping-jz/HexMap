using TMPro;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;

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
    [SerializeField]
    private HexUnit unitPrefab;

    HexCellShaderData cellShaderData;
    private int chunkCountX, chunkCountZ;
    HexCell[] cells;
    HexGridChunk[] chunks;
    List<HexUnit> units = new List<HexUnit>();

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

    public HexUnit UnitPrefab
    {
        get
        {
            return unitPrefab;
        }
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);

        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
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
        ClearPath();
        ClearUnits();
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

        cellShaderData.Initialize(cellCountX, cellCountZ);
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
        cell.Index = i;
        cell.ShaderData = cellShaderData;

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
    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }
        return null;
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

    public IEnumerator ShowUI(bool visible)
    {
        foreach (HexGridChunk chunk in chunks)
        {
            chunk.ShowUI(visible);
            yield return null;
        }
    }

    HexCell currentPathFrom, currentPathTo;

    public void FindPath(HexCell from, HexCell to, HexUnit hexUnit)
    {
        ClearPath();
        if (search(from, to, hexUnit))
        {
            currentPathFrom = from;
            currentPathTo = to;
        }
        ShowPath(hexUnit.Speed);
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Grid = this;
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }


    /// <summary>
    /// 非常棒的寻路算法演示
    /// https://catlikecoding.com/unity/tutorials/hex-map/part-16/
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <returns></returns>
    /// 
    private int searchPhase = 1;
    private bool search(HexCell from, HexCell to, HexUnit hexUnit)
    {
        searchPhase += 1;
        int searchFrontierPhase = searchPhase;

        PriorityQueue<HexCell> frontier = new PriorityQueue<HexCell>();

        from.SearchPhase = searchFrontierPhase;
        from.Distance = 0;
        from.PathFrom = null;
        frontier.Enqueue(from, from.SearchPriority);

        int speed = hexUnit.Speed;
        while (frontier.Count > 0)
        {

            HexCell current = frontier.Dequeue();
            if (current == to)
            {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            //还是有很多重复计算
            for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (!neighbor)
                {
                    continue;
                }

                if (!hexUnit.IsValidDestination(neighbor))
                {
                    continue;
                }
                int moveCost = hexUnit.getMoveCost(current, neighbor, d);
                if (moveCost < 0)
                {
                    continue;
                }

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;
                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase != searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic =
                        neighbor.Coordinates.DistanceTo(to.Coordinates);
                    frontier.Enqueue(neighbor, neighbor.SearchPriority);
                }
                else if (distance < neighbor.Distance)
                {
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    frontier.Enqueue(neighbor, neighbor.SearchPriority);
                }
            }
        }

        return false;
    }



    void ShowPath(int speed)
    {
        if (HasPath)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }

            currentPathFrom.EnableHighlight(Color.blue);
            currentPathTo.EnableHighlight(Color.red);
        }
    }

    public void ClearPath()
    {
        if (HasPath)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            currentPathFrom.DisableHighlight();
            currentPathTo.DisableHighlight();
        }
        currentPathFrom = currentPathTo = null;
    }

    public bool HasPath
    {
        get
        {
            return currentPathFrom && currentPathTo;
        }
    }

    public List<HexCell> GetPath()
    {
        if (!HasPath)
        {
            return null;
        }
        List<HexCell> paths = ListPool<HexCell>.Get();
        for (HexCell to = currentPathTo; to != null; to = to.PathFrom)
        {
            paths.Add(to);
        }
        paths.Add(currentPathFrom);
        paths.Reverse();
        return paths;
    }

    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    List<HexCell> GetVisibleCells(HexCell from, int range)
    {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();
        searchPhase += 1;
        int searchFrontierPhase = searchPhase;
        searchFrontierPhase = UnityEngine.Random.Range(0, searchFrontierPhase);
        int searched = -searchFrontierPhase;
        PriorityQueue<HexCell> frontier = new PriorityQueue<HexCell>();

        from.SearchPhase = searchFrontierPhase;
        from.Distance = 0;
        frontier.Enqueue(from, from.SearchPriority);

        while (frontier.Count > 0)
        {

            HexCell current = frontier.Dequeue();
            from.SearchPhase = searched;
            visibleCells.Add(current);
            //还是有很多重复计算
            for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (!neighbor || neighbor.SearchPhase == searched)
                {
                    continue;
                }

                int distance = current.Distance + 1;
                if (distance > range)
                {
                    continue;
                }

                if (neighbor.SearchPhase != searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    frontier.Enqueue(neighbor, neighbor.SearchPriority);
                }
            }
        }

        return visibleCells;
    }

    void ClearUnits()
    {
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        foreach (HexCell cell in cells)
        {
            cell.Save(writer);
        }

        writer.Write(units.Count);
        foreach (HexUnit unit in units)
        {
            unit.Save(writer);
        }
    }

    public void Load(BinaryReader reader, int version)
    {
        ClearPath();
        ClearUnits();
        StopAllCoroutines();
        if (!CreateMap(reader.ReadInt32(), reader.ReadInt32()))
        {
            return;
        }

        bool originModel = cellShaderData.ImmediateMode;
        cellShaderData.ImmediateMode = true;

        foreach (HexCell cell in cells)
        {
            cell.Load(reader);
        }

        if (version >= 2)
        {
            int unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }

        foreach (HexGridChunk chunk in chunks)
        {
            chunk.Refresh();
        }
        cellShaderData.ImmediateMode = originModel;
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