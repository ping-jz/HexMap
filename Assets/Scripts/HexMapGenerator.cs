using UnityEngine;

public class HexMapGenerator : MonoBehaviour {

    [SerializeField]
    private HexGrid grid;

	public void GenerateMap(int x, int z)
	{
		grid.CreateMap(x, z);
		for (int i = 0; i < z; i++)
		{
			grid.GetCell(x / 2, i).TerrainTypeIndex = 1;
		}
	}
}