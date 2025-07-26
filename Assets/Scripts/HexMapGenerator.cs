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
	private PriorityQueue<int> searchFrontier;
	private List<MapRegin> mapRegins;
	private List<ClimateData> climate = new List<ClimateData>();
	private List<ClimateData> nextClimate = new List<ClimateData>();

	private int cellCount;

	public void GenerateMap(int x, int z, bool wrapping)
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
			searchFrontier = new PriorityQueue<int>();
		}

		cellCount = x * z;
		grid.CreateMap(x, z, wrapping);


		for (int i = 0; i < cellCount; i++)
		{
			grid.CellData[i].values.waterLevel = waterLevel;
		}
		CreateRegion(x, z);
		CreateLand();
		ErodeLand();
		CreateClimate();
		CreateRivers();
		SetTerrainType();

		for (int i = 0; i < cellCount; i++)
		{
			grid.GetCell(i).RefreshAll();
		}

		Random.state = originalRandomState;
	}

	private void CreateRegion(int x, int z)
	{
		if (mapRegins == null)
		{
			mapRegins = new List<MapRegin>();
		}

		mapRegins.Clear();
		int borderX = grid.Wrapping ? regionBorder : mapBorderX;
		switch (regionCount)
		{
			case 2:
				{
					MapRegin one;
					one.xMin = borderX;
					one.xMax = x / 2 - regionBorder;
					one.zMin = mapBorderZ;
					one.zMax = z - mapBorderZ;
					mapRegins.Add(one);

					MapRegin two;
					two.xMin = x / 2 + regionBorder;
					two.xMax = x - borderX;
					two.zMin = mapBorderZ;
					two.zMax = z - mapBorderZ;
					mapRegins.Add(two);

				}
				break;
			case 3:
				{
					MapRegin one;
					one.xMin = borderX;
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
					three.xMax = x - borderX;
					three.zMin = mapBorderZ;
					three.zMax = z - mapBorderZ;
					mapRegins.Add(three);

				}
				break;
			default:
				{
					if (grid.Wrapping)
					{
						borderX = 0;
					}
					MapRegin mapRegin;
					mapRegin.xMin = borderX;
					mapRegin.xMax = x - borderX;
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
		int firstCellIdx = GetRandomCellIdx(mapRegin);
		grid.SearchData[firstCellIdx] = new HexCellSearchData();
		searchFrontier.Enqueue(firstCellIdx, grid.SearchData[firstCellIdx].SearchPriority);
		HexCoordinates center = grid.CellData[firstCellIdx].coordinates;

		int rise = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			int idx = searchFrontier.Dequeue();
			int originalElevation = grid.CellData[idx].Elevation;
			int newElevation = originalElevation + rise;
			if (newElevation > elevationMaximum)
			{
				continue;
			}

			grid.CellData[idx].values.elevation = newElevation;
			if (originalElevation < waterLevel &&
				grid.CellData[idx].Elevation >= waterLevel)
			{
				landBudget -= 1;
				if (landBudget == 0)
				{
					break;
				}
			}
			size += 1;

			HexCoordinates coordinates = grid.CellData[idx].coordinates;
			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{

				if (grid.GetCellIdx(coordinates.Step(d), out int neighborIdx) &&
					searchPhase.Add(grid.CellData[neighborIdx].coordinates))
				{
					grid.SearchData[neighborIdx] = new HexCellSearchData
					{
						distance = grid.CellData[neighborIdx].coordinates.DistanceTo(center),
						heuristic = Random.value < jitterProbability ? 1 : 0
					};
					searchFrontier.Enqueue(neighborIdx, grid.SearchData[neighborIdx].SearchPriority);
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

		int firstCellIdx = GetRandomCellIdx(mapRegin);
		grid.SearchData[firstCellIdx] = new HexCellSearchData();
		searchFrontier.Enqueue(firstCellIdx, grid.SearchData[firstCellIdx].SearchPriority);
		HexCoordinates center = grid.CellData[firstCellIdx].coordinates;

		int sink = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0)
		{
			int idx = searchFrontier.Dequeue();
			int originalElevation = grid.CellData[idx].Elevation;
			int elevation = originalElevation - sink;
			if (elevation < elevationMinimum)
			{
				continue;
			}
			grid.CellData[idx].values.elevation = elevation;
			if (originalElevation >= waterLevel &&
				grid.CellData[idx].Elevation < waterLevel)
			{
				landBudget += 1;
			}
			size += 1;

			HexCoordinates coordinates = grid.CellData[idx].coordinates;
			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{

				if (grid.GetCellIdx(coordinates.Step(d), out int neighborIdx) &&
						searchPhase.Add(grid.CellData[neighborIdx].coordinates))
				{
					grid.SearchData[neighborIdx].distance = grid.CellData[neighborIdx]
																.coordinates.DistanceTo(center);
					grid.SearchData[neighborIdx].heuristic = Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighborIdx, grid.SearchData[neighborIdx].SearchPriority);
				}
			}
		}

		searchFrontier.Clear();
		searchPhase.Clear();
		return landBudget;
	}

	int GetRandomCellIdx(MapRegin mapRegion)
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
		HexCellData cell = grid.CellData[cellIndex];
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
				if (!grid.GetCellIdx(
					cell.coordinates.Step(d), out int neighborIndex))
				{
					continue;
				}
				ClimateData neighborClimate = nextClimate[neighborIndex];
				if (d == windDirection)
				{
					neighborClimate.clouds += cloudDispersal * windStrength;
				}
				else
				{
					neighborClimate.clouds += cloudDispersal;
				}


				int elevationDelta = grid.CellData[neighborIndex].ViewElevation - cell.ViewElevation;
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

				nextClimate[neighborIndex] = neighborClimate;
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
		List<int> riverOrigins = ListPool<int>.Get();
		int landCells = 0;
		for (int i = 0; i < cellCount; i++)
		{
			HexCellData cell = grid.CellData[i];
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
				riverOrigins.Add(i);
				riverOrigins.Add(i);
			}
			if (weight > 0.5f)
			{
				riverOrigins.Add(i);
			}
			if (weight > 0.25f)
			{
				riverOrigins.Add(i);
			}
		}

		int riverBudget = Mathf.RoundToInt(landCells * riverPercentage);
		while (riverBudget > 0 && riverOrigins.Count > 0)
		{
			int index = Random.Range(0, riverOrigins.Count);
			int lastIndex = riverOrigins.Count - 1;
			int originIdx = riverOrigins[index];
			HexCellData origin = grid.CellData[originIdx];
			riverOrigins[index] = riverOrigins[lastIndex];
			riverOrigins.RemoveAt(lastIndex);

			if (origin.IsUnderwater)
			{
				continue;
			}

			bool isValidOrigin = true;
			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				if (grid.GetCellIdx(
					origin.coordinates.Step(d), out int neighborIndex) &&
					(grid.CellData[neighborIndex].HasRiver ||
						grid.CellData[neighborIndex].IsUnderwater))
				{
					isValidOrigin = false;
					break;
				}
			}
			if (isValidOrigin)
			{
				riverBudget -= CreateRiver(originIdx);
			}

		}


		ListPool<int>.Add(riverOrigins);
	}

	int CreateRiver(int originIdx)
	{
		int length = 1;
		List<HexDirection> flowDirections = ListPool<HexDirection>.Get();
		int cellIndex = originIdx;
		HexCellData cell = grid.CellData[cellIndex];
		HexDirection direction = HexDirection.TopRight;
		while (!cell.IsUnderwater)
		{
			flowDirections.Clear();
			int minNeighborElevation = int.MaxValue;
			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				if (!grid.GetCellIdx(cell.coordinates.Step(d), out int neighborIndex))
				{
					continue;
				}
				HexCellData neighbor = grid.CellData[neighborIndex];

				if (neighbor.Elevation < minNeighborElevation)
				{
					minNeighborElevation = neighbor.Elevation;
				}

				if (neighborIndex == originIdx || neighbor.HasIncomingRiver)
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
					grid.CellData[cellIndex].flags = cell.flags.WithRiverOut(d);
					grid.CellData[neighborIndex].flags = neighbor.flags.WithRiverIn(d.Opposite());
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
					cell.values.waterLevel = minNeighborElevation;
					if (minNeighborElevation == cell.Elevation)
					{
						cell.values.elevation = minNeighborElevation - 1;
					}
					grid.CellData[cellIndex].values = cell.values;
				}
				break;
			}

			direction = flowDirections[Random.Range(0, flowDirections.Count)];
			//cell.SetOutgoingRiver(direction);
			cell.flags = cell.flags.WithRiverOut(direction);
			grid.GetCellIdx(cell.coordinates.Step(direction), out int outIndex);
			grid.CellData[outIndex].flags =
				grid.CellData[outIndex].flags.WithRiverIn(direction.Opposite());
			length += 1;

			if (minNeighborElevation >= cell.Elevation &&
				Random.value < extraLakeProbability)
			{
				cell.values.waterLevel = cell.Elevation;
				cell.values.elevation -= 1;
			}

			grid.CellData[cellIndex] = cell;
			cellIndex = outIndex;
			cell = grid.CellData[cellIndex];
		}
		ListPool<HexDirection>.Add(flowDirections);
		return length;
	}

	float DeterminTemperature(int cellIndex, HexCellData cell, int channel)
	{
		Vector3 position = grid.CellPositions[cellIndex];
		float latitude = (float)cell.coordinates.Z / grid.CellCountZ;
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
		temperature += (HexMetrics.SampleNoise(position * 0.1f)[channel] * 2f - 1f) * temperatureJitter;
		return temperature;
	}

	void SetTerrainType()
	{
		int rockDesertElevation =
			elevationMaximum - (elevationMaximum - waterLevel) / 2;
		int channel = Random.Range(0, 4);
		for (int i = 0; i < cellCount; i++)
		{
			HexCellData cell = grid.CellData[i];
			float moisture = climate[i].moisture;
			float temperature = DeterminTemperature(i, cell, channel);
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

				grid.CellData[i].values.terrainTypeIndex = (byte)cellBiome.terrain;
				grid.CellData[i].values.plantLevel = (byte)cellBiome.plant;
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
						if (!grid.GetCellIdx(cell.coordinates.Step(d), out int neighborIdx))
						{
							continue;
						}
						int delta = grid.CellData[neighborIdx].Elevation - cell.WaterLevel;
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
				grid.CellData[i].values.terrainTypeIndex = (byte)terrain;
			}
			//cell.SetMapData(grid, moisture);
		}
	}

	void ErodeLand()
	{
		List<int> erodibleIndices = ListPool<int>.Get();
		for (int i = 0; i < cellCount; i++)
		{
			if (IsErodible(i, grid.CellData[i].Elevation))
			{
				erodibleIndices.Add(i);
			}
		}

		int erodibleCount = (int)(erodibleIndices.Count * (1.0 - erosionPercentage));
		while (erodibleCount < erodibleIndices.Count)
		{
			int index = Random.Range(0, erodibleIndices.Count);
			int cellIdx = erodibleIndices[index];
			HexCellData cell = grid.CellData[cellIdx];
			int targetCellIdx = GetErosionTarget(cellIdx, cell.Elevation);

			grid.CellData[cellIdx].values.elevation = cell.values.elevation -= 1;

			HexCellData targetCell = grid.CellData[targetCellIdx];
			grid.CellData[targetCellIdx].values.elevation = targetCell.values.elevation += 1;

			if (!IsErodible(cellIdx, cell.Elevation))
			{
				int lastIndex = erodibleIndices.Count - 1;
				erodibleIndices[index] = erodibleIndices[lastIndex];
				erodibleIndices.RemoveAt(lastIndex);
			}

			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				if (grid.GetCellIdx(cell.coordinates.Step(d), out int neighborIdx) &&
					//这个编程技巧不错，复用了{IsErodible}的判断，减少erodibleCells.Contains的调用
					grid.CellData[neighborIdx].Elevation == cell.Elevation + 2 &&
					!erodibleIndices.Contains(neighborIdx))
				{
					erodibleIndices.Add(neighborIdx);
				}
			}

			if (IsErodible(targetCellIdx, targetCell.Elevation) && !erodibleIndices.Contains(targetCellIdx))
			{
				erodibleIndices.Add(targetCellIdx);
			}

			for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
			{
				if (
					grid.GetCellIdx(targetCell.coordinates.Step(d), out int neighborIdx) &&
					neighborIdx != cellIdx &&
					//这个编程技巧不错，复用了{IsErodible}的判断，减少erodibleCells.Contains的调用
					grid.CellData[neighborIdx].Elevation == targetCell.Elevation + 1 &&
					!IsErodible(neighborIdx, grid.CellData[neighborIdx].Elevation)
				)
				{
					erodibleIndices.Remove(neighborIdx);
				}
			}
		}

		ListPool<int>.Add(erodibleIndices);
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
	bool IsErodible(int cellIdx, int elevation)
	{
		int erodibleElevation = elevation - 2;
		HexCoordinates coordinates = grid.CellData[cellIdx].coordinates;
		for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
		{
			if (grid.GetCellIdx(coordinates.Step(d), out int neighborIdx) &&
				grid.CellData[neighborIdx].Elevation <= erodibleElevation)
			{
				return true;
			}
		}
		return false;
	}

	int GetErosionTarget(int cellIdx, int elevation)
	{
		List<int> erodibleCells = ListPool<int>.Get();
		int erodibleElevation = elevation - 2;
		HexCoordinates coordinates = grid.CellData[cellIdx].coordinates;
		for (HexDirection d = HexDirection.TopRight; d <= HexDirection.TopLeft; d++)
		{
			if (grid.GetCellIdx(coordinates.Step(d), out int neighborIdx) &&
				grid.CellData[neighborIdx].Elevation <= erodibleElevation)
			{
				erodibleCells.Add(neighborIdx);
			}
		}
		int target = erodibleCells[Random.Range(0, erodibleCells.Count)];
		ListPool<int>.Add(erodibleCells);
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

