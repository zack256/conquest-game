using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayScript : MonoBehaviour
{

    public Material mat;

    void Update() {
        Ray ray = CameraScript.GetCameraRay();
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            GameObject hitted = hit.transform.gameObject;
            hitted.GetComponent<Renderer>().material = mat;
        }
    }
}
