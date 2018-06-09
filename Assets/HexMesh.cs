using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

    Mesh            hexMesh;
    List<Vector3>   vertices;
    List<int>       triangles;
    MeshCollider    meshCollider;
    List<Color>     colors;

    void Awake() {
        GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
        meshCollider = gameObject.AddComponent<MeshCollider>();
        hexMesh.name = "Hex Mesh";
        vertices     = new List<Vector3>();
        triangles    = new List<int>();
        colors       = new List<Color>();
    }

    public void Triangulate(HexCell[] cells)
    {
        hexMesh.Clear();
        vertices.Clear();
        triangles.Clear();
        colors.Clear();

        for (int i = 0; i < cells.Length; i++) {
            Triangulate(cells[i]);
        }
        hexMesh.vertices    = vertices.ToArray();
        hexMesh.triangles   = triangles.ToArray();
        hexMesh.colors      = colors.ToArray();
        hexMesh.RecalculateNormals();

        meshCollider.sharedMesh = hexMesh;
    }

    void Triangulate(HexCell cell) {
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
            Triangulate(d, cell);
        }
    }

    /*
     *   v3-+-+-+-v4
     *    \ |X|X| /
     *     v1-+-v2
     *      \ | /
     *       \|/
     *        v center
     */
    void Triangulate(HexDirection direction, HexCell cell) {
        Vector3 center = cell.Position;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        // add triangle
        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.color);

        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, v1, v2);
        }
    }

    void TriangulateConnection(
        HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2
    ) {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null) { 
            return; 
        }

        // add "bridge" quad to blend triangle edge with neighbor
        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;
        v3.y = v4.y = neighbor.Position.y;

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbor);
        }
        else
        {
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(cell.color, neighbor.color);
        }

        /* 
         * add triangle to fill corners
         * find bottom (lowest) cell and left and right neighbors
         *
         *  R  |  B  |  L | R  |  B | L
         *     V     |    V    |    V
         *   / L \   |  / B \  |  / R \
         *           |         |
         *   ccwise      no       cwise
         *            rotation
         */
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null) {
            Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation) {
                if (cell.Elevation <= nextNeighbor.Elevation) {
                    TriangulateCorner(v2, cell, v4, neighbor, v5, nextNeighbor);
                } 
                else { // nxt is lower, rotate cc-wise to keep properly oriented
                    TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation) {
                TriangulateCorner(v4, neighbor, v5, nextNeighbor, v2, cell);
            }
            else {
                TriangulateCorner(v5, nextNeighbor, v2, cell, v4, neighbor);
            }
        }
    }

    void TriangulateEdgeTerraces(
        Vector3 beginLeft, Vector3 beginRight, HexCell beginCell, 
        Vector3 endLeft,   Vector3 endRight,   HexCell endCell
    ) {
        Vector3 v3 = HexMetrics.TerraceLerp(beginLeft,  endLeft,  1);
        Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color   c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

        // first step
        AddQuad(beginLeft, beginRight, v3, v4);
        AddQuadColor(beginCell.color, c2);

        // intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color   c1 = c2;

            v3 = HexMetrics.TerraceLerp(beginLeft,  endLeft,  i);
            v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);

            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2);
        }

        // last step
        AddQuad(v3, v4, endLeft, endRight);
        AddQuadColor(c2, endCell.color);
    }

    void TriangulateCorner (
        Vector3 bottom, HexCell bottomCell,
        Vector3 left,   HexCell leftCell,
        Vector3 right,  HexCell rightCell
    ) {
        HexEdgeType leftEdgeType  = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            // slope, slope, flat (SSF)
            if (rightEdgeType == HexEdgeType.Slope)
            {
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
            // SFS
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
            // slope-cliff cases (SCS, SCC)
            else
            {
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            // FFS
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            // slope-cliff cases (CSS, CSC)
            else
            {
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }
        // cliff-cliff cases (CCSR, CCSL)
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else
            {
                TriangulateCornerTerracesCliff(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
        }
        // all remaining corner cases (FFF, CCF, CCCR, CCCL)
        else
        {
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
        }
    }

    void TriangulateCornerTerraces (
        Vector3 begin, HexCell beginCell, 
        Vector3 left,  HexCell leftCell, 
        Vector3 right, HexCell rightCell
    ) {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

        // first (bottom) step
        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.color, c3, c4);

        // intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }

        // final (top) step
        AddQuad(v3, v4, left, right);
        AddQuadColor(c3, c4, leftCell.color, rightCell.color);
    }

    void TriangulateCornerTerracesCliff (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        // fill bottom half
        // get boundary point 1 elevation level above bottom cell
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        // ensure interpolator is always positive
        if (b < 0) {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(begin, right, b);
        Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

        TriangulateBoundaryTriangle(
            begin, beginCell, left, leftCell, boundary, boundaryColor
        );

        // fill top half
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope ) {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        } else {
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    void TriangulateCornerCliffTerraces (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        // fill bottom half
        // get boundary point 1 elevation level above bottom cell
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        // ensure interpolator is always positive
        if (b < 0) {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(begin, left, b);
        Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

        TriangulateBoundaryTriangle(
            right, rightCell, begin, beginCell, boundary, boundaryColor
        );

        // fill top half
        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else
        {
            AddTriangle(left, right, boundary);
            AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangle (
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    ){
        Vector3 v2 = HexMetrics.TerraceLerp(begin, left, 1);
        Color   c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

        // first step
        AddTriangle(begin, v2, boundary);
        AddTriangleColor(beginCell.color, c2, boundaryColor);

        // intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++) {
            Vector3 v1 = v2;
            Color   c1 = c2;
            v2 = HexMetrics.TerraceLerp(begin, left, i);
            c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);

            AddTriangle(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        // last step
        AddTriangle(v2, left, boundary);
        AddTriangleColor(c2, leftCell.color, boundaryColor);
    }

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
        int vertexIndex = vertices.Count;

        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
    }

    void AddTriangleColor(Color color) {
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    void AddTriangleColor(Color c1, Color c2, Color c3) {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
    }

    /*
     *   v3-------v4
     *    \XXXXXXX/
     *     v1---v2
     *      \   /
     *       \ /
     *        v center
     */
    void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
        int vertexIndex = vertices.Count;

        vertices.Add(Perturb(v1));
        vertices.Add(Perturb(v2));
        vertices.Add(Perturb(v3));
        vertices.Add(Perturb(v4));

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
    }

    void AddQuadColor (Color c1, Color c2, Color c3, Color c4) {
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c3);
        colors.Add(c4);
    }

    void AddQuadColor (Color c1, Color c2)
    {
        colors.Add(c1);
        colors.Add(c1);
        colors.Add(c2);
        colors.Add(c2);
    }

    Vector3 Perturb (Vector3 position) {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
        // position.y += (sample.y * 2f - 1f) * HexMetrics.cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
        return position;
    }
}
