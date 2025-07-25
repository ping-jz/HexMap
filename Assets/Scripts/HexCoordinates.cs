using System;
using System.IO;
using UnityEngine;

[Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int x, z;

    public readonly int X
    {
        get
        {
            return x;
        }
    }

    public readonly int Z
    {
        get
        {
            return z;
        }
    }

    public readonly int ColumnIndex => (x + z / 2) / HexMetrics.chunkSizeX;

    public HexCoordinates(int x, int z)
    {
        //20250715 搞不懂啊
        if (HexMetrics.Wrapping)
        {
            int oX = x + z / 2;
            if (oX < 0)
            {
                x += HexMetrics.wrapSize;
            }
            else if (oX >= HexMetrics.wrapSize)
            {
                x -= HexMetrics.wrapSize;
            }
        }
        this.x = x;
        this.z = z;
    }


    public int Y
    {
        get
        {
            return -X - Z;
        }
    }

    public int DistanceTo(HexCoordinates other)
    {
        int xy = distanceTo(X, other.x) + distanceTo(Y, other.Y);
        if (HexMetrics.Wrapping)
        {
            other.x += HexMetrics.wrapSize;
            int xyWrapped = distanceTo(X, other.x) + distanceTo(Y, other.Y);
            if (xyWrapped < xy)
            {
                xy = xyWrapped;
            }
            else
            {
                other.x -= 2 * HexMetrics.wrapSize;
                xyWrapped = distanceTo(X, other.x) + distanceTo(Y, other.Y);
                if (xyWrapped < xy)
                {
                    xy = xyWrapped;
                }
            }
        }

        return (xy + distanceTo(Z, other.Z)) / 2;
    }

    private int distanceTo(int a, int b)
    {
        return a < b ? b - a : a - b;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }

    	public readonly HexCoordinates Step(HexDirection direction) =>
		direction switch
		{
			HexDirection.TopRight => new HexCoordinates(x, z + 1),
			HexDirection.Right => new HexCoordinates(x + 1, z),
			HexDirection.BottomRight => new HexCoordinates(x + 1, z - 1),
			HexDirection.BottomLeft => new HexCoordinates(x, z - 1),
			HexDirection.Left => new HexCoordinates(x - 1, z),
			_ => new HexCoordinates(x - 1, z + 1)
		};

    public override string ToString()
    {
        return "(" +
            X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        //地图的长宽是固定的所以x和z是固定范围内的数值

        //六边形的宽
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x;

        //六边形的1.5倍长，为什么是1.5倍
        //又是一个我不懂的数学难题
        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        //https://catlikecoding.com/unity/tutorials/hex-map/part-1/#5
        //没深刻理解这个，后面做的时候在回来看吧。不纠结了
        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }
        return new HexCoordinates(iX, iZ);
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(x);
        writer.Write(z);
    }

    public static HexCoordinates Load(BinaryReader reader)
    {
        HexCoordinates c;
        c.x = reader.ReadInt32();
        c.z = reader.ReadInt32();
        return c;
    }
}