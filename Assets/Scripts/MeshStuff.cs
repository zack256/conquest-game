﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshStuff {

    static int Mod (int a, int b) {
        int r = a % b;
        return r < 0 ? r + b : r;
    }

    static float SumOfArray (float[] l) {
        float total = 0;
        for (int i = 0; i < l.Length; i++) {
            total += l[i];
        }
        return total;
    }

    static float FindAngle (float sinTheta, float cosTheta) {
        // Given a sin and cos of an angle, this should return the
        // correct angle in the correct quadrant.
        float presumedTheta = Mathf.Asin(sinTheta);
        if (cosTheta < 0) {
            return Mathf.PI - presumedTheta;
        } else {
            if (sinTheta < 0) {
                return presumedTheta + 2 * Mathf.PI;
            } else {
                return presumedTheta;
            }
        }
    }

    static float[] GetInteriorAngles (List<Vector2> vertices) {
        // Calculates the interior angle in radians of each
        // vertex's interior angle.

        int n = vertices.Count;
        float[] theta = new float[n];

        for (int i = 0; i < n; i++) {
            
            // For a vertex B, get the 2 adjacent vertices A and C
            // and then calculate the vectors BA and BC.
            Vector2 B = vertices[i];
            Vector2 A = vertices[Mod(i - 1, n)];
            Vector2 C = vertices[Mod(i + 1, n)];
            Vector2 BA = A - B;
            Vector2 BC = C - B;

            // Finds sin and cos of angle to get correct angle.
            float crossMag = BA.x * BC.y - BA.y * BC.x;
            float dotProduct = BA.x * BC.x + BA.y * BC.y;
            float magnitudesProduct = BA.magnitude * BC.magnitude;
            float sinTheta = crossMag / magnitudesProduct;
            float cosTheta = dotProduct / magnitudesProduct;
            theta[i] = FindAngle(sinTheta, cosTheta);

        }

        // Depending on the shape and how the coordinates were listed, we either
        // obtained the interior angles (good) or the exterior ones (bad). We
        // compare with the theoretical values (might not be exact because of
        // floats), and shift if nescessary.
        float angleSum = SumOfArray(theta);
        float theoreticalInteriorAngleSum = Mathf.PI * (n - 2);
        float theoreticalExteriorAngleSum = theoreticalInteriorAngleSum + 4 * Mathf.PI;
        bool gotInteriorAngles = Mathf.Abs(angleSum - theoreticalInteriorAngleSum) < Mathf.Abs(angleSum - theoreticalExteriorAngleSum);
        if (!gotInteriorAngles) {
            for (int i = 0; i < n; i++) {
                theta[i] = 2 * Mathf.PI - theta[i];
            }
        }

        return theta;
    }

    static List<bool> ClassifyConvexVertices (List<Vector2> vertices) {
        // Classifies verticies as convex [interior] angles or not,
        // where convex means < 180 degrees.
        int n = vertices.Count;
        List<bool> isConvex = new List<bool>();
        float[] theta = GetInteriorAngles(vertices);
        for (int i = 0; i < n; i++) {
            isConvex.Add(theta[i] < Mathf.PI);
        }
        return isConvex;
    }

    static float CalcSlope (Vector2 A, Vector2 B) {
        return (B.y - A.y) / (B.x - A.x);
    }

    static int PointIsInTriangleHelper (Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
        // O: P is definetly not in ABC (P on a)
        // 1: P is definetly in ABC.
        // -1: P might be in ABC (on different side than A for side a though)

        if (B.x == C.x) {
            // Vertical line.
            if (
                (P.x == B.x)
                &&
                (
                    (B.y < C.y && P.y >= B.y && P.y <= C.y)
                ||
                    (C.y < B.y && P.y >= C.y && P.y <= B.y)
                )
            ) return 1;
            if (Mathf.Sign(P.x - B.x) != Mathf.Sign(A.x - B.x)) return 0;
            return -1;
        }

        float aSlope = CalcSlope(B, C);
        float aYInt = B.y - aSlope * B.x;
        float aRes = P.y - (aSlope * P.x + aYInt);
        if (aRes == 0) return 1;
        float aaRes = A.y - (aSlope * A.x + aYInt);
        if (Mathf.Sign(aRes) != Mathf.Sign(aaRes)) return 0;
        return -1;
    }

    static bool PointIsInTriangle (Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
        int aRes = PointIsInTriangleHelper(A, B, C, P);
        int bRes = PointIsInTriangleHelper(B, C, A, P);
        int cRes = PointIsInTriangleHelper(C, A, B, P);
        if (aRes == 1 || bRes == 1 || cRes == 1) return true;
        return aRes != 0 && bRes != 0 && cRes != 0;
    }

    static bool VertexIsEar (List<Vector2> vertices, List<bool> isConvex, int idx) {
        // An acceptable test for checking if a vertex is an ear or not is
        // if the vertex [interior angle] is convex and the the triangle formed
        // by it an its neighbors doesn't contain any other vertices of the
        // polygon.
        if (!isConvex[idx]) {
            return false;
        }

        int n = vertices.Count;
        int leftIdx = Mod(idx - 1, n);
        int rightIdx = Mod(idx + 1, n);
        for (int i = 0; i < n; i++) {
            if (i == leftIdx || i == idx || i == rightIdx) {
                continue;
            }
            if (PointIsInTriangle(vertices[leftIdx], vertices[idx], vertices[rightIdx], vertices[i])) {
                return false;
            }
        }
        return true;
    }

    static List<int> ListOfNumbersInOrder (int n) {
        List<int> l = new List<int>();
        for (int i = 0; i < n; i++) {
            l.Add(i);
        }
        return l;
    }

    static int[] FindTrianglesForMesh (List<Vector2> vertices0) {
        // Partitions a polygon (described by [vertices]) into n-2 triangles,
        // where [n] is the number of vertices. Uses the ear clipping method,
        // where until we reach 3 sides, we take away 2 edges and add another
        // (kinda), which leads to n-2 triangles.
        // Also, we return an int array of size 3*(n-2), where every 3 elements
        // corresponds to the indices of [vertices] for that triangle.

        // The implementation of this algorithm can be simplified/optimized.
        // Maybe it will be at some point!

        // Copies vertices because it'll help if we can modify the list during
        // the algorithm.
        List<Vector2> vertices = new List<Vector2>(vertices0);  // Copies vertices

        int n = vertices.Count;
        int[] triangles = new int[3 * (n - 2)];
        List<int> idxs = ListOfNumbersInOrder(n);

        for (int i = 0; i < n - 2; i++) {
            int numVerticesRemaining = vertices.Count;
            List<bool> isConvex = ClassifyConvexVertices(vertices);
            for (int j = 0; j < numVerticesRemaining; j++) {
                if (VertexIsEar(vertices, isConvex, j)) {
                    int leftIdx = Mod(j - 1, numVerticesRemaining);
                    int rightIdx = Mod(j + 1, numVerticesRemaining);
                    Vector2 A = vertices[leftIdx];
                    Vector2 B = vertices[j];
                    Vector2 C = vertices[rightIdx];
                    Vector2 BA = A - B;
                    Vector2 BC = C - B;

                    // Pretty sure I calculate the direction of the cross
                    // product's normal vector so that the triangles are
                    // all oriented the same way.
                    float signedCross = BA.y * BC.x - BA.x * BC.y;

                    triangles[3 * i + 1] = idxs[j];
                    if (signedCross < 0) {
                        triangles[3 * i] = idxs[leftIdx];
                        triangles[3 * i + 2] = idxs[rightIdx];
                    } else {
                        triangles[3 * i] = idxs[rightIdx];
                        triangles[3 * i + 2] = idxs[leftIdx];
                    }

                    vertices.RemoveAt(j);
                    idxs.RemoveAt(j);
                    break;

                }
            }
        }

        return triangles;
    }

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

    /**
    public static void RedoMesh (GameObject gameObject, List<Vector2> vertices) {
        // Given a list of 2D coordinates, replaces the mesh of this object
        // with one in the shape described by the vertices.
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = Vector2ListToVector3Array(vertices);
        mesh.triangles = FindTrianglesForMesh(vertices);
        mesh.uv = CalcNewUVs(vertices);
        mesh.RecalculateNormals();
        UnityEngine.Object.Destroy(gameObject.GetComponent<MeshCollider>());
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }
    **/

    public static void RedoMesh (GameObject gameObject, List<List<List<Vector2>>> vertices) {

        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        List<Vector2> newVertices = new List<Vector2>();
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < vertices.Count; i++) {
            List<int> trianglesRes = new List<int>(FindTrianglesForMesh(vertices[i][0]));
            for (int j = 0; j < trianglesRes.Count; j++) {
                trianglesRes[j] += newVertices.Count;
            }
            newVertices.AddRange(vertices[i][0]);
            newTriangles.AddRange(trianglesRes);
        }

        mesh.vertices = Vector2ListToVector3Array(newVertices);
        mesh.triangles = newTriangles.ToArray();
        mesh.uv = CalcNewUVs(newVertices);
        mesh.RecalculateNormals();
        UnityEngine.Object.Destroy(gameObject.GetComponent<MeshCollider>());
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    /**
    public static void RedoMesh (GameObject gameObject, GJPolygonGeometry g) {
        List<Vector2> vertices =  Utils.FloatCoordinatesToVector2List(g.coordinates[0]);
        if (vertices.Count < 4) throw new Exception("Invalid GeoJSON coordinates- must have at least 4 coordinates for a polygon.");
        if (
            (vertices[0].x != vertices[vertices.Count - 1].x)
            ||
            (vertices[0].y != vertices[vertices.Count - 1].y)
        ) throw new Exception("Invalid GeoJSON coordinates- first coordinate must equal the last for a polygon.");
        List<Vector2> newVertices = new List<Vector2>(vertices);
        newVertices.RemoveAt(newVertices.Count - 1);
        RedoMesh(gameObject, newVertices);
    }
    **/

    public static void RedoMesh (GameObject gameObject, GJMultiPolygonGeometry g) {

        List<List<List<Vector2>>> vertices = new List<List<List<Vector2>>>();

        List<List<List<List<float>>>> coords = g.coordinates;
        for (int i = 0; i < coords.Count; i++) {
            vertices.Add(new List<List<Vector2>>());
            for (int j = 0; j < coords[i].Count; j++) {
                vertices[i].Add(new List<Vector2>());
                for (int k = 0; k < coords[i][j].Count; k++) {
                    vertices[i][j].Add(new Vector2(coords[i][j][k][0], coords[i][j][k][1]));
                }
                vertices[i][j].RemoveAt(vertices[i][j].Count - 1);
            }
        }

        // if (vertices.Count < 4) throw new Exception("Invalid GeoJSON coordinates- must have at least 4 coordinates for a polygon.");
        // if (
        //     (vertices[0].x != vertices[vertices.Count - 1].x)
        //     ||
        //     (vertices[0].y != vertices[vertices.Count - 1].y)
        // ) throw new Exception("Invalid GeoJSON coordinates- first coordinate must equal the last for a polygon.");
        // List<Vector2> newVertices = new List<Vector2>(vertices);
        // newVertices.RemoveAt(newVertices.Count - 1);
        RedoMesh(gameObject, vertices);
    }

    /**
    public static void RedoMesh (GameObject gameObject, GJObj g) {
        List<Vector2> vertices =  Utils.FloatCoordinatesToVector2List(g.features[0].geometry.coordinates[0]);
        if (vertices.Count < 4) throw new Exception("Invalid GeoJSON coordinates- must have at least 4 coordinates for a polygon.");
        if (
            (vertices[0].x != vertices[vertices.Count - 1].x)
            ||
            (vertices[0].y != vertices[vertices.Count - 1].y)
        ) throw new Exception("Invalid GeoJSON coordinates- first coordinate must equal the last for a polygon.");
        List<Vector2> newVertices = new List<Vector2>(vertices);
        newVertices.RemoveAt(newVertices.Count - 1);
        RedoMesh(gameObject, newVertices);
    }
    **/

}