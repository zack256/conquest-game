﻿using System.Collections;
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

}
