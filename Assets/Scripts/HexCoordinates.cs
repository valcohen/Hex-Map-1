using System;
using System.IO;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int x, z;

    public int X { get { return x; } }
    public int Z { get { return z; } }
    public int Y { get {
            return -X - Z;
        }
    }

    public HexCoordinates(int x, int z) {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates (int x, int z) {
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position) {
        float x = position.x / (HexMetrics.innerRadius * 2f);
        float y = -x;

        // adjust for non-zero Z
        // every 2 rows, shift an entire unit to the left
        float offset = position.z / (HexMetrics.outerRadius * 3f);
        x -= offset;
        y -= offset;

        // x and y are now whole numbers at the center of each cell. round to int.
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0) {
            /*
            Debug.LogWarning(String.Format(
                "rounding error! {0} ({1}), {2} ({3}), {4} ({5})", 
                x, iX, 
                y, iY, 
                (-x - y), iZ
            ));
            */
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x -y - iZ);

            if (dX > dY && dX > dZ) {
                iX = -iY - iZ;
            } else if (dZ > dY) {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public int DistanceTo (HexCoordinates other) {
        int dX = Math.Abs(X - other.x);
        int dY = Math.Abs(Y - other.Y);
        int dZ = Math.Abs(z - other.z);

        int largest = 0;

        if (dX > largest) largest = dX;
        if (dY > largest) largest = dY;
        if (dZ > largest) largest = dZ;

        return largest;
    }

    public override string ToString() {
        return "(" + X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

    public string ToStringOnSeparateLines() {
        return "X:" + X.ToString() + "\n" + "Y:" + Y.ToString() + "\n" + "Z:" + Z.ToString();
    }

    public void Save (BinaryWriter writer) {
        writer.Write(x);
        writer.Write(z);
    }

    public static HexCoordinates Load (BinaryReader reader) {
        HexCoordinates c;
        c.x = reader.ReadInt32();
        c.z = reader.ReadInt32();
        return c;
    }
}
