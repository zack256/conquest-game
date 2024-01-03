using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshStuff {

    static Vector2[] CalcNewUVs (List<Vector2> vertices) {
        int n = vertices.Count;
        Vector2[] newUVs = new Vector2[n];
        for (int i = 0; i < n; i++) {
            newUVs[i] = new Vector2(vertices[i].x, vertices[i].y);
        }
        return newUVs;
    }

    static Vector3[] Vector2ListToVector3Array (List<Vector2> l) {
        int n = l.Count;
        Vector3[] res = new Vector3[n];
        for (int i = 0; i < n; i++) {
            res[i] = new Vector3(l[i].x, l[i].y, 0);
        }
        return res;
    }

    public static void RedoMesh (GameObject gameObject, List<List<List<Vector2>>> vertices) {
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

    public static void RedoMesh (GameObject gameObject, GJPolygonGeometry g) {
        List<Vector2> vertices =  Utils.FloatCoordinatesToVector2List(g.coordinates[0]);
        Utils.JSONPolygonRingIsValid(vertices);
        List<Vector2> newVertices = new List<Vector2>(vertices);
        newVertices.RemoveAt(newVertices.Count - 1);
        // RedoMesh(gameObject, newVertices);
        List<List<List<Vector2>>> multiPolygonForm = new List<List<List<Vector2>>>();
        multiPolygonForm.Add(new List<List<Vector2>>());
        multiPolygonForm[0].Add(newVertices);
        RedoMesh(gameObject, multiPolygonForm);
    }

    public static void RedoMesh (GameObject gameObject, GJMultiPolygonGeometry g) {

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

        RedoMesh(gameObject, vertices);
    }

}