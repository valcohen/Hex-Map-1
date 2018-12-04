using UnityEngine.UI;
using UnityEngine;
using System.IO;

public class HexCell : MonoBehaviour {
    
    public HexCoordinates coordinates;

    int terrainTypeIndex;

    int elevation = int.MinValue;

    int distance;

    public int Index { get; set; }

    public int TerrainTypeIndex {
        get {
            return terrainTypeIndex;
        }
        set {
            if (terrainTypeIndex != value) {
                terrainTypeIndex = value;
                ShaderData.RefreshTerrain(this);
            }
        }
    }

    public bool IsVisible {
        get { return visibility > 0; }
    }
    int visibility;

    public void IncreaseVisibility () {
        visibility += 1;
        if (visibility == 1) {
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility() {
        visibility -= 1;
        if (visibility == 0) {
            ShaderData.RefreshVisibility(this);
        }
    }

    public int Distance {
        get {
            return distance;
        }
        set {
            distance = value;
        }
    }

    public int Elevation {
        get { return elevation;  }
        set
        {
            if (elevation == value) { return; }

            elevation = value;

            RefreshPosition();

            ValidateRivers();

            // prevent invalid roads
            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > maxRoadSlope)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public void SetLabel (string text) {
        Text label = uiRect.GetComponent<Text>();
        label.text = text;
    }

    void RefreshPosition () {
        // raise the cell
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y += (
            HexMetrics.SampleNoise(position).y * 2f - 1f) *
            HexMetrics.elevationPerturbStrength;

        transform.localPosition = position;

        // raise the label
        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;   // ui Z cuz canvas is rotated
        uiRect.localPosition = uiPosition;
    }

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
            if (Unit) {
                Unit.ValidateLocation();
            }
        }
    }

    void RefreshSelfOnly () {
        this.chunk.Refresh();
        if (Unit) {
            Unit.ValidateLocation();
        }
    }

    /*
     * Rivers. Possible cell configurations:
     * 
     *         / \     / \
     *        |   |   | +-+
     *         \ /     \ /
     * 
     *     / \     / \     / \
     *    | +-+   | +-+   +---+
     *     \ X     X /     \ /
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

    /*
     * IO
     */

    public void Save (BinaryWriter writer) {

        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)waterLevel);
        writer.Write((byte)urbanLevel);
        writer.Write((byte)farmLevel);
        writer.Write((byte)plantLevel);
        writer.Write((byte)specialIndex);

        writer.Write(walled);

        if (hasIncomingRiver) {
            writer.Write((byte)(incomingRiver + 128));    // 128 == has a river
        } else {
            writer.Write((byte)0);
        }

        if (hasOutgoingRiver) {
            writer.Write((byte)(outgoingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++) {
            if (roads[i]) {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);

    }

    public void Load (BinaryReader reader) {

        terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);

        elevation       = reader.ReadByte();
        RefreshPosition();

        waterLevel      = reader.ReadByte();
        urbanLevel      = reader.ReadByte();
        farmLevel       = reader.ReadByte();
        plantLevel      = reader.ReadByte();
        specialIndex    = reader.ReadByte();

        walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128) {     // 128 == has a river
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128) {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++) {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
    }

    /*
     * Pathfinding
     */

    public void DisableHighlight () {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight (Color color) {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    public HexCell PathFrom { get; set; }

    public int SearchHeuristic { get; set; }

    public int SearchPriority { 
        get {
            return distance + SearchHeuristic;
        }
    }

    public HexCell NextWithSamePriority { get; set; }

    /*
     * 0 = not yet reached, 1 = currently in frontier, 2 = removed from frontier
     */
    public int SearchPhase { get; set; }

    /*
     * Units
     */

    public HexUnit Unit { get; set; }


    /*
     * Cell data
     */

    public HexCellShaderData ShaderData { get; set; }

}
