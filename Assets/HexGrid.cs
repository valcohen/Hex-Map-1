using System;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

    public int      width  = 6;
    public int      height = 6;

    public HexCell  cellPrefab;
    public Text     cellLabelPrefab;

    public Color    defaultColor = Color.white;

    Canvas          gridCanvas;
    HexCell[]       cells;
    HexMesh         hexMesh;

	private void Awake()
	{
        gridCanvas  = GetComponentInChildren<Canvas>();
        hexMesh     = GetComponentInChildren<HexMesh>();

        cells = new HexCell[height * width];

        for (int z = 0, i = 0; z < height; z++) {
            for (int x = 0; x < width; x++) {
                CreateCell(z, x, i++);
            }
        }
	}

    private void Start() {
        hexMesh.Triangulate(cells);

    }

    public void ColorCell (Vector3 position, Color color) {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        HexCell cell = cells[index];
        cell.color = color;
        hexMesh.Triangulate(cells);

        Debug.Log("Touched at " + coordinates.ToString());
    }

    private void CreateCell(int z, int x, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;
        cell.name = "Cell " + cell.coordinates.ToString();

        if (x > 0) {    // skip 1st col, set West neighbor
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0) {    // skip first row, as it has no lower neighbors
            // even rows
            if ((z & 1) == 0) {    // set SouthEast neighbor
                cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                if (x > 0) {       // skip 1st col, set SouthWest neighbor
                    cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
                }
            }
            // odd rows
            else {
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
                if (x < width - 1) {
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();
    }
}
