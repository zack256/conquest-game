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
    public GJMultiPolygonGeometry geometry;
}

public class GJObj {
    public string type;
    public List<GJFeature> features;
}