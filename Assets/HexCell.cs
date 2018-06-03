using UnityEngine;

public class HexCell : MonoBehaviour {
    
    public HexCoordinates coordinates;

    public Color color;

    public int Elevation {
        get { return elevation;  }
        set { 
            elevation = value;

            // raise the cell
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            transform.localPosition = position;

            // raise the label
            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = elevation * -HexMetrics.elevationStep;   // Z cuz canvas is rotated
            uiRect.localPosition = uiPosition;
        }
    }
    int elevation;

    public RectTransform uiRect;

    [SerializeField]
    HexCell[] neighbors;

    public HexCell GetNeighbor(HexDirection direction) {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

}
