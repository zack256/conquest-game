using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

using static GJObj;

public class LoadJSON
{
    static string GetDataFilePath (string relPath) {
        return Application.dataPath + relPath;
    }
    public static string ReadJSON (string relPath) {
        string absPath = GetDataFilePath(relPath);
        using (StreamReader r = new StreamReader(absPath)) {
            string JSONString = r.ReadToEnd();
            return JSONString;
        }
    }
    public static GJObj GetGJObjFromJSON (string relPath) {
        string JSONString = ReadJSON(relPath);
        return JsonConvert.DeserializeObject<GJObj>(JSONString);
    }
}
