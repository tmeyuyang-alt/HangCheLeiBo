using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayerButton : MonoBehaviour
{
    public CameraController controller;
    public float height;
    public float angle;
    public float distance;

    public int layer;
    void Start()
    {
        GetComponentInChildren<Button>().onClick.AddListener(() =>
        {
            controller.CenterOffset.y = height;
            controller.CameraDistance = distance;
            //controller.Angle = angle;
            AppManager.Instance.SelectedLayer(layer);
        });
    }
}
