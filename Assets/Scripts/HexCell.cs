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

            Refresh();
        }
    }
    int elevation = int.MinValue;

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

    public bool HasRiverThroughEdge (HexDirection direction) {
        return
            (hasIncomingRiver && incomingRiver == direction) ||
            (hasOutgoingRiver && outgoingRiver == direction);
    }

}
