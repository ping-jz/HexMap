[System.Serializable]
public struct HexCellSearchData
{
	public int distance;

	public int pathFrom;

	public int heuristic;

	public int searchPhase;

	public readonly int SearchPriority => distance + heuristic;
}