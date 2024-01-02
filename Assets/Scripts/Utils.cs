
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils {

    static Vector2 ListToVector2 (List<float> l) {
        return new Vector2(l[0], l[1]);
    }

    public static List<Vector2> FloatCoordinatesToVector2List (List<List<float>> l) {
        List<Vector2> newL = new List<Vector2>();
        for (int i = 0; i < l.Count; i++) {
            newL.Add(ListToVector2(l[i]));
        }
        return newL;
    }

    public static void PrintArray<T> (T[] arr) {
        string s = "";
        for (int i = 0; i < arr.Length; i++) {
            s += arr[i];
            s += ", ";
        }
        Debug.Log(s);
    }

    public static void PrintList<T> (List<T> l) {
        string s = "";
        for (int i = 0; i < l.Count; i++) {
            s += l[i];
            s += ", ";
        }
        Debug.Log(s);
    }

    public static List<Vector2> Vector2ListFromFloatList (List<float> l) {
        // Given a list of floats, returrns a list of Vector2s, where each
        // Vector2 is composed of pairs of floats in the list.
        List<Vector2> res = new List<Vector2>();
        for (int i = 0; i < l.Count / 2; i++) {
            res.Add(new Vector2(l[2 * i], l[2 * i + 1]));
        }
        return res;
    }

    public static void JSONPolygonRingIsValid (List<Vector2> coordinates) {
        // Not 100%
        int n = coordinates.Count;
        if (n < 4) throw new Exception("Invalid GeoJSON coordinates- must have at least 4 coordinates for a polygon.");
        if (
            (coordinates[0].x != coordinates[n - 1].x)
            ||
            (coordinates[0].y != coordinates[n - 1].y)
        ) throw new Exception("Invalid GeoJSON coordinates- first coordinate must equal the last for a polygon.");
    }

}
