using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class HexUnit : MonoBehaviour {
    
    public HexCell Location {
        get {
            return location;
        }
        set {
            if (location) {
                location.Unit = null;   // clear old unit on move
            }
            location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    HexCell location;

    public float Orientation {
        get {
            return orientation;
        }
        set {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    float orientation;

    public void ValidateLocation() {
        transform.localPosition = location.Position;
    }

    public bool IsValidDestination (HexCell cell) {
        return      !cell.IsUnderwater
                &&  !cell.Unit;
    }

    List<HexCell> pathToTravel;

    public void Travel (List<HexCell> path) {
        Location = path[path.Count - 1];
        pathToTravel = path;
    }

    void OnDrawGizmos() {
        if (pathToTravel == null || pathToTravel.Count == 0) { return; }

        for (int i = 1; i < pathToTravel.Count; i++)
        {
            Vector3 a = pathToTravel[i - 1].Position;
            Vector3 b = pathToTravel[i].Position;
            for (float t = 0f; t < 1f; t += 0.2f) {
                Gizmos.DrawSphere(Vector3.Lerp(a, b, t), 1.5f);
            }
        }
    }


    public void Die () {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save (BinaryWriter writer) {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load (BinaryReader reader, HexGrid grid) {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(
            Instantiate(unitPrefab), grid.GetCell(coordinates), orientation
        );
    }

    public static HexUnit unitPrefab;
}
