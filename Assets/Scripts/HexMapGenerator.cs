using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

	static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };
	static float[] moistureBands = { 0.12f, 0.28f, 0.85f };
	//y轴温度，x轴水汽。如果从数组结构的来看，就互换
	static Biome[] biomes = {
		new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
		new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
		new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
		new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
	};

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
		seepageFactor = 0.125f,
		startingMoisture = 0.1f,
		extraLakeProbability = 0.25f,
		lowTemperature = 0f,
		highTemperature = 1f,
		temperatureJitter = 0.1f
	;

	private HexDirection windDirection = HexDirection.TopLeft;

	[SerializeField, Range(1f, 10f)]
	private float windStrength = 4f;
	[SerializeField, Range(0f, 0.2f)]
	private float riverPercentage = 0.1f;
	[SerializeField]
	private HemisphereMode hemisphere;

	private HashSet<HexCoordinates> searchPhase;
	private PriorityQueue<HexCell> searchFrontier;
	private List<MapRegin> mapRegins;
	private List<ClimateData> climate = new List<ClimateData>();
	private List<ClimateData> nextClimate = new List<ClimateData>();

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
		CreateRivers();
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
		//20250707。为什么要两个，应该后续某种优化要用吧。
		climate.Clear();
		nextClimate.Clear();

		ClimateData initialData = new ClimateData();
		initialData.moisture = startingMoisture;
		ClimateData nextInitialData = new ClimateData();
		for (int i = 0; i < cellCount; i++)
		{
			climate.Add(initialData);
			nextClimate.Add(nextInitialData);
		}

		for (int cycle = 0; cycle < 40; cycle++)
		{
			for (int i = 0; i < cellCount; i++)
			{
				EvolveClimate(i);
			}
			List<ClimateData> swap = climate;
			climate = nextClimate;
			nextClimate = swap;
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

			float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
			if (cellClimate.clouds > cloudMaximum)
			{
				cellClimate.moisture += cellClimate.clouds - cloudMaximum;
				cellClimate.clouds = cloudMaximum;
			}

			//为什么的云的扩散方向和风吹的方向相反
			//20250707先用的风吹的方向，相通了再用
			HexDirection mainDispersalDirection = windDirection.Opposite();
			float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
			float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
			float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				HexCell neighbor = cell.GetNeighbor(d);
				if (!neighbor)
				{
					continue;
				}
				ClimateData neighborClimate = nextClimate[neighbor.Index];
				if (d == windDirection)
				{
					neighborClimate.clouds += cloudDispersal * windStrength;
				}
				else
				{
					neighborClimate.clouds += cloudDispersal;
				}


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

				nextClimate[neighbor.Index] = neighborClimate;
			}
		}

		ClimateData nextCellClimate = nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;
		if (nextCellClimate.moisture > 1f)
		{
			nextCellClimate.moisture = 1f;
		}
		nextClimate[cellIndex] = nextCellClimate;
		climate[cellIndex] = new ClimateData();
	}

	void CreateRivers()
	{
		List<HexCell> riverOrigins = ListPool<HexCell>.Get();
		int landCells = 0;
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			if (cell.IsUnderwater)
			{
				continue;
			}
			landCells += 1;

			ClimateData data = climate[i];
			float weight = data.moisture * (cell.Elevation - waterLevel) /
				(elevationMaximum - waterLevel);
			if (weight > 0.75f)
			{
				riverOrigins.Add(cell);
				riverOrigins.Add(cell);
			}
			if (weight > 0.5f)
			{
				riverOrigins.Add(cell);
			}
			if (weight > 0.25f)
			{
				riverOrigins.Add(cell);
			}
		}

		int riverBudget = Mathf.RoundToInt(landCells * riverPercentage);
		while (riverBudget > 0 && riverOrigins.Count > 0)
		{
			int index = Random.Range(0, riverOrigins.Count);
			int lastIndex = riverOrigins.Count - 1;
			HexCell origin = riverOrigins[index];
			riverOrigins[index] = riverOrigins[lastIndex];
			riverOrigins.RemoveAt(lastIndex);

			if (!origin.IsUnderwater)
			{
				bool isValidOrigin = true;
				foreach (HexCell neighbor in origin.Neighbors)
				{
					if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater))
					{
						isValidOrigin = false;
						break;
					}
				}
				if (isValidOrigin)
				{
					riverBudget -= CreateRiver(origin);
				}
			}
		}


		ListPool<HexCell>.Add(riverOrigins);
	}

	int CreateRiver(HexCell origin)
	{
		int length = 1;
		HexCell cell = origin;
		List<HexDirection> flowDirections = ListPool<HexDirection>.Get();
		HexDirection direction = HexDirection.TopRight;
		while (!cell.IsUnderwater)
		{
			flowDirections.Clear();
			int minNeighborElevation = int.MaxValue;
			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				HexCell neighbor = cell.GetNeighbor(d);
				if (!neighbor)
				{
					continue;
				}

				if (neighbor.Elevation < minNeighborElevation)
				{
					minNeighborElevation = neighbor.Elevation;
				}

				if (neighbor == origin || neighbor.HasIncomingRiver)
				{
					continue;
				}

				int delta = neighbor.Elevation - cell.Elevation;
				if (delta > 0)
				{
					continue;
				}
				else if (delta < 0)
				{
					flowDirections.Add(d);
					flowDirections.Add(d);
					flowDirections.Add(d);
				}


				if (neighbor.HasOutgoingRiver)
				{
					cell.SetOutgoingRiver(d);
					return length;
				}

				//防止急转弯
				if (length == 1 || (d != direction.Next2() && d != direction.Previous2()))
				{
					flowDirections.Add(d);
				}

				flowDirections.Add(d);
			}

			if (flowDirections.Count == 0)
			{
				if (length == 1)
				{
					return 0;
				}

				if (minNeighborElevation >= cell.Elevation)
				{
					cell.WaterLevel = minNeighborElevation;
					if (minNeighborElevation == cell.Elevation)
					{
						cell.Elevation = minNeighborElevation - 1;
					}
				}
				RefreshCellWithDependents(cell);
				break;
			}

			if (minNeighborElevation >= cell.Elevation &&
				Random.value < extraLakeProbability)
			{
				cell.WaterLevel = cell.Elevation;
				cell.Elevation -= 1;
			}

			direction = flowDirections[Random.Range(0, flowDirections.Count)];
			cell.SetOutgoingRiver(direction);
			length += 1;
			cell = cell.GetNeighbor(direction);
			RefreshCellWithDependents(cell);
		}
		ListPool<HexDirection>.Add(flowDirections);
		return length;
	}

	float DeterminTemperature(HexCell cell, int channel)
	{
		float latitude = (float)cell.Coordinates.Z / grid.CellCountZ;
		if (hemisphere == HemisphereMode.Both)
		{
			latitude *= 2;
			if (latitude > 1f)
			{
				latitude = 2f - latitude;
			}
		}
		else if (hemisphere == HemisphereMode.North)
		{
			latitude = 1f - latitude;
		}

		float temperature = Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

		temperature *= 1f - (cell.ViewElevation - waterLevel) / (elevationMaximum - waterLevel + 1f);
		temperature += (HexMetrics.SampleNoise(cell.Position * 0.1f)[channel] * 2f - 1f) * temperatureJitter;
		return temperature;
	}

	void SetTerrainType()
	{
		int rockDesertElevation =
			elevationMaximum - (elevationMaximum - waterLevel) / 2;
		int channel = Random.Range(0, 4);
		for (int i = 0; i < cellCount; i++)
		{
			HexCell cell = grid.GetCell(i);
			float moisture = climate[i].moisture;
			float temperature = DeterminTemperature(cell, channel);
			//20250711这段地形随机代码，我没能彻底理解。因为缺乏对自然的足够理解
			if (!cell.IsUnderwater)
			{
				int t = 0;
				for (; t < temperatureBands.Length; t++)
				{
					if (temperature < temperatureBands[t])
					{
						break;
					}
				}

				int m = 0;
				for (; m < moistureBands.Length; m++)
				{
					if (moisture < moistureBands[m])
					{
						break;
					}
				}

				Biome cellBiome = biomes[t * 4 + m];
				if (cellBiome.terrain == 0)
				{
					if (cell.Elevation >= rockDesertElevation)
					{
						cellBiome.terrain = 3;
					}
				}
				else if (cell.Elevation == elevationMaximum)
				{
					cellBiome.terrain = 4;
				}

				if (cellBiome.terrain == 4)
				{
					cellBiome.plant = 0;
				}
				else if (cellBiome.plant < 3 && cell.HasRiver)
				{
					cellBiome.plant += 1;
				}

				cell.TerrainTypeIndex = cellBiome.terrain;
				cell.PlantLevel = cellBiome.plant;
				refrechCells(cell);
			}
			else
			{
				int terrain;
				if (cell.Elevation == waterLevel - 1)
				{
					int cliffs = 0, slopes = 0;
					for (
						HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++
					)
					{
						HexCell neighbor = cell.GetNeighbor(d);
						if (!neighbor)
						{
							continue;
						}
						int delta = neighbor.Elevation - cell.WaterLevel;
						if (delta == 0)
						{
							slopes += 1;
						}
						else if (delta > 0)
						{
							cliffs += 1;
						}
					}

					if (cliffs + slopes > 3)
					{
						terrain = 1;
					}
					else if (cliffs > 0)
					{
						terrain = 3;
					}
					else if (slopes > 0)
					{
						terrain = 0;
					}
					else
					{
						terrain = 1;
					}
				}
				else if (cell.Elevation >= waterLevel)
				{
					terrain = 1;
				}
				else if (cell.Elevation < 0)
				{
					terrain = 3;
				}
				else
				{
					terrain = 2;
				}

				if (terrain == 1 && temperature < temperatureBands[0])
				{
					terrain = 2;
				}
				cell.TerrainTypeIndex = terrain;
			}
			cell.SetMapData(moisture);
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

enum HemisphereMode
{
	Both, North, South
}

struct Biome
{
	public int terrain, plant;

	public Biome(int terrain, int plant)
	{
		this.terrain = terrain;
		this.plant = plant;
	}
}

