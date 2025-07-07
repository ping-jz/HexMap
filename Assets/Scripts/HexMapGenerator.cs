using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

	[SerializeField]
	private HexGrid grid;
	[SerializeField]
	private bool useFixedSeed;
	[SerializeField]
	private int seed;
	[SerializeField, Range(0f, 0.5f)]
	private float jitterProbability = 0.25f;
	[SerializeField, Range(20, 200)]
	private int chunkSizeMin = 30;
	[SerializeField, Range(20, 200)]
	private int chunkSizeMax = 100;
	[SerializeField, Range(0.0f, 1.0f)]
	private float landPercentage = 0.292f, highRiseProbability = 0.25f;
	[SerializeField, Range(0f, 0.4f)]
	private float sinkProbability = 0.2f;
	[SerializeField, Range(1, 5)]
	private int waterLevel = 3;
	[SerializeField, Range(-4, 0)]
	private int elevationMinimum = -2;
	[SerializeField, Range(6, 10)]
	private int elevationMaximum = 8;
	[SerializeField, Range(0, 10)]
	private int mapBorderX = 5, mapBorderZ = 5;
	[SerializeField, Range(1, 3)]
	private int regionCount;
	[SerializeField, Range(0, 10)]
	private int regionBorder = 5;
	[SerializeField, Range(0.0f, 1.0f)]
	private float erosionPercentage = 0.5f;
	[SerializeField, Range(0f, 1f)]
	private float evaporation = 0.5f,
		precipitationFactor = 0.25f,
		evaporationFactor = 0.5f,
		runoffFactor = 0.25f,
		seepageFactor = 0.125f
	;

	private HashSet<HexCoordinates> searchPhase;
	private PriorityQueue<HexCell> searchFrontier;
	private List<MapRegin> mapRegins;
	private List<ClimateData> climate;

	private int cellCount;

	public void GenerateMap(int x, int z)
	{
		Random.State originalRandomState = Random.state;
		if (!useFixedSeed)
		{
			seed = Random.Range(0, int.MaxValue);
			seed ^= (int)System.DateTime.Now.Ticks;
			seed ^= (int)Time.unscaledTime;
			seed &= int.MaxValue;
		}
		Random.InitState(seed);

		if (searchPhase == null)
		{
			searchPhase = new HashSet<HexCoordinates>();
		}
		if (searchFrontier == null)
		{
			searchFrontier = new PriorityQueue<HexCell>();
		}

		cellCount = x * z;
		grid.CreateMap(x, z);


		for (int i = 0; i < cellCount; i++)
		{
			grid.GetCell(i).WaterLevel = waterLevel;
		}
		CreateRegion(x, z);
		CreateLand();
		ErodeLand();
		CreateClimate();
		SetTerrainType();

		Random.state = originalRandomState;
	}

	private void CreateRegion(int x, int z)
	{
		if (mapRegins == null)
		{
			mapRegins = new List<MapRegin>();
		}

		mapRegins.Clear();
		switch (regionCount)
		{
			case 2:
				{
					MapRegin one;
					one.xMin = mapBorderX;
					one.xMax = x / 2 - regionBorder;
					one.zMin = mapBorderZ;
					one.zMax = z - mapBorderZ;
					mapRegins.Add(one);

					MapRegin two;
					two.xMin = x / 2 + regionBorder;
					two.xMax = x - mapBorderX;
					two.zMin = mapBorderZ;
					two.zMax = z - mapBorderZ;
					mapRegins.Add(two);

				}
				break;
			case 3:
				{
					MapRegin one;
					one.xMin = mapBorderX;
					one.xMax = x / 3 - regionBorder;
					one.zMin = mapBorderZ;
					one.zMax = z - mapBorderZ;
					mapRegins.Add(one);

					MapRegin two;
					two.xMin = x / 3 + regionBorder;
					two.xMax = x * 2 / 3 - regionBorder;
					two.zMin = mapBorderZ;
					two.zMax = z - mapBorderZ;
					mapRegins.Add(two);

					MapRegin three;
					three.xMin = x * 2 / 3 + regionBorder;
					three.xMax = x - mapBorderX;
					three.zMin = mapBorderZ;
					three.zMax = z - mapBorderZ;
					mapRegins.Add(three);

				}
				break;
			default:
				{
					MapRegin mapRegin;
					mapRegin.xMin = mapBorderX;
					mapRegin.xMax = x - mapBorderX;
					mapRegin.zMin = mapBorderZ;
					mapRegin.zMax = z - mapBorderZ;
					mapRegins.Add(mapRegin);
				}
				break;
		}
	}

	void CreateLand()
	{
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage);
		for (int guard = 0; guard < 10000; guard++)
		{
			bool sink = Random.value < sinkProbability;
			foreach (MapRegin mapRegin in mapRegins)
			{
				int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax + 1);
				if (sink)
				{
					landBudget = SinkTerrain(chunkSize, landBudget, mapRegin);
				}
				else
				{
					landBudget = RaiseTerrain(chunkSize, landBudget, mapRegin);
					if (landBudget == 0)
					{
						return;
					}
				}
			}
		}

		if (landBudget > 0)
		{
			Debug.LogWarning($"Failed to use up  {landBudget} land budget.");
		}
	}

	int RaiseTerrain(int chunkSize, int landBudget, MapRegin mapRegin)
	{
		HexCell firstCell = GetRandomCell(mapRegin);
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell, firstCell.SearchPriority);
		HexCoordinates center = firstCell.Coordinates;

		int rise = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			int originalElevation = current.Elevation;
			int elevation = originalElevation + rise;
			if (elevation > elevationMaximum)
			{
				continue;
			}
			current.Elevation = elevation;
			if (originalElevation < waterLevel &&
				current.Elevation >= waterLevel)
			{
				landBudget -= 1;
				if (landBudget == 0)
				{
					break;
				}
			}
			size += 1;

			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && !searchPhase.Contains(neighbor.Coordinates))
				{
					searchPhase.Add(neighbor.Coordinates);
					neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
					neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighbor, neighbor.SearchPriority);
				}
			}
		}

		searchFrontier.Clear();
		searchPhase.Clear();
		return landBudget;
	}

	int SinkTerrain(int chunkSize, int landBudget, MapRegin mapRegin)
	{
		searchFrontier.Clear();
		searchPhase.Clear();

		HexCell firstCell = GetRandomCell(mapRegin);
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell, firstCell.SearchPriority);
		HexCoordinates center = firstCell.Coordinates;

		int sink = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			int originalElevation = current.Elevation;
			int elevation = originalElevation - sink;
			if (elevation < elevationMinimum)
			{
				continue;
			}
			current.Elevation = elevation;
			if (originalElevation >= waterLevel &&
				current.Elevation < waterLevel)
			{
				landBudget += 1;
			}
			size += 1;

			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && !searchPhase.Contains(neighbor.Coordinates))
				{
					searchPhase.Add(neighbor.Coordinates);
					neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
					neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighbor, neighbor.SearchPriority);
				}
			}
		}

		searchFrontier.Clear();
		searchPhase.Clear();
		return landBudget;
	}

	HexCell GetRandomCell(MapRegin mapRegion)
	{
		return grid.GetCell(Random.Range(mapRegion.xMin, mapRegion.xMax), Random.Range(mapRegion.zMin, mapRegion.zMax));
	}

	void CreateClimate()
	{
		if (climate == null)
		{
			climate = new List<ClimateData>();
		}
		climate.Clear();
		ClimateData initialData = new ClimateData();
		for (int i = 0; i < cellCount; i++)
		{
			climate.Add(initialData);
		}

		for (int cycle = 0; cycle < 40; cycle++)
		{
			for (int i = 0; i < cellCount; i++)
			{
				EvolveClimate(i);
			}
		}
	}

	void EvolveClimate(int cellIndex)
	{
		HexCell cell = grid.GetCell(cellIndex);
		ClimateData cellClimate = climate[cellIndex];

		if (cell.IsUnderwater)
		{
			cellClimate.clouds += evaporation;
		}
		else
		{
			float evaporation = cellClimate.moisture * evaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}

		if (0.0 < cellClimate.clouds)
		{
			float precipitation = cellClimate.clouds * precipitationFactor;
			cellClimate.clouds -= precipitation;
			cellClimate.moisture += precipitation;

			float cloudDispersal = cellClimate.clouds * (1f / 6f);
			cellClimate.clouds = 0f;
			float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
			float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				HexCell neighbor = cell.GetNeighbor(d);
				if (!neighbor)
				{
					continue;
				}
				ClimateData neighborClimate = climate[neighbor.Index];
				neighborClimate.clouds += cloudDispersal;

				int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
				if (elevationDelta < 0)
				{
					cellClimate.moisture -= runoff;
					neighborClimate.moisture += runoff;
				}
				else if (elevationDelta == 0)
				{
					cellClimate.moisture -= seepage;
					neighborClimate.moisture += seepage;
				}

				climate[neighbor.Index] = neighborClimate;
			}
		}

		climate[cellIndex] = cellClimate;
	}

	void SetTerrainType()
	{
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			if (!cell.IsUnderwater)
			{
				cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
			}
			cell.SetMapData(climate[i].moisture);
		}
	}

	void ErodeLand()
	{
		List<HexCell> erodibleCells = ListPool<HexCell>.Get();
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			if (IsErodible(cell))
			{
				erodibleCells.Add(cell);
			}
		}

		int erodibleCount = (int)(erodibleCells.Count * (1.0 - erosionPercentage));
		while (erodibleCount < erodibleCells.Count)
		{
			int index = Random.Range(0, erodibleCells.Count);
			HexCell cell = erodibleCells[index];
			HexCell targetCell = GetErosionTarget(cell);

			cell.Elevation -= 1;
			targetCell.Elevation += 1;
			RefreshCellWithDependents(cell);

			if (!IsErodible(cell))
			{
				erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
				erodibleCells.RemoveAt(erodibleCells.Count - 1);
			}

			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				HexCell neighbor = cell.GetNeighbor(d);
				if (neighbor &&
					//这个编程技巧不错，复用了{IsErodible}的判断，减少erodibleCells.Contains的调用
					neighbor.Elevation == cell.Elevation + 2 &&
					!erodibleCells.Contains(neighbor))
				{
					erodibleCells.Add(neighbor);
				}
			}

			if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
			{
				erodibleCells.Add(targetCell);
			}

			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				HexCell neighbor = targetCell.GetNeighbor(d);
				if (
					neighbor &&
					neighbor != cell &&
					//这个编程技巧不错，复用了{IsErodible}的判断，减少erodibleCells.Contains的调用
					neighbor.Elevation == targetCell.Elevation + 1 &&
					!IsErodible(neighbor)
				)
				{
					erodibleCells.Remove(neighbor);
				}
			}
		}

		ListPool<HexCell>.Add(erodibleCells);
	}

	private void RefreshCellWithDependents(HexCell cell)
	{
		refrechCells(cell);
		refrechCells(cell.Neighbors);
	}

	void refrechCells(HexCell cell)
	{

		HexGridChunk chunk = grid.GetChunk(cell);
		if (chunk)
		{
			chunk.Refresh();
		}
		if (cell.Unit)
		{
			cell.Unit.ValidateLocation();
		}
	}

	void refrechCells(IEnumerable<HexCell> cells)
	{
		foreach (HexCell cell in cells)
		{
			if (!cell)
			{
				continue;
			}

			refrechCells(cell);
		}
	}

	bool IsErodible(HexCell cell)
	{
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation)
			{
				return true;
			}
		}
		return false;
	}

	HexCell GetErosionTarget(HexCell cell)
	{
		List<HexCell> erodibleCells = ListPool<HexCell>.Get();
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
		{
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation)
			{
				erodibleCells.Add(neighbor);
			}
		}
		HexCell target = erodibleCells[Random.Range(0, erodibleCells.Count)];
		ListPool<HexCell>.Add(erodibleCells);
		return target;
	}
}

struct MapRegin
{
	public int xMin, xMax, zMin, zMax;
}

struct ClimateData
{
	public float clouds, moisture;
}