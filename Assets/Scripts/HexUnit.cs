using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{

    float orientation;
    HexCell location;

    public HexCell Location
    {
        get
        {
            return location;
        }
        set
        {
            location = value;
            location.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    public float Orientation
    {
        get
        {
            return orientation;
        }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    public void Die()
    {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        location.Coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(
            Instantiate(grid.UnitPrefab), grid.GetCell(coordinates), orientation
        );
    }


}