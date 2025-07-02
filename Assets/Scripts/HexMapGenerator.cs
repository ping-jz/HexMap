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

	private HashSet<HexCoordinates> searchPhase;
	private PriorityQueue<HexCell> searchFrontier;
	private List<MapRegin> mapRegins;

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

	void SetTerrainType()
	{
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			if (!cell.IsUnderwater)
			{
				cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
			}
		}
	}
}

struct MapRegin
{
	public int xMin, xMax, zMin, zMax;
}