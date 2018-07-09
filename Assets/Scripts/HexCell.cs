using UnityEngine;

public class HexCell : MonoBehaviour {
    
    public HexCoordinates coordinates;

    public Color Color {
        get {
            return color;
        }
        set {
            if (color == value) {
                return;
            }
            color = value;
            Refresh();
        }
    }
    Color color;

    public int Elevation {
        get { return elevation;  }
        set {
            if (elevation == value) {
                return;
            }
            elevation = value;

            // raise the cell
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            position.y += (
                HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.elevationPerturbStrength;

            transform.localPosition = position;

            // raise the label
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;   // ui Z cuz canvas is rotated
            uiRect.localPosition = uiPosition;


            ValidateRivers();

            // prevent invalid roads
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection)i) > maxRoadSlope ) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }
    int elevation = int.MinValue;

    public int GetElevationDifference (HexDirection direction) {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    public float StreamBedY {
        get {
            return
                (elevation + HexMetrics.streamBedElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public float RiverSurfaceY {
        get {
            return
                (elevation + HexMetrics.waterSurfaceElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public float WaterSurfaceY {
        get {
            return
                (waterLevel + HexMetrics.waterSurfaceElevationOffset) *
                HexMetrics.elevationStep;
        }
    }

    public int WaterLevel {
        get {
            return waterLevel;
        }
        set {
            if (waterLevel == value) { return; }
            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }
    int waterLevel;

    public bool IsUnderwater { get { return waterLevel > elevation; } }

    public Vector3 Position {
        get {
            return transform.localPosition;
        }
    }

    public RectTransform uiRect;

    public HexGridChunk chunk;

    [SerializeField]
    HexCell[] neighbors;

    public HexCell GetNeighbor(HexDirection direction) {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) {
        return HexMetrics.GetEdgeType(
            elevation, neighbors[(int)direction].elevation
        );
    }

    public HexEdgeType GetEdgeType(HexCell otherCell) {
        return HexMetrics.GetEdgeType(
            elevation, otherCell.elevation
        );
    }

    void Refresh () {
        if (chunk) {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++) {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != this.chunk) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    void RefreshSelfOnly () {
        this.chunk.Refresh();
    }

    /*
     * Rivers. Possible cell configurations:
     * 
     *         / \     / \
     *        |   |   |  -|
     *         \ /     \ /
     * 
     *     / \     / \     / \
     *    | +-|   | +-|   |---|
     *     \ \     / /     \ /
     */

    bool hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver, outgoingRiver;

    public bool HasIncomingRiver { get { return hasIncomingRiver; } }
    public bool HasOutgoingRiver { get { return hasOutgoingRiver; } }

    public HexDirection IncomingRiver { get { return incomingRiver; } }
    public HexDirection OutgoingRiver { get { return outgoingRiver; } }

    public bool HasRiver { get { return hasIncomingRiver || hasOutgoingRiver; } }
    public bool HasRiverBeginOrEnd { get { return hasIncomingRiver != hasOutgoingRiver; } }

    public HexDirection RiverBeginOrEndDirection {
        get {
            return hasIncomingRiver ? incomingRiver : outgoingRiver;
        }
    }

    public bool HasRiverThroughEdge (HexDirection direction) {
        return
            (hasIncomingRiver && incomingRiver == direction) ||
            (hasOutgoingRiver && outgoingRiver == direction);
    }

    public void RemoveOutgoingRiver () {
        if (!hasOutgoingRiver) { return; }

        hasOutgoingRiver = false;
        this.RefreshSelfOnly();

        // We don't currently support rivers that flow out of the map,
        // so no need to check whether neighbor exists.
        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver () {
        if (!hasIncomingRiver) { return; }

        hasIncomingRiver = false;
        this.RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver() {
        this.RemoveOutgoingRiver();
        this.RemoveIncomingRiver();
    }

    public void SetOutgoingRiver (HexDirection direction) {
        if (hasOutgoingRiver && outgoingRiver == direction) { return; }

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor)) { return; }

        // clean up existing rivers
        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction) {
            RemoveIncomingRiver();
        }

        // set our outgoing river
        hasOutgoingRiver = true;
        outgoingRiver = direction;
        specialIndex = 0;   // if we have a river, disable full-cell special features

        // set neighbor's incoming river
        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.specialIndex = 0;

        SetRoad((int)direction, false);
    }

    bool IsValidRiverDestination (HexCell neighbor) {
        return neighbor && (
                this.elevation  >= neighbor.elevation 
            ||  this.waterLevel == neighbor.elevation
        );
    }

    void ValidateRivers () {
        if (
                hasOutgoingRiver 
            &&  !IsValidRiverDestination(GetNeighbor(outgoingRiver))
        ) {
            RemoveOutgoingRiver();
        }
        if (
                hasIncomingRiver
            &&  !GetNeighbor(incomingRiver).IsValidRiverDestination(this)
        ) {
            RemoveIncomingRiver();
        }
    }

    /*
     * Roads.
     */

    [SerializeField]
    bool[] roads = new bool[6];
    int maxRoadSlope = 1;

    public bool HasRoadThroughEdge (HexDirection direction) {
        return roads[(int)direction];
    }

    public bool HasRoads {
        get {
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i]) {
                    return true;
                }
            }
            return false;
        }
    }

    public void RemoveRoads () {
        for (int i = 0; i < neighbors.Length; i++) {
            if (roads[i]) {
                SetRoad(i, false);
            }
        }
    }

    public void AddRoad (HexDirection direction) {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction)
            && !IsSpecial && !GetNeighbor(direction).IsSpecial
            && GetElevationDifference(direction) <= maxRoadSlope
        ) {
            SetRoad((int)direction, true);
        }
    }

    void SetRoad (int i, bool state) {
        roads[i] = state;
        neighbors[i].roads[(int)((HexDirection)i).Opposite()] = state;
        neighbors[i].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    /*
     * Features - Buildings, Farms & Vegetation
     */
    public int UrbanLevel {
        get {
            return urbanLevel;
        }
        set {
            if (urbanLevel != value) {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get
        {
            return farmLevel;
        }
        set
        {
            if (farmLevel != value)
            {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get
        {
            return plantLevel;
        }
        set
        {
            if (plantLevel != value)
            {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    int urbanLevel, farmLevel, plantLevel;

    /*
     * Walls
     */
    public bool Walled {
        get {
            return walled;
        }
        set {
            if (walled != value) {
                walled = value;
                Refresh();
            }
        }
    }
    bool walled;

    /*
     * Special features
     */
    public int SpecialIndex {
        get {
            return specialIndex;
        }
        set {
            if (specialIndex != value && !HasRiver) {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }
    int specialIndex;

    public bool IsSpecial {
        get { return specialIndex > 0; }
    }
}
