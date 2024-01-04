using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshWork {

    static Vector2[] CalcNewUVs (List<Vector2> vertices) {
        int n = vertices.Count;
        Vector2[] newUVs = new Vector2[n];
        for (int i = 0; i < n; i++) {
            newUVs[i] = new Vector2(vertices[i].x, vertices[i].y);
        }
        return newUVs;
    }

    static Vector2[] CalcNewUVs (Vector3[] vertices) {
        int n = vertices.Length;
        Vector2[] newUVs = new Vector2[n];
        for (int i = 0; i < n; i++) {
            newUVs[i] = new Vector2(vertices[i].x, vertices[i].y);
        }
        return newUVs;
    }

    static Vector3[] Vector2ListToVector3Array (List<Vector2> l) {
        return Vector2ListToVector3Array(l, 0);
    }

    static Vector3[] Vector2ListToVector3Array (List<Vector2> l, float zVal) {
        int n = l.Count;
        Vector3[] res = new Vector3[n];
        for (int i = 0; i < n; i++) {
            res[i] = new Vector3(l[i].x, l[i].y, zVal);
        }
        return res;
    }

    static void RedoMesh2DOneSided (GameObject gameObject, List<List<List<Vector2>>> vertices) {
        // Given: a MultiPolygon representation of the desired mesh, but
        // with Vector2s.

        List<Vector2> newVertices = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < vertices.Count; i++) {
            List<int> trianglesRes = new List<int>(GeometryWork.FindTrianglesForPolygon(vertices[i][0]));
            for (int j = 0; j < trianglesRes.Count; j++) {
                trianglesRes[j] += newVertices.Count;
            }
            newVertices.AddRange(vertices[i][0]);
            newTriangles.AddRange(trianglesRes);
        }

        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = Vector2ListToVector3Array(newVertices);
        mesh.triangles = newTriangles.ToArray();
        mesh.uv = CalcNewUVs(newVertices);
        mesh.RecalculateNormals();
        UnityEngine.Object.Destroy(gameObject.GetComponent<MeshCollider>());
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    public static void RedoMesh (GameObject gameObject, GJPolygonGeometry g, float width) {
        List<Vector2> vertices =  Utils.FloatCoordinatesToVector2List(g.coordinates[0]);
        Utils.JSONPolygonRingIsValid(vertices);
        List<Vector2> newVertices = new List<Vector2>(vertices);
        newVertices.RemoveAt(newVertices.Count - 1);
        List<List<List<Vector2>>> multiPolygonForm = new List<List<List<Vector2>>>();
        multiPolygonForm.Add(new List<List<Vector2>>());
        multiPolygonForm[0].Add(newVertices);
        RedoMesh(gameObject, multiPolygonForm, width);
    }

    public static void RedoMesh (GameObject gameObject, GJMultiPolygonGeometry g, float width) {

        List<List<List<Vector2>>> vertices = new List<List<List<Vector2>>>();
        List<List<List<List<float>>>> coords = g.coordinates;

        // Iterating thru polygons
        for (int i = 0; i < coords.Count; i++) {
            vertices.Add(new List<List<Vector2>>());
            // Iterating thru rings
            for (int j = 0; j < coords[i].Count; j++) {
                vertices[i].Add(new List<Vector2>());
                // Iterating thru coordinates
                for (int k = 0; k < coords[i][j].Count; k++) {
                    vertices[i][j].Add(new Vector2(coords[i][j][k][0], coords[i][j][k][1]));
                }
                Utils.JSONPolygonRingIsValid(vertices[i][j]);
                vertices[i][j].RemoveAt(vertices[i][j].Count - 1);
            }
        }

        RedoMesh(gameObject, vertices, width);
    }

    public static void RedoMesh (GameObject gameObject, List<List<List<Vector2>>> vertices, float width) {
        // Given: a MultiPolygon representation of the desired mesh, but
        // with Vector2s.

        // Width negative: 2D mesh with one side transparent.
        // Width 0: 2D mesh with both sides solid.
        // Width positive: 3D prism mesh with face(s)s described by [vertices].

        if (width < 0) {
            RedoMesh2DOneSided(gameObject, vertices);
            return;
        }

        List<Vector2> newVertices = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        // Gets triangles and vertices for one face(s) of the multipolygon,
        // like usual.
        for (int i = 0; i < vertices.Count; i++) {
            List<int> trianglesRes = new List<int>(GeometryWork.FindTrianglesForPolygon(vertices[i][0]));
            for (int j = 0; j < trianglesRes.Count; j++) {
                trianglesRes[j] += newVertices.Count;
            }
            newVertices.AddRange(vertices[i][0]);
            newTriangles.AddRange(trianglesRes);
        }

        // Copies one face(s)'s vertices to the other face(s)'s.
        int totalVertices = 2 * newVertices.Count;
        int halfTotalVertices = totalVertices / 2;
        Vector3[] allVerticesArr = new Vector3[totalVertices];
        for (int i = 0; i < totalVertices; i++) {
            if (i < halfTotalVertices) {
                allVerticesArr[i] = new Vector3(newVertices[i].x, newVertices[i].y, 0);
            } else {
                allVerticesArr[i] = new Vector3(newVertices[i - halfTotalVertices].x, newVertices[i - halfTotalVertices].y, width);
            }
        }

        // Copies triangles from one face(s) to the other.
        int halfT = newTriangles.Count;
        for (int i = 0; i < halfT; i++) {
            // newTriangles.Add(newTriangles[i] + halfTotalVertices);
            
            // Add the second face(s)'s triangles with their vertices flipped
            // to account for opposite orientation.
            newTriangles.Add(newTriangles[halfT - i - 1] + halfTotalVertices);
        }

        // Connects the two faces(s) of the prism(s). For a corresponding side,
        // we'll need two triangles to make up a rectangle.
        int numVerticesCovered = 0;
        for (int i = 0; i < vertices.Count; i++) {
            int numVerticesForThisPolygon = vertices[i][0].Count;
            for (int j = 0; j < numVerticesForThisPolygon; j++) {
                // These look like the correct orientations.
                newTriangles.Add(numVerticesCovered + j);
                newTriangles.Add(numVerticesCovered + GeometryWork.Mod(j + 1, numVerticesForThisPolygon));
                newTriangles.Add(numVerticesCovered + j + halfTotalVertices);
                newTriangles.Add(numVerticesCovered + j + halfTotalVertices);
                newTriangles.Add(numVerticesCovered + GeometryWork.Mod(j + 1, numVerticesForThisPolygon));
                newTriangles.Add(numVerticesCovered + GeometryWork.Mod(j + 1, numVerticesForThisPolygon) + halfTotalVertices);
            }
            numVerticesCovered += numVerticesForThisPolygon;
        }

        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = allVerticesArr;
        mesh.triangles = newTriangles.ToArray();
        mesh.uv = CalcNewUVs(allVerticesArr);
        mesh.RecalculateNormals();
        UnityEngine.Object.Destroy(gameObject.GetComponent<MeshCollider>());
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

}