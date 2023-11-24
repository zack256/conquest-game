using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshStuff : MonoBehaviour
{

    int Mod (int a, int b) {
        int r = a % b;
        return r < 0 ? r + b : r;
    }

    float SumOfArray (float[] l) {
        float total = 0;
        for (int i = 0; i < l.Length; i++) {
            total += l[i];
        }
        return total;
    }

    float FindAngle (float sinTheta, float cosTheta) {
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

    float[] GetInteriorAngles (List<Vector2> vertices) {
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

    List<bool> ClassifyConvexVertices (List<Vector2> vertices) {
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

    float CalcSlope (Vector2 A, Vector2 B) {
        return (B.y - A.y) / (B.x - A.x);
    }

    bool PointIsInTriangle (Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
        // Checks if a point P is inside the triangle ABC. Edge inclusive.
        // TODO: Make work for vertical lines.

        float aSlope = CalcSlope(B, C);
        float bSlope = CalcSlope(C, A);
        float cSlope = CalcSlope(A, B);

        float aYInt = B.y - aSlope * B.x;
        float bYInt = C.y - bSlope * C.x;
        float cYInt = A.y - cSlope * A.x;

        // Checks if P is on the edge of the triangle.
        // I'm aware that I check floating point inaccuracies in some
        // places and not others...maybe...
        float aRes = P.y - (aSlope * P.x + aYInt);
        float bRes = P.y - (bSlope * P.x + bYInt);
        float cRes = P.y - (cSlope * P.x + cYInt);
        if (aRes == 0 || bRes == 0 || cRes == 0) {
            return true;
        }

        // Now, we check which side of the line BC P is on.
        // If it's on the same side as A, and on the same sides
        // as corresponding CA and AB, then we mark it as inside
        // the triangle ABC.
        float aaRes = A.y - (aSlope * A.x + aYInt);
        if (Mathf.Sign(aRes) != Mathf.Sign(aaRes)) {
            return false;
        }
        float bbRes = B.y - (bSlope * B.x + bYInt);
        if (Mathf.Sign(bRes) != Mathf.Sign(bbRes)) {
            return false;
        }
        float ccRes = C.y - (cSlope * C.x + cYInt);
        if (Mathf.Sign(cRes) != Mathf.Sign(ccRes)) {
            return false;
        }

        return true;
    }

    bool VertexIsEar (List<Vector2> vertices, List<bool> isConvex, int idx) {
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

    List<int> ListOfNumbersInOrder (int n) {
        List<int> l = new List<int>();
        for (int i = 0; i < n; i++) {
            l.Add(i);
        }
        return l;
    }

    int[] FindTrianglesForMesh (List<Vector2> vertices0) {
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

    Vector2[] CalcNewUVs (List<Vector2> vertices) {
        int n = vertices.Count;
        Vector2[] newUVs = new Vector2[n];
        for (int i = 0; i < n; i++) {
            newUVs[i] = new Vector2(vertices[i].x, vertices[i].y);
        }
        return newUVs;
    }

    Vector3[] Vector2ListToVector3Array (List<Vector2> l) {
        int n = l.Count;
        Vector3[] res = new Vector3[n];
        for (int i = 0; i < n; i++) {
            res[i] = new Vector3(l[i].x, l[i].y, 0);
        }
        return res;
    }

    public void RedoMesh (List<Vector2> vertices) {
        // Given a list of 2D coordinates, replaces the mesh of this object
        // with one in the shape described by the vertices.
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = Vector2ListToVector3Array(vertices);
        mesh.triangles = FindTrianglesForMesh(vertices);
        mesh.uv = CalcNewUVs(vertices);
        mesh.RecalculateNormals();
        Destroy(gameObject.GetComponent<MeshCollider>());
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    List<Vector2> Vector2ListFromFloatList (List<float> l) {
        List<Vector2> res = new List<Vector2>();
        for (int i = 0; i < l.Count / 2; i++) {
            res.Add(new Vector2(l[2 * i], l[2 * i + 1]));
        }
        print(res.Count);
        print(res[0]);
        return res;
    }

    List<Vector2> triangle = new List<Vector2> {
        new Vector2(0, 0),
        new Vector2(3, 0),
        new Vector2(3, 4),
    };
    List<Vector2> convexQuadrilateral = new List<Vector2> {
        new Vector2(-3, -2),
        new Vector2(6, -2),
        new Vector2(8, 4),
        new Vector2(2, 6),
    };
    List<Vector2> kite = new List<Vector2> {
        new Vector2(-2, -3),
        new Vector2(0, 0),
        new Vector2(2, -4),
        new Vector2(0, 6),
    };
    List<Vector2> reversedKite = new List<Vector2> {
        new Vector2(-2, -3),
        new Vector2(0, 6),
        new Vector2(2, -4),
        new Vector2(0, 0),
    };
    List<float> advancedKite = new List<float> {
        0, 5,
        -5, -4,
        -3, -2,
        -1, -1,
        1, -1,
        3, -2,
        5, -4
    };
    List<float> arch = new List<float> {
        7, 8, 10, 14, 16, 11, 13.5f, 6.5f, 12, 7.5f, 13, 10, 11, 11, 9, 7
    };

    void Start () {
        RedoMesh(Vector2ListFromFloatList(arch));
    }

}
