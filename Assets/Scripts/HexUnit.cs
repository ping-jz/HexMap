using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{

    List<HexCell> pathToTravel;
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
            if (location)
            {
                location.Unit = null;
            }
            location = value;
            location.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    public void Travel(List<HexCell> path)
    {
        Location = path[path.Count - 1];
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    IEnumerator TravelPath()
    {
        float t = 0f;
        Vector3 a, b, c = pathToTravel[0].Position;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].Position;
            //从两个相邻的六边形的中心点开始计算，得出其共同相邻边的中心点
            //你当时理解错就缺少点连线的概念，才没办法理解。
            c = (b + pathToTravel[i].Position) * 0.5f;
            for (; t < 1f; t += Time.deltaTime * 4f)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                yield return null;
            }
            t -= 1f;
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * 4f)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            yield return null;
        }
        transform.localPosition = location.Position;
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

    void OnEnable()
    {
        if (location)
        {
            transform.localPosition = location.Position;
        }
    }

    void OnDrawGizmos()
    {
        if (pathToTravel == null || pathToTravel.Count == 0)
        {
            return;
        }

        {
            Vector3 a, b, c = pathToTravel[0].Position;
            //点连线
            for (int i = 1; i < pathToTravel.Count; i++)
            {
                a = c;
                b = pathToTravel[i - 1].Position;
                //从两个相邻的六边形的中心点开始计算，得出其共同相邻边的中心点
                //你当时理解错就缺少点连线的概念，才没办法理解。
                c = (b + pathToTravel[i].Position) * 0.5f;
                for (float t = 0f; t < 1.0f; t += 0.1f)
                {
                    Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
                }
            }

            a = c;
            b = pathToTravel[pathToTravel.Count - 1].Position;
            c = b;
            for (float t = 0f; t < 1f; t += 0.1f)
            {
                Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
            }
        }
    }

}