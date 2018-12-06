using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexUnit : MonoBehaviour {
    const int visionRange = 3;
    
    public HexCell Location {
        get {
            return location;
        }
        set {
            if (location) {
                Grid.DecreaseVisibility(location, visionRange); // update old location
                location.Unit = null;           // clear old unit on move
            }
            location = value;
            value.Unit = this;
            Grid.IncreaseVisibility(location, visionRange);     // update new location
            transform.localPosition = value.Position;
        }
    }
    HexCell location, currentTravelLocation;

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

    public int Speed { get { return 24; } }

    public void ValidateLocation() {
        transform.localPosition = location.Position;
    }

    public bool IsValidDestination (HexCell cell) {
        return      !cell.IsUnderwater
                &&  !cell.Unit;
    }

    public HexGrid Grid { get; set; }

    void OnEnable() {
        if (location) {
            transform.localPosition = location.Position;
            if (currentTravelLocation) {
                Grid.IncreaseVisibility(location, visionRange);
                Grid.DecreaseVisibility(currentTravelLocation, visionRange);
                currentTravelLocation = null;
            }
        }
    }

    public int GetMoveCost (
        HexCell fromCell, HexCell toCell, HexDirection direction
    ) {
        HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
        if (edgeType == HexEdgeType.Cliff) {
            return -1;
        }

        int moveCost;

        // road travel costs 1
        if (fromCell.HasRoadThroughEdge(direction)) {
            moveCost = 1;
        }

        // don't allow travel thru walls
        else if (fromCell.Walled != toCell.Walled) {
            return -1;
        }

        // offroad flats cost 5, everything else costs 10
        else
        {
            moveCost = (edgeType == HexEdgeType.Flat) ? 5 : 10;

            // slow own when moving thru features
            moveCost += toCell.UrbanLevel
                      + toCell.FarmLevel
                      + toCell.PlantLevel
                      + toCell.SpecialIndex;
        }

        return moveCost;
    }

    List<HexCell> pathToTravel;
    const float travelSpeed = 4f;

    public void Travel (List<HexCell> path) {
        location.Unit = null;   // clean old loc
        location = path[path.Count - 1];
        location.Unit = this;

        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    // draw spheres along path using Gizmos, which must be enabled in the view
    /*
    void OnDrawGizmos() {
        if (pathToTravel == null || pathToTravel.Count == 0) { return; }

        Vector3 a, b, c = pathToTravel[0].Position;

        for (int i = 1; i < pathToTravel.Count; i++)
        {
            // cut across corners of adjacent cells by averaging their positions
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * 0.5f;

            for (float t = 0f; t < 1f; t += 0.2f) {  // 1 / .2 = 5 steps
            // for (float t = 0f; t < 1f; t += Time.deltaTime * travelSpeed) { // smooth steps, lots-o-spheres
                Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
            }
        }

        // move to center of destination cell
        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;
        for (float t = 0f; t < 1f; t += 0.2f) {  // 1 / .2 = 5 steps
            Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
        }
    }
    */


    IEnumerator TravelPath () {
        Vector3 a, b, c = pathToTravel[0].Position;
        yield return LookAt(pathToTravel[1].Position);
        Grid.DecreaseVisibility(
            currentTravelLocation ? currentTravelLocation : pathToTravel[0], 
            visionRange
        );

        float t = Time.deltaTime * travelSpeed;   // time remaining to destination
        for (int i = 1; i < pathToTravel.Count; i++) {
            currentTravelLocation = pathToTravel[i];
            // cut across corners of adjacent cells by averaging their positions
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + currentTravelLocation.Position) * 0.5f;
            Grid.IncreaseVisibility(pathToTravel[i], visionRange);
            for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);

                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;   // force vertical orientation vs leaning along the path
                transform.localRotation = Quaternion.LookRotation(d);

                yield return null;
            }
            Grid.DecreaseVisibility(pathToTravel[i], visionRange);
            t -= 1f;
        }
        currentTravelLocation = null;

        // move to center of destination cell
        a = c;
        b = location.Position;
        c = b;
        Grid.IncreaseVisibility(location, visionRange);
        for (; t < 1f; t += Time.deltaTime * travelSpeed) {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);

            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;   // force vertical orientation
            transform.localRotation = Quaternion.LookRotation(d);

            yield return null;
        }

        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;

        // disable this if using OnDrawGgizmos, as it uses pathToTravel
        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }

    const float rotationSpeed = 360f;
    IEnumerator LookAt (Vector3 point) {
        point.y = transform.localPosition.y;    // prevent leaning

        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation = 
            Quaternion.LookRotation(point - transform.localPosition);
        float angle = Quaternion.Angle(fromRotation, toRotation);

        if (angle > 0f) {
            float speed = rotationSpeed / angle;

            for (
                float t = Time.deltaTime * speed;
                t < 1f;
                t += Time.deltaTime * speed
            ) {
                transform.localRotation =
                    Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }

    public void Die () {
        if (location) {
            Grid.DecreaseVisibility(location, visionRange);
        }
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
