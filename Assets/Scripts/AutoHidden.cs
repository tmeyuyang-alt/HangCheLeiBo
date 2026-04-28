using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoHidden : MonoBehaviour
{
    public CameraController controller;

    public Vector2 Angle;
    public Vector2 Height;
    public Vector2 Distance;
    private Renderer m_renderer;
    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();
    }
    void Update()
    {
        if (
        //(controller.Angle > Angle.x && controller.Angle < Angle.y)&& 
         (controller.CenterOffset.y > Height.x && controller.CenterOffset.y < Height.y)
         && (controller.CameraDistance > Distance.x && controller.CameraDistance < Distance.y))
        {
            m_renderer.enabled = false;
        }
        else
        {
            m_renderer.enabled = true;
        }
    }
}
