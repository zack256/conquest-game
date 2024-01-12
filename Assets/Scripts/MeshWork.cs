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

    static Vector3 NormalOfTriangle (Vector3 A, Vector3 B, Vector3 C) {
        // Assumes triangle goes A -> B -> C.
        Vector3 AB = B - A;
        Vector3 BC = C - B;
        return Vector3.Cross(AB, BC);
    }

    static int CheckTriangleAndReturnThirdVertex (int AIdx, int BIdx, int CIdx, int pointIdx, int neighborIdx) {
        if (AIdx == pointIdx && BIdx == neighborIdx) return CIdx;
        if (AIdx == pointIdx && CIdx == neighborIdx) return BIdx;
        if (BIdx == pointIdx && AIdx == neighborIdx) return CIdx;
        if (BIdx == pointIdx && CIdx == neighborIdx) return AIdx;
        if (CIdx == pointIdx && AIdx == neighborIdx) return BIdx;
        if (CIdx == pointIdx && BIdx == neighborIdx) return AIdx;
        return -1;
    }

    static void RedoMesh2DOneSided (GameObject gameObject, MultiPolygon multiPolygon) {
        // Given: a MultiPolygon representation of the desired mesh

        List<List<List<Vector2>>> vertices = multiPolygon.coordinates;

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

    public static void RedoMesh (GameObject gameObject, MultiPolygon multiPolygon, float width) {
        // Given: a MultiPolygon representation of the desired mesh

        // Width negative: 2D mesh with one side transparent.
        // Width 0: 2D mesh with both sides solid.
        // Width positive: 3D prism mesh with face(s)s described by [vertices].

        if (width < 0) {
            RedoMesh2DOneSided(gameObject, multiPolygon);
            return;
        }

        List<List<List<Vector2>>> vertices = multiPolygon.coordinates;

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

        if (width != 0) {
            // Connects the two faces(s) of the prism(s). For a corresponding side,
            // we'll need two triangles to make up a rectangle.
            int numVerticesCovered = 0;
            for (int i = 0; i < vertices.Count; i++) {
                int numVerticesForThisPolygon = vertices[i][0].Count;
                for (int j = 0; j < numVerticesForThisPolygon; j++) {

                    int pointIdx = numVerticesCovered + j;
                    int neighborIdx = numVerticesCovered + GeometryWork.Mod(j + 1, numVerticesForThisPolygon);
                    int acrossIdx = pointIdx + halfTotalVertices;
                    int diagIdx = neighborIdx + halfTotalVertices;
                    int thirdIdx = -1;

                    for (int k = 0; k < newTriangles.Count; k += 3) {
                        thirdIdx = CheckTriangleAndReturnThirdVertex(
                            newTriangles[k],
                            newTriangles[k + 1],
                            newTriangles[k + 2],
                            pointIdx,
                            neighborIdx);
                        if (thirdIdx != -1) break;
                    }

                    if (thirdIdx == -1) throw new Exception("Something went wrong, can't find the third vertex?");
                        
                    Vector3 A = allVerticesArr[pointIdx];
                    Vector3 E = allVerticesArr[thirdIdx];
                    Vector3 AE = E - A;

                    Vector3 n;
                    float cosTheta;

                    n = NormalOfTriangle(allVerticesArr[pointIdx], allVerticesArr[neighborIdx], allVerticesArr[acrossIdx]);
                    cosTheta = (Vector3.Dot(AE, n)) / (AE.magnitude * n.magnitude);
                    if (cosTheta > 0) {
                        newTriangles.Add(pointIdx);
                        newTriangles.Add(acrossIdx);
                        newTriangles.Add(neighborIdx);
                    } else {
                        newTriangles.Add(pointIdx);
                        newTriangles.Add(neighborIdx);
                        newTriangles.Add(acrossIdx);
                    }

                    n = NormalOfTriangle(allVerticesArr[acrossIdx], allVerticesArr[neighborIdx], allVerticesArr[diagIdx]);
                    cosTheta = (Vector3.Dot(AE, n)) / (AE.magnitude * n.magnitude);
                    if (cosTheta > 0) {
                        newTriangles.Add(acrossIdx);
                        newTriangles.Add(diagIdx);
                        newTriangles.Add(neighborIdx);
                    } else {
                        newTriangles.Add(acrossIdx);
                        newTriangles.Add(neighborIdx);
                        newTriangles.Add(diagIdx);
                    }
                }
                numVerticesCovered += numVerticesForThisPolygon;
            }
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