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



	private HashSet<HexCoordinates> searchPhase;
	private PriorityQueue<HexCell> searchFrontier;

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
		CreateLand();
		SetTerrainType();

		Random.state = originalRandomState;
	}

	void CreateLand()
	{
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage);
		while (landBudget > 0)
		{
			int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax + 1);
			if (Random.value < sinkProbability)
			{
				landBudget = SinkTerrain(chunkSize, landBudget);
			}
			else
			{
				landBudget = RaiseTerrain(chunkSize, landBudget);
			}
		}
	}

	int RaiseTerrain(int chunkSize, int landBudget)
	{
		searchFrontier.Clear();
		searchPhase.Clear();

		HexCell firstCell = GetRandomCell();
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

	int SinkTerrain(int chunkSize, int landBudget)
	{
		searchFrontier.Clear();
		searchPhase.Clear();

		HexCell firstCell = GetRandomCell();
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

	HexCell GetRandomCell()
	{
		return grid.GetCell(Random.Range(0, cellCount));
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