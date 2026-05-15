using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatChange : MonoBehaviour
{
    public Material originalMat;
    public Material changedMat;
    
    public GameObject targetMesh;

    private void OnEnable()
    {
        targetMesh.GetComponent<MeshRenderer>().material = originalMat;
    }

    private void OnDisable()
    {
        targetMesh.GetComponent<MeshRenderer>().material = changedMat;
    }
}
