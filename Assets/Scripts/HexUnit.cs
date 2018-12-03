﻿using System.IO;
using System.Collections;
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
    const float travelSpeed = 4f;

    public void Travel (List<HexCell> path) {
        Location = path[path.Count - 1];
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    void OnDrawGizmos() {
        if (pathToTravel == null || pathToTravel.Count == 0) { return; }

        Vector3 a, b = pathToTravel[0].Position;

        for (int i = 1; i < pathToTravel.Count; i++)
        {
            // cut across corners of adjacent cells by averaging their positions
            a = b;
            b = (pathToTravel[i - 1].Position + pathToTravel[i].Position) * 0.5f;
            for (float t = 0f; t < 1f; t += 0.2f) {
                Gizmos.DrawSphere(Vector3.Lerp(a, b, t), 1.5f);
            }
        }

        // move to center of destination cell
        a = b;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        for (float t = 0f; t < 1f; t += 0.2f) {
            Gizmos.DrawSphere(Vector3.Lerp(a, b, t), 2f);
        }
    }

    void OnEnable() {
        if (location) {
            transform.localPosition = location.Position;
        }
    }

    IEnumerator TravelPath () {
        Vector3 a, b = pathToTravel[0].Position;

        for (int i = 1; i < pathToTravel.Count; i++) {
            // cut across corners of adjacent cells by averaging their positions
            a = b;
            b = (pathToTravel[i - 1].Position + pathToTravel[i].Position) * 0.5f;
            for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Vector3.Lerp(a, b, t);
                yield return null;
            }
        }

        // move to center of destination cell
        a = b;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        for (float t = 0f; t < 1f; t += 0.2f)
        {
            transform.localPosition = Vector3.Lerp(a, b, t);
            yield return null;
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
