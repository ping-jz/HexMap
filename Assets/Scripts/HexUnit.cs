using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{

    List<HexCell> pathToTravel;
    float orientation;
    HexCell location;

    const float rotationSpeed = 180f;
    const float speed = 4f;

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
        transform.localPosition = c;
        yield return LookAt(pathToTravel[1].Position);
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].Position;
            //从两个相邻的六边形的中心点开始计算，得出其共同相邻边的中心点
            //你当时理解错就缺少点连线的概念，才没办法理解。
            c = (b + pathToTravel[i].Position) * 0.5f;
            for (; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            t -= 1f;
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * speed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }
        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }

    IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation =
            Quaternion.LookRotation(point - transform.localPosition);
        float angle = Quaternion.Angle(fromRotation, toRotation);

        if (angle > 0f)
        {
            float speed = rotationSpeed / angle;
            for (float t = Time.deltaTime * speed;
                t < 1f; t += Time.deltaTime * speed)
            {
                transform.localRotation = Quaternion.Slerp(
                    fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
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