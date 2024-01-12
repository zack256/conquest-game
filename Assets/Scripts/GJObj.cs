using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GJPolygonGeometry {
    public string type;
    // List of "rings", the first one is the exterior ring and the rest (if
    // any) are interior rings (holes). Each ring is a list of coordinate pairs.
    public List<List<List<float>>> coordinates;
}

public class GJMultiPolygonGeometry {
    public string type;
    // List of polygon-type coordinates (see above)
    public List<List<List<List<float>>>> coordinates;
}

public class GJFeature {
    public string type;

    // Swap between accepting Polygon vs. MultiPolygon.
    // public GJPolygonGeometry geometry;
    public GJMultiPolygonGeometry geometry;

}

public class GJObj {
    public string type;
    public List<GJFeature> features;
}


public class MultiPolygon {
    public List<List<List<Vector2>>> coordinates;

    public MultiPolygon (GJPolygonGeometry g) {
        // Assumes one ring (the outside one)
        List<Vector2> coordinates = Utils.FloatCoordinatesToVector2List(g.coordinates[0]);
        Utils.JSONPolygonRingIsValid(coordinates);
        coordinates.RemoveAt(coordinates.Count - 1);
        this.coordinates = new List<List<List<Vector2>>>();
        this.coordinates.Add(new List<List<Vector2>>());
        this.coordinates[0].Add(coordinates);
    }

    public MultiPolygon (GJMultiPolygonGeometry g) {
        this.coordinates = new List<List<List<Vector2>>>();
        for (int i = 0; i < g.coordinates.Count; i++) {
            // Assumes one ring (the outside one)
            List<Vector2> ring = Utils.FloatCoordinatesToVector2List(g.coordinates[i][0]);
            Utils.JSONPolygonRingIsValid(ring);
            ring.RemoveAt(ring.Count - 1);
            List<List<Vector2>> polygon = new List<List<Vector2>>();
            polygon.Add(ring);
            this.coordinates.Add(polygon);
        }
    }

    /**
    public Vector2 GetCenter () {
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        for (int i = 0; i < coordinates.Count; i++) {
            for (int j = 0; j < coordinates[i].Count; j++) {
                // Should only have 1 ring tho.
                for (int k = 0; k < coordinates[i][j].Count; k++) {
                    Vector2 coordinate = coordinates[i][j][k];
                    minX = Math.Min(minX, coordinate.x);
                    maxX = Math.Max(maxX, coordinate.x);
                    minY = Math.Min(minY, coordinate.y);
                    maxY = Math.Max(maxY, coordinate.y);
                }
            }
        }
        float midX = (minX + maxX) / 2;
        float midY = (minY + maxY) / 2;
        return new Vector2(midX, midY);
    }
    **/
}