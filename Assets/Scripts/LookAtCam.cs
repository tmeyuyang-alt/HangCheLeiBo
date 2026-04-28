using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCam : MonoBehaviour
{
    private Transform tf_camera;

    public bool Inversion = false;


    // Update is called once per frame
    void Update()
    {
        tf_camera = Camera.main.transform;



        transform.LookAt((Inversion ? -tf_camera.forward : tf_camera.forward) + transform.position);

    }
}
