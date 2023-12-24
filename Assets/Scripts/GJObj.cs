using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GJGeometry {
    public string type;
    public List<List<List<float>>> coordinates;
}

public class GJFeature {
    public string type;
    public GJGeometry geometry;
}

public class GJObj {
    public string type;
    public List<GJFeature> features;
}