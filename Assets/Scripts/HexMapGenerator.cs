using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

	[SerializeField]
	private HexGrid grid;
	[SerializeField, Range(0f, 0.5f)]
	private float jitterProbability = 0.25f;
	[SerializeField, Range(20, 200)]
	private int chunkSizeMin = 30;
	[SerializeField, Range(20, 200)]
	private int chunkSizeMax = 100;
	[SerializeField, Range(0.0f, 1.0f)]
	private float landPercentage = 0.292f;

	private HashSet<HexCoordinates> searchPhase;
	private PriorityQueue<HexCell> searchFrontier;

	private int cellCount;

	public void GenerateMap(int x, int z)
	{
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

		CreateLand();
	}

	void CreateLand()
	{
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage);
		while (landBudget > 0)
		{
			landBudget = RaiseTerrain(Random.Range(chunkSizeMin, chunkSizeMax + 1), landBudget);
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

		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			HexCell current = searchFrontier.Dequeue();
			if (current.TerrainTypeIndex == 0)
			{
				current.TerrainTypeIndex = 1;
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

	HexCell GetRandomCell()
	{
		return grid.GetCell(Random.Range(0, cellCount));
	}
}