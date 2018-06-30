using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour {

    public HexMesh terrain, rivers, roads, water, waterShore, estuaries;

    HexCell[] cells;
    Canvas    gridCanvas;

    void Awake () {
        gridCanvas  = GetComponentInChildren<Canvas>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        ShowUI(false);
    }

    public void AddCell(int index, HexCell cell) {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(this.transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void Refresh () {
        enabled = true;
    }

    public void ShowUI (bool visible) {
        gridCanvas.gameObject.SetActive(visible);
    }

    void LateUpdate() {
        Triangulate();
        enabled = false;
    }

    public void Triangulate() {
        terrain.Clear();
        rivers.Clear();
        roads.Clear();
        water.Clear();
        waterShore.Clear();
        estuaries.Clear();

        for (int i = 0; i < cells.Length; i++) {
            Triangulate(cells[i]);
        }

        terrain.Apply();
        rivers.Apply();
        roads.Apply();
        water.Apply();
        waterShore.Apply();
        estuaries.Apply();
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
        EdgeVertices edge = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                // drop middle edge vertex to streambed height
                edge.v3.y = cell.StreamBedY;

                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, edge);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, edge);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, edge);
            }
        }
        else
        {
            TriangulateWithoutRiver(direction, cell, center, edge);
        }

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, edge);
        }

        if (cell.IsUnderwater) {
            TriangulateWater(direction, cell, center);
        }
    }


    void TriangulateConnection(
        HexDirection direction, HexCell cell, EdgeVertices e1
    ) {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        // add "bridge" quad to blend triangle edge with neighbor
        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        EdgeVertices e2 = new EdgeVertices(
            e1.v1 + bridge,
            e1.v5 + bridge
        );

        if (cell.HasRiverThroughEdge(direction))
        {
            // drop middle edge vertex to streambedheight
            e2.v3.y = neighbor.StreamBedY;

            if (!cell.IsUnderwater)
            {
                if (!neighbor.IsUnderwater)
                {
                    {
                        TriangulateRiverQuad(e1.v2, e1.v4, e2.v2, e2.v4,
                            cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                            cell.HasIncomingRiver && cell.IncomingRiver == direction
                        );
                    }
                }
                else if (cell.Elevation > neighbor.WaterLevel) {
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY
                    );
                }
            }
            // we're underwater but neighbor isn't
            else if (
                !neighbor.IsUnderwater &&
                neighbor.Elevation > cell.WaterLevel
            ) {
                TriangulateWaterfallInWater(
                    e2.v4, e2.v2, e1.v4, e1.v2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    cell.WaterSurfaceY
                );
            }
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(
                e1, cell, e2, neighbor, cell.HasRoadThroughEdge(direction)
            );
        }
        else
        {
            TriangulateEdgeStrip(
                e1, cell.Color, e2, neighbor.Color,
                cell.HasRoadThroughEdge(direction)
            );
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
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(
                        e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor
                    );
                }
                else
                { // nxt is lower, rotate cc-wise to keep properly oriented
                    TriangulateCorner(
                        v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
                    );
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(
                    e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell
                );
            }
            else
            {
                TriangulateCorner(
                    v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
                );
            }
        }
    }

    void TriangulateWithoutRiver (
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        TriangulateEdgeFan(center, e, cell.Color);

        if (cell.HasRoads) {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(
                center,
                Vector3.Lerp(center, e.v1, interpolators.x),
                Vector3.Lerp(center, e.v5, interpolators.y),
                e, cell.HasRoadThroughEdge(direction)
            );
        }
    }

    /*
     *  To accommodate river through center, 
     *  create trapzoid instead of triangle. 
     *  
     *   e.v1 ______________ e.v5
     *        \  /| /| /|  /
     *    m.v1 \/_|/_|/_|// m.v5
     *          \ | /| /|/
     *           \|/_|/_| 
     *         cL   ctr  cR
     */
    void TriangulateWithRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        // To create a channel across the cell, stretch the center into 
        // a line with same width as channel.
        Vector3 centerL, centerR;

        // Outer half:
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            // Find left vertex by moving 1/4 the way from the center 
            // to the 1st corner of the previous part.
            centerL = center +
                HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;

            // Same for the right vertex, but use 2nd corner of next part
            centerR = center +
                HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        // Handle one-step turns
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
            centerR = center;
        }
        // Handle two-step turns
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Next()) *
                (0.5f * HexMetrics.innerToOuter);
        }

        else
        {
            centerL = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Previous()) *
                (0.5f * HexMetrics.innerToOuter);
            centerR = center;
        }

        center = Vector3.Lerp(centerL, centerR, 0.5f);

        // Middle line: create edge vertices between center & edge
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(centerL, e.v1, 0.5f),
            Vector3.Lerp(centerR, e.v5, 0.5f),
            1f / 6f     // compensate for pinched channels
        );

        // Lower channel bottom
        m.v3.y = center.y = e.v3.y;

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        // Inner half:
        terrain.AddTriangle(centerL, m.v1, m.v2);
        terrain.AddQuad(centerL, center, m.v2, m.v3);
        terrain.AddQuad(center, centerR, m.v3, m.v4);
        terrain.AddTriangle(centerR, m.v4, m.v5);

        terrain.AddTriangleColor(cell.Color);
        terrain.AddQuadColor(cell.Color);
        terrain.AddQuadColor(cell.Color);
        terrain.AddTriangleColor(cell.Color);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(
                centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed
            );
            TriangulateRiverQuad(
                m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed
            );
        }
    }

    void TriangulateWithRiverBeginOrEnd(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        // set middle vertex to streambed height
        m.v3.y = e.v3.y;

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

        if (!cell.IsUnderwater)
        {
            bool reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(
                m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed
            );

            center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
            rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed)
            {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(1.0f, 0.2f),
                    new Vector2(0.0f, 0.2f)
                );
            }
            else
            {
                rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(0.0f, 0.6f),
                    new Vector2(1.0f, 0.6f)
                );
            }
        }
    }

    void TriangulateAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        if (cell.HasRoads) {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }

        // check if inside the curve and move center toward edge
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                (HexMetrics.innerToOuter * 0.5f);
            }
            // if river in next direction but not previous, check if it's
            // a straight river & move center towards 1st corner.
            else if (
                cell.HasRiverThroughEdge(direction.Previous2())
            )
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        // has a river in previous direction & it's straight; 
        // move center towards next solid corner
        else if (
            cell.HasRiverThroughEdge(direction.Previous()) &&
            cell.HasRiverThroughEdge(direction.Next2())
        )
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }


        EdgeVertices m = new EdgeVertices(
            Vector3.Lerp(center, e.v1, 0.5f),
            Vector3.Lerp(center, e.v5, 0.5f)
        );

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);
    }

    /*
     * U = 0 at left of river, 1 at right when looking downstream
     * V goes from 0 to 1 in the direction of the flow
     * 
     * To avoid repeaating the texture 5 times by applying it to each quad,
     * we stretch it and apply 1/5th to each quad:
     *          ________                 ________      
     * forward |     1.0|       reverse |    -0.2|      
     *         |0.8 ____|               |0.0 ____|      
     *        /|     0.8|\             /|     0.0|\     
     *       / |0.6 ____| \           / |0.2 ____| \    
     *      /  |     0.6|  \         /  |     0.2|  \  
     *     /   |0.4 ____|   \       /   |0.4 ____|   \ 
     *     \   |     0.4|   /       \   |     0.4|   / 
     *      \  |0.2 ____|  /         \  |0.6 ____|  / 
     *       \ |     0.2| /           \ |     0.6| /   
     *        \|0.0 ____|/             \|0.8 ____|/
     */
    void TriangulateRiverQuad (
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed
    ) {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        rivers.AddQuad(v1, v2, v3, v4);

        if (reversed) {
            rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v); // right to left, top to bottom
        } else {
            rivers.AddQuadUV(0f, 1f, v,        v + 0.2f); // left to right, bottom to top
        }
    }

    void TriangulateRiverQuad (
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed
    ) {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left,   HexCell leftCell,
        Vector3 right,  HexCell rightCell
    ) { 
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
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
            terrain.AddTriangle(bottom, left, right);
            terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end,   HexCell endCell,
        bool hasRoad
    ) {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        // first step
        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2, hasRoad);

        // intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            EdgeVertices e1 = e2;
            Color c1 = c2;

            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);

            TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
        }

        // last step
        TriangulateEdgeStrip(e2, c2, end, endCell.Color, hasRoad);
    }

    void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left,  HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        // first (bottom) step
        terrain.AddTriangle(begin, v3, v4);
        terrain.AddTriangleColor(beginCell.Color, c3, c4);

        // intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            terrain.AddQuad(v1, v2, v3, v4);
            terrain.AddQuadColor(c1, c2, c3, c4);
        }

        // final (top) step
        terrain.AddQuad(v3, v4, left, right);
        terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left,  HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        // fill bottom half
        // get boundary point 1 elevation level above bottom cell
        float b = 1f / (rightCell.Elevation - beginCell.Elevation);
        // ensure interpolator is always positive
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        TriangulateBoundaryTriangle(
            begin, beginCell, left, leftCell, boundary, boundaryColor
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
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left,  HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        // fill bottom half
        // get boundary point 1 elevation level above bottom cell
        float b = 1f / (leftCell.Elevation - beginCell.Elevation);
        // ensure interpolator is always positive
        if (b < 0)
        {
            b = -b;
        }
        Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

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
            terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    ) {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        // first step
        terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        // intermediate steps
        for (int i = 2; i < HexMetrics.terraceSteps; i++)
        {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);

            terrain.AddTriangleUnperturbed(v1, v2, boundary);
            terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        // last step
        terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color) {
        terrain.AddTriangle(center, edge.v1, edge.v2);
        terrain.AddTriangle(center, edge.v2, edge.v3);
        terrain.AddTriangle(center, edge.v3, edge.v4);
        terrain.AddTriangle(center, edge.v4, edge.v5);

        terrain.AddTriangleColor(color);
        terrain.AddTriangleColor(color);
        terrain.AddTriangleColor(color);
        terrain.AddTriangleColor(color);
    }

    void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2,
        bool hasRoad = false
    ) {
        terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);
        terrain.AddQuadColor(c1, c2);

        if (hasRoad) {
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
        }
    }

    /*
     *        v4  v5  v6
     *     +---+---+---+---+
     *     |\  |\  |\  |\  |
     *     | \ | \ | \ | \ |
     *     |  \|  \|  \|  \|
     *     +---+---+---+---+
     *        v1  v2  v3
     *    U:  0.0 1.0 0.0
     */
    void TriangulateRoadSegment (
        Vector3 v1, Vector3 v2, Vector3 v3, 
        Vector3 v4, Vector3 v5, Vector3 v6
    ) {
        roads.AddQuad(v1, v2, v4, v5);
        roads.AddQuad(v2, v3, v5, v6);

        roads.AddQuadUV(0f, 1f, 0f, 0f);    // V unused so set to 0
        roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    /*
     *   e.v1 ___________ e.v5
     *        \ | /| /| /
     *         \|/_|/_|/ 
     *       mL \  |  / mR
     *           \ | /
     *            \|/
     *          center
     */
    void TriangulateRoad(
        Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e,
        bool hasRoadThroughCellEdge
    ) {
        if (hasRoadThroughCellEdge) {
            Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);

            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);

            roads.AddTriangle(center, mL, mC);
            roads.AddTriangle(center, mC, mR);
            roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f)
            );
            roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f)
            );
        }
        else {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    void TriangulateRoadEdge ( Vector3 center, Vector3 mL, Vector3 mR) {
        roads.AddTriangle(center, mL, mR);
        roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
    }

    /*
     * v2.x = left interpolator, v2.y = right interpolator
     * use larger intrpolator (wider midpoint) if there's an adjacent road
     */
    Vector2 GetRoadInterpolators (HexDirection direction, HexCell cell) {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction)) {
            interpolators.x = interpolators.y = 0.5f;
        }
        else {
            interpolators.x =
                cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.y =
                cell.HasRoadThroughEdge(direction.Next())     ? 0.5f : 0.25f;
        }
        return interpolators;
    }

    void TriangulateRoadAdjacentToRiver (
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());

        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;

        // avoid rivers ends; move road center 1/3 toward middle edge opposite river
        if (cell.HasRiverBeginOrEnd) {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(
                cell.RiverBeginOrEndDirection.Opposite()
            ) * (1f / 3f);
        }
        // avoid straight rivers; move road center halfway away
        else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite()) {
            Vector3 corner;
            if (previousHasRiver) {
                if (
                    !hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Next())
                ) {
                    return;     // prune isolated road part
                }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            }
            else {
                if (
                    !hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Previous())
                ) {
                    return;
                }
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
            center     += corner * 0.25f;
        }
        // avoid zig-zag rivers
        else if (cell.IncomingRiver == cell.OutgoingRiver.Previous()) {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        }
        else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
        {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        }
        // avoid inside of curved rivers
        else if (previousHasRiver && nextHasRiver) {
            if (!hasRoadThroughEdge) { return; }    // prune isolated road part
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) *
                HexMetrics.innerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        }
        // avoid outside of curved rivers
        else {
            HexDirection middle;
            if (previousHasRiver) {
                middle = direction.Next();
            }
            else if (nextHasRiver) {
                middle = direction.Previous();
            }
            else {
                middle = direction;
            }
            // prune roads
            if (
                !cell.HasRoadThroughEdge(middle) &&
                !cell.HasRoadThroughEdge(middle.Previous()) &&
                !cell.HasRoadThroughEdge(middle.Next())
            ) {
                return;
            }
            roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
        }

        Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
        Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);

        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        // fill triangular gaps where rivers are adjacent to roads
        if (previousHasRiver) {
            TriangulateRoadEdge(roadCenter, center, mL);
        }
        if (nextHasRiver) {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }

    /*
     * Water
     */
    void TriangulateWater (
        HexDirection direction, HexCell cell, Vector3 center
    ) {
        center.y = cell.WaterSurfaceY;

        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor != null && !neighbor.IsUnderwater) {
            TriangulateWaterShore(direction, cell, neighbor, center);
        } else {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
    }

    void TriangulateOpenWater (
        HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center
    ) {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        water.AddTriangle(center, c1, c2);

        // connect adjacent water cells with a single quad
        if (direction <= HexDirection.SE && neighbor != null) {

            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            water.AddQuad(c1, c2, e1, e2);

            if (direction <= HexDirection.E) {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater) { return; }
                water.AddTriangle(
                    c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next())
                );
            }
        }

    }

    void TriangulateWaterShore (
        HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center
    ) {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction)
        );
        water.AddTriangle(center, e1.v1, e1.v2);
        water.AddTriangle(center, e1.v2, e1.v3);
        water.AddTriangle(center, e1.v3, e1.v4);
        water.AddTriangle(center, e1.v4, e1.v5);

        Vector3 center2 = neighbor.Position;
        center2.y = center.y;
        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
        );

        if (cell.HasRiverThroughEdge(direction))
        {
            TriangulateEstuary(e1, e2, cell.IncomingRiver == direction);
        }
        else
        {
            waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
            waterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null) {
            Vector3 v3 = nextNeighbor.Position +
                (nextNeighbor.IsUnderwater
                    ? HexMetrics.GetFirstWaterCorner(direction.Previous())
                    : HexMetrics.GetFirstSolidCorner(direction.Previous())
                );
            v3.y = center.y;
            waterShore.AddTriangle(e1.v5, e2.v5, v3);
            waterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
            );
        }
    }

    /*
     *   y1 + *
     *      |  \
     *      +---*----+ waterY 
     *      |   ^\   |
     *      |     \  |
     *   y2 +------*-+
     */
    void TriangulateWaterfallInWater (
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float waterY
    ) {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;

        // move bottom vertices up to water level
        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        float t = (waterY - y2) / (y1 - y2);
        v3 = Vector3.Lerp(v3, v1, t);
        v4 = Vector3.Lerp(v4, v2, t);

        rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }

    void TriangulateEstuary (
        EdgeVertices e1, EdgeVertices e2, bool incomingRiver
    ) {
        waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);

        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        waterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );

        // fill gap with tri between river end and middle of water edge
        // fill entire trapezoid by adding quad on both sides of middle triangle
        estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);  // rotate left side orientation to get symmetrical geometry
        estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        estuaries.AddQuadUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), 
            new Vector2(1f, 1f), new Vector2(0f, 0f)
        );
        estuaries.AddTriangleUV(
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f)
        );
        estuaries.AddQuadUV(
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        );

        // support river flow effect using UV2
        /*
         *  1.5   1        0  -0.5  widen U so river flow spreads into water
         *  0.8  0.8      0.8  0.8  match river's V coord which go from 0.8 to 1
         *   +----+--.-.-.--+----+
         *    \  .|\ . . . /|.  /
         *     \. | \. . ./ | ./
         *      \ | .\ . /. | /
         *       \|.__\./__.|/
         *      0.7   0.5  0.3      widen flow
         *      1.15  1.1  1.15     shore connection is 50% larger
         *                          than regular conns, so end at 1.1
         */
        if (incomingRiver)
        {
            estuaries.AddQuadUV2(
                new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
                new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, 1.1f),
                new Vector2(1f, 0.8f),
                new Vector2(0f, 0.8f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f)
            );
        }
        // mirror U coords, reverse V coords like in TriangulateRiverQuad
        else {
            estuaries.AddQuadUV2(
                new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
            );
            estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
            estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
            );
        }

    }
}
