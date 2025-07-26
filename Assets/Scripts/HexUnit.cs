using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    const int visionRange = 3;

    List<int> pathToTravel;
    float orientation;
    int locationCellIndex = -1, currentTravelLocationCellIndex = -1;

    const float rotationSpeed = 180f;
    const float speed = 4f;

    public HexGrid Grid { get; set; }

    public int Speed
    {
        get
        {
            return 24;
        }
    }

    public int Location
    {
        get
        {
            return locationCellIndex;
        }
        set
        {
            if (locationCellIndex >= 0)
            {
                Grid.DecreaseVisibility(locationCellIndex, visionRange);
                 Grid.GetCell(locationCellIndex).Unit = null;
            }

            locationCellIndex = value;
            Grid.GetCell(value).Unit = this;
            Grid.IncreaseVisibility(value, visionRange);
            transform.localPosition = Grid.CellPositions[value];
            Grid.MakeChildOfColumn(transform, Grid.CellData[value].coordinates.ColumnIndex);
        }
    }

    public void Travel(List<int> path)
    {
        HexCell location = Grid.GetCell(locationCellIndex);
        location.Unit = null;
        location = Grid.GetCell(path[path.Count - 1]);
        locationCellIndex = location.Index;
        location.Unit = this;
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    IEnumerator TravelPath()
    {
        float t = 0f;
        Vector3 a, b, c = Grid.CellPositions[pathToTravel[0]];
        transform.localPosition = c;
        yield return LookAt(Grid.CellPositions[pathToTravel[1]]);

        if (currentTravelLocationCellIndex < 0)
        {
            currentTravelLocationCellIndex = pathToTravel[0];
        }


        Grid.DecreaseVisibility(currentTravelLocationCellIndex, visionRange);
        int currentColumn = Grid.CellData[currentTravelLocationCellIndex].coordinates.ColumnIndex;

        for (int i = 1; i < pathToTravel.Count; i++)
        {
            currentTravelLocationCellIndex = pathToTravel[i];
            a = c;
            b = Grid.CellPositions[pathToTravel[i - 1]];

            int nextColumn = Grid.CellData[currentTravelLocationCellIndex].coordinates.ColumnIndex;
            if (currentColumn != nextColumn)
            {
                if (nextColumn < currentColumn - 1)
                {
                    a.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    b.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
                else if (nextColumn > currentColumn + 1)
                {
                    a.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    b.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
                Grid.MakeChildOfColumn(transform, nextColumn);
                currentColumn = nextColumn;
            }

            //从两个相邻的六边形的中心点开始计算，得出其共同相邻边的中心点
            //你当时理解错就缺少点连线的概念，才没办法理解。
            c = (b + Grid.CellPositions[currentTravelLocationCellIndex]) * 0.5f;
            Grid.IncreaseVisibility(pathToTravel[i], visionRange);

            for (; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                if (d != Vector3.zero)
                {
                    transform.localRotation = Quaternion.LookRotation(d);
                }
                yield return null;
            }
            Grid.DecreaseVisibility(pathToTravel[i], visionRange);
            t -= 1f;
        }
        currentTravelLocationCellIndex = -1;


        a = c;
        b = Grid.CellPositions[locationCellIndex];
        c = b;
        Grid.IncreaseVisibility(locationCellIndex, visionRange);
        for (; t < 1f; t += Time.deltaTime * speed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }
        transform.localPosition = Grid.CellPositions[locationCellIndex];
        orientation = transform.localRotation.eulerAngles.y;

        ListPool<int>.Add(pathToTravel);
        pathToTravel = null;
    }

    IEnumerator LookAt(Vector3 point)
    {
        if (HexMetrics.Wrapping)
        {
            float xDistance = point.x - transform.localPosition.x;
            if (xDistance < -HexMetrics.innerRadius * HexMetrics.wrapSize)
            {
                point.x += HexMetrics.innerRadius * HexMetrics.wrapSize;
            }
            else if (xDistance > HexMetrics.innerRadius * HexMetrics.wrapSize)
            {
                point.x -= HexMetrics.innerRadius * HexMetrics.wrapSize;
            }
        }

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
        transform.localPosition = Grid.CellPositions[locationCellIndex];
    }

    public void Die()
    {
        if (locationCellIndex >= 0)
        {
            Grid.DecreaseVisibility(locationCellIndex, visionRange);
             Grid.GetCell(locationCellIndex).Unit = null;
            locationCellIndex = -1;
        }
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        Grid.CellData[locationCellIndex].coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(
            Instantiate(grid.UnitPrefab), grid.GetCell(coordinates).Index, orientation
        );
    }

    void OnEnable()
    {
        if (locationCellIndex >= 0)
        {
            transform.localPosition = Grid.CellPositions[locationCellIndex];
            if (currentTravelLocationCellIndex >= 0)
            {
               
                Grid.IncreaseVisibility(locationCellIndex, visionRange);
                Grid.DecreaseVisibility(currentTravelLocationCellIndex, visionRange);
                currentTravelLocationCellIndex = -1;
            }
        }
    }

    public int VisionRange
    {
        get
        {
            return 3;
        }
    }


    public bool IsValidDestination(HexCellData cell)
    {
        return cell.IsExplored && !cell.IsUnderwater && !Grid.GetCell(cell.coordinates).Unit;
    }

    public int getMoveCost(HexCellData fromCell, HexCellData toCell, HexDirection d)
    {
        HexEdgeType edgeType = fromCell.GetEdgeType(toCell);

        if (edgeType == HexEdgeType.Cliff)
        {
            return -1;
        }

        //int distance = current.Distance;
        int moveCost = 0;
        if (fromCell.HasRoadThroughEdge(d))
        {
            moveCost += 1;
        }
        else if (fromCell.Walled != toCell.Walled)
        {
            return -1;
        }
        else
        {
            moveCost += edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost += toCell.UrbanLevel + toCell.FarmLevel +
                toCell.PlantLevel;
        }

        return moveCost;
    }

    void OnDrawGizmos()
    {
        if (pathToTravel == null || pathToTravel.Count == 0)
        {
            return;
        }

        {
            Vector3 a, b, c = Grid.CellPositions[pathToTravel[0]];
            //点连线
            for (int i = 1; i < pathToTravel.Count; i++)
            {
                a = c;
                b = Grid.CellPositions[pathToTravel[i - 1]];
                //从两个相邻的六边形的中心点开始计算，得出其共同相邻边的中心点
                //你当时理解错就缺少点连线的概念，才没办法理解。
                c = (b + Grid.CellPositions[pathToTravel[i]]) * 0.5f;
                for (float t = 0f; t < 1.0f; t += 0.1f)
                {
                    Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
                }
            }

            a = c;
            b = Grid.CellPositions[pathToTravel[pathToTravel.Count - 1]];
            c = b;
            for (float t = 0f; t < 1f; t += 0.1f)
            {
                Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
            }
        }
    }

}