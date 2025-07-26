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
    HexCellSearchData[] cellSearchDatas;
    int[] cellVisibility;
    HexGridChunk[] chunks;
    List<HexUnit> units = new List<HexUnit>();
    Transform[] columns;

    int currentCenterColumnIndex = -1;
    bool wrapping = true;

    public HexCellShaderData ShaderData => cellShaderData;

    public HexCellData[] CellData
    { get; private set; }

    public Vector3[] CellPositions
    { get; private set; }

    public TextMeshPro[] UiRects
    {
        get; private set;
    }

    public bool Wrapping
    {
        get { return wrapping; }
    }

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

    public HexCellSearchData[] SearchData
    {
        get
        {
            return cellSearchDatas;
        }
    }

    void Awake()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);

        cellShaderData = gameObject.AddComponent<HexCellShaderData>();
        CreateMap(cellCountX, cellCountZ, wrapping);
    }

    public bool CreateMap(int x, int z, bool wrapping)
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

        if (columns != null)
        {
            foreach (Transform chunk in columns)
            {
                Destroy(chunk.gameObject);
            }
        }

        cellCountX = x;
        cellCountZ = z;
        this.wrapping = wrapping;
        currentCenterColumnIndex = -1;
        HexMetrics.wrapSize = wrapping ? cellCountX : 0;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        cellShaderData.Initialize(this, cellCountX, cellCountZ);
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
            HexMetrics.wrapSize = wrapping ? cellCountX : 0;
            ResetVisibility();
        }
    }

    void CreateChunks()
    {
        columns = new Transform[chunkCountX];
        for (int x = 0; x < chunkCountX; x++)
        {
            columns[x] = new GameObject("Column").transform;
            columns[x].SetParent(transform, false);
        }

        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.Grid = this;
                chunk.transform.SetParent(columns[x], false);
            }
        }
    }

    private void CreateCell()
    {
        cells = new HexCell[cellCountZ * cellCountX];
        cellSearchDatas = new HexCellSearchData[cells.Length];
        cellVisibility = new int[cells.Length];
        CellData = new HexCellData[cells.Length];
        CellPositions = new Vector3[cells.Length];
        UiRects = new TextMeshPro[cells.Length];


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
        //这个思路很好，想办法运用下20250630
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = new HexCell(this, i);
        CellPositions[i] = position;
        CellData[i].coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        TextMeshPro label = Instantiate(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        UiRects[i] = label;

        cell.Elevation = 0;
        cell.ColumnIndex = x / HexMetrics.chunkSizeX;
        RefreshPosition(i);

        if (wrapping)
        {
            cell.Explorable = z > 0 && z < cellCountZ - 1;
        }
        else
        {
            cell.Explorable = x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;
        }


        AddCellToChunk(x, z, cell);

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.Left, cells[i - 1]);
            if (wrapping && x == cellCountX - 1)
            {
                cell.SetNeighbor(HexDirection.Right, cells[i - x]);
            }
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
                else if (wrapping)
                {
                    cell.SetNeighbor(HexDirection.BottomLeft, cells[i - 1]);
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
                else if (wrapping)
                {
                    cell.SetNeighbor(
                        HexDirection.BottomRight, cells[i - cellCountX * 2 + 1]
                    );
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

    public void CenterMap(float xPosition)
    {
        int centerColumnIndex = (int)
            (xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));
        if (centerColumnIndex == currentCenterColumnIndex)
        {
            return;
        }

        currentCenterColumnIndex = centerColumnIndex;

        int minColumnIndex = centerColumnIndex - chunkCountX / 2;
        int maxColumnIndex = centerColumnIndex + chunkCountX / 2;

        Vector3 position;
        position.y = position.z = 0f;
        for (int i = 0; i < columns.Length; i++)
        {
            if (i < minColumnIndex)
            {
                position.x = chunkCountX * (HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
            }
            else if (i > maxColumnIndex)
            {
                position.x = chunkCountX * -(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
            }
            else
            {
                position.x = 0f;
            }
            columns[i].localPosition = position;
        }

    }

    public int GetCell(int xOffset, int zOffset)
    {
        return xOffset + zOffset * cellCountX;
    }

    public HexCell GetCell(int cellIndex)
    {
        return cells[cellIndex];
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

    public bool GetCellIdx(HexCoordinates coordinates, out int data)
    {
        int z = coordinates.Z;
        int x = coordinates.X + z / 2;
        if (z < 0 || z >= cellCountZ || x < 0 || x >= cellCountX)
        {
            data = -1;
            return false;
        }
        data = x + z * cellCountX;
        return true;
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;
        int x = coordinates.X + z / 2;
        if (z < 0 || z >= cellCountZ || x < 0 || x >= cellCountX)
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

    int currentPathFromIndex = -1, currentPathToIndex;

    public void FindPath(HexCell from, HexCell to, HexUnit hexUnit)
    {
        ClearPath();
        if (search(from, to, hexUnit))
        {
            currentPathFromIndex = from.Index;
            currentPathToIndex = to.Index;
        }
        ShowPath(hexUnit.Speed);
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Grid = this;
        // unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void MakeChildOfColumn(Transform child, int columnIndex)
    {
        child.SetParent(columns[columnIndex], false);
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
    private bool search(HexCell fromCell, HexCell to, HexUnit hexUnit)
    {
        searchPhase += 1;
        int searchFrontierPhase = searchPhase;

        PriorityQueue<int> frontier = new PriorityQueue<int>();

        cellSearchDatas[fromCell.Index] = new HexCellSearchData
        {
            searchPhase = searchFrontierPhase
        };
        frontier.Enqueue(fromCell.Index,
            cellSearchDatas[fromCell.Index].SearchPriority);

        int speed = hexUnit.Speed;
        while (frontier.Count > 0)
        {
            HexCell current = cells[frontier.Dequeue()];
            HexCellSearchData currentData = cellSearchDatas[current.Index];
            if (current == to)
            {
                return true;
            }

            int currentTurn = (currentData.distance - 1) / speed;

            //还是有很多重复计算
            for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
            {
                HexCell neighbor = current.GetNeighbor(this, d);
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

                int distance = currentData.distance + moveCost;
                int turn = (distance - 1) / speed;
                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (cellSearchDatas[neighbor.Index].searchPhase != searchFrontierPhase)
                {
                    cellSearchDatas[neighbor.Index] = new HexCellSearchData
                    {
                        searchPhase = searchFrontierPhase,
                        distance = distance,
                        pathFrom = current.Index,
                        heuristic = neighbor.Coordinates.DistanceTo(to.Coordinates),
                    };
                    frontier.Enqueue(neighbor.Index, cellSearchDatas[neighbor.Index].SearchPriority);
                }
                else if (distance < cellSearchDatas[neighbor.Index].distance)
                {
                    cellSearchDatas[neighbor.Index].distance = distance;
                    cellSearchDatas[neighbor.Index].pathFrom = current.Index;
                    frontier.Enqueue(neighbor.Index, cellSearchDatas[neighbor.Index].SearchPriority);
                }
            }
        }

        return false;
    }



    void ShowPath(int speed)
    {
        if (HasPath)
        {
            HexCell current = cells[currentPathToIndex];
            while (current.Index != currentPathFromIndex)
            {
                int turn = (cellSearchDatas[current.Index].distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = cells[cellSearchDatas[current.Index].pathFrom];
            }

            cells[currentPathFromIndex].EnableHighlight(Color.blue);
            cells[currentPathToIndex].EnableHighlight(Color.red);
        }
    }

    public void ClearPath()
    {
        if (HasPath)
        {
            HexCell current = cells[currentPathToIndex];
            while (current.Index != currentPathFromIndex)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = cells[cellSearchDatas[current.Index].pathFrom];
            }
            cells[currentPathFromIndex].DisableHighlight();
            cells[currentPathToIndex].DisableHighlight();
        }
        currentPathFromIndex = currentPathToIndex = -1;
    }

    public bool HasPath
    {
        get
        {
            return currentPathFromIndex > 0 && currentPathToIndex > 0;
        }
    }

    public List<int> GetPath()
    {
        if (!HasPath)
        {
            return null;
        }
        List<int> paths = ListPool<int>.Get();
        for (int to = currentPathToIndex; to != currentPathFromIndex; to = cellSearchDatas[to].pathFrom)
        {
            paths.Add(to);
        }
        paths.Add(currentPathFromIndex);
        paths.Reverse();
        return paths;
    }

    public void IncreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        foreach (HexCell cell in cells)
        {
            int val = cellVisibility[cell.Index] += 1;
            if (val == 1)
            {
                cell.MarkAsExplored();
                cellShaderData.RefreshVisibility(cell.Index);
            }

        }
        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility(HexCell fromCell, int range)
    {
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        foreach (HexCell cell in cells)
        {
            cellVisibility[cell.Index] -= 1;
            if (cellVisibility[cell.Index] == 0)
            {
                cellShaderData.RefreshVisibility(cell.Index);
            }
        }
        ListPool<HexCell>.Add(cells);
    }

    List<HexCell> GetVisibleCells(HexCell fromCell, int range)
    {
        range += fromCell.ViewElevation;
        List<HexCell> visibleCells = ListPool<HexCell>.Get();
        searchPhase += 1;
        int searchFrontierPhase = searchPhase;
        int searched = -searchFrontierPhase;
        PriorityQueue<int> frontier = new PriorityQueue<int>();

        cellSearchDatas[fromCell.Index] = new HexCellSearchData
        {
            searchPhase = searchFrontierPhase,
            pathFrom = cellSearchDatas[fromCell.Index].pathFrom,
        };
        frontier.Enqueue(fromCell.Index, cellSearchDatas[fromCell.Index].SearchPriority);
        HexCoordinates fromCoordinates = fromCell.Coordinates;
        while (frontier.Count > 0)
        {

            HexCell current = GetCell(frontier.Dequeue());
            cellSearchDatas[current.Index].searchPhase = searched;
            visibleCells.Add(current);
            //还是有很多重复计算
            for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
            {
                HexCell neighbor = current.GetNeighbor(this, d);
                if (!neighbor || cellSearchDatas[neighbor.Index].searchPhase == searched || !neighbor.Explorable)
                {
                    continue;
                }

                int distance = cellSearchDatas[current.Index].distance + 1;
                if (distance + neighbor.ViewElevation > range ||
                    distance > fromCoordinates.DistanceTo(neighbor.Coordinates)
                )
                {
                    continue;
                }

                if (cellSearchDatas[neighbor.Index].searchPhase != searchFrontierPhase)
                {
                    cellSearchDatas[neighbor.Index] = new HexCellSearchData
                    {
                        searchPhase = searchFrontierPhase,
                        distance = distance,
                        pathFrom = cellSearchDatas[neighbor.Index].pathFrom,
                        heuristic = 0
                    };
                    frontier.Enqueue(neighbor.Index, cellSearchDatas[neighbor.Index].SearchPriority);
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
        writer.Write(wrapping);
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
        int x = reader.ReadInt32();
        int z = reader.ReadInt32();
        bool wrapping = version >= 3 ? reader.ReadBoolean() : false;
        if (!CreateMap(x, z, wrapping))
        {
            return;
        }

        bool originModel = cellShaderData.ImmediateMode;
        cellShaderData.ImmediateMode = true;

        foreach (HexCell cell in cells)
        {
            cell.Load(this, reader);
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

    public bool ViewElevationChanged
    {
        get; set;
    }

    public bool IsCellVisible(int cellIndex) => cellVisibility[cellIndex] > 0;

    public void LateUpdate()
    {
        if (ViewElevationChanged)
        {
            ResetVisibility();
            ViewElevationChanged = false;
        }
    }

    public void ResetVisibility()
    {
        foreach (HexCell cell in cells)
        {
            if (cellVisibility[cell.Index] > 0)
            {
                cellVisibility[cell.Index] = 0;
                cellShaderData.RefreshVisibility(cell.Index);
            }
        }

        foreach (HexUnit unit in units)
        {
            IncreaseVisibility(unit.Location, unit.VisionRange);
        }
    }

    public void RefreshPosition(int index)
    {
        Vector3 position = CellPositions[index];
        position.y = CellData[index].Elevation * HexMetrics.elevationStep;
        position.y +=
            (HexMetrics.SampleNoise(position).y * 2f - 1f) *
            HexMetrics.elevationPerturbStrength;
        CellPositions[index] = position;

        Vector3 uiPosition = UiRects[index].rectTransform.localPosition;
        uiPosition.z = -position.y;
        UiRects[index].rectTransform.localPosition = uiPosition;
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