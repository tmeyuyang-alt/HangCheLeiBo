using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coordinate : MonoBehaviour
{
    private Camera m_camera;

    public Transform SelectionObject;

    public static bool ClickAxial =false;

    public bool useDistanceScale = true;

    public float scale = 1;

    private Material materialX;
    private Material materialY;
    private Material materialZ;
    public Color SelectedCol;
    public Color xUnSelectedCol;
    public Color yUnSelectedCol;
    public Color zUnSelectedCol;

    public int AxiaId = 0;

    public float offset = 0;

    private void Start()
    {
        m_camera = Camera.main.GetComponent<Camera>();

        Renderer[] renders = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renders.Length; i++)
        {
            renders[i].material.renderQueue += 2000;
        }

        materialX = transform.Find("X").GetComponent<Renderer>().material;
        materialY = transform.Find("Y").GetComponent<Renderer>().material;
        materialZ = transform.Find("Z").GetComponent<Renderer>().material;
    }
    private Vector3 forward;
    private Vector3 lastMousePosition;
    //private bool mouse_down = false;


    public void SetSelectionObject(Transform sobj)
    {
        SelectionObject = null;
        this.transform.position = sobj.transform.position;
        SelectionObject = sobj;
        this.gameObject.SetActive(true);
    }
    // private Vector3 lastPos;
    //private Vector3 hitPos;
    private void Update()
    {
        float distance = Vector3.Distance(m_camera.transform.position, transform.position);
        transform.localScale = Vector3.one * distance * scale;

        if (Input.GetMouseButtonDown(0))
        {
            //mouse_down = true;

            lastMousePosition = Input.mousePosition;
            //lastPos = transform.position;

            Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);

            RaycastHit[] hits;

            hits = Physics.RaycastAll(ray);

            if (hits != null)
            {
                if (hits.Length > 0)
                {
                    for (int i = 0; i < hits.Length; i++)
                    {
                        if (hits[i].collider.name == "X")
                        {
                            forward = transform.forward;
                            //hitPos = hits[i].point;
                            //Cursor.visible = false;//隐藏指针
                            ClickAxial = true;
                            materialX.color = SelectedCol;
                            AxiaId = 1;
                            //offset = transform.position.z- hits[i].point.z;
                            break;
                        }
                        else if (hits[i].collider.name == "Y")
                        {
                            forward = transform.up;
                            //hitPos = hits[i].point;
                            //Cursor.visible = false;//隐藏指针
                            ClickAxial = true;
                            materialY.color = SelectedCol;
                            AxiaId = 2;
                            //offset = transform.position.y - hits[i].point.y;
                            break;
                        }
                        else if (hits[i].collider.name == "Z")
                        {
                            forward = -transform.right;
                            //hitPos = hits[i].point;
                            //Cursor.visible = false;//隐藏指针
                            materialZ.color = SelectedCol;
                            AxiaId = 3;
                            ClickAxial = true;
                            //offset = transform.position.x - hits[i].point.x;
                            break;
                        }
                    }

                    if (ClickAxial)
                    {
                        Vector3 p = HitAixaPlane();


                        switch (AxiaId)
                        {
                            case 1://X
                                offset = transform.position.z - p.z;
                                break;
                            case 2://Y
                                offset = transform.position.y - p.y;
                                break;
                            case 3://Z
                                offset = transform.position.x - p.x;
                                break;
                        }
                    }
                }
                else
                {
                    ClickAxial = false;
                }
            }
            else
            {
                ClickAxial = false;
            }

        }

        if (Input.GetMouseButton(0))
        {

            if (ClickAxial)
            {
                Vector3 point = HitAixaPlane();
                switch (AxiaId)
                {
                    case 1://X
                        transform.position = new Vector3(transform.position.x, transform.position.y, offset + point.z);                        
                        break;
                    case 2://Y
                        transform.position = new Vector3(transform.position.x, offset + point.y, transform.position.z);
                        break;
                    case 3://Z
                        transform.position = new Vector3(offset + point.x, transform.position.y, transform.position.z);
                        break;
                }
            }
        }
        Highlight();
        if (Input.GetMouseButtonUp(0))
        {
            //mouse_down = false;
            forward = Vector3.zero;
            Cursor.visible = true;
            ClickAxial = false;
            materialX.color = xUnSelectedCol;
            materialY.color = yUnSelectedCol;
            materialZ.color = zUnSelectedCol;
        }
        if (SelectionObject != null)
        {
            SelectionObject.position = transform.position;
        }

        if (SelectionObject == null)
        {
            gameObject.SetActive(false);
        }


    }
    public Vector3 HitAixaPlane()
    {
        Plane plane = new Plane();
        switch (AxiaId)
        {
            case 1:
                plane.n = Vector3.up;
                break;
            case 2:
                plane.n = Vector3.forward;
                break;
            case 3:
                plane.n = Vector3.up;
                break;
        }
        plane.p1 = transform.position;
        Vector3 point = RayPlaneIntersection(m_camera.ScreenPointToRay(Input.mousePosition), plane);

        return point;
    }
    
    //一点，和一个法向量确定一个平面
    public struct Plane
    {   
        public Vector3 p1;
        public Vector3 n;
    };
    public Vector3 RayPlaneIntersection(Ray ray,Plane plane)
    {
            Vector3 p;
            float t;
            t = (mul(plane.n, plane.p1) - mul(plane.n ,ray.origin)) / (mul(plane.n , ray.direction));
            p = ray.origin + t * ray.direction;
            return p;
    }

    float mul(Vector3 v1, Vector3 v2)
    {
        float x = v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        return x;
    }
    
    private void OnDestroy()
    {
        ClickAxial = false;
    }

    public void Highlight()
    {

        //TODO 高亮提示
        Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits;
        hits = Physics.RaycastAll(ray);
        if (hits != null)
        {
            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.name == "X")
                    {
                        materialX.color = SelectedCol;
                        materialY.color = yUnSelectedCol;
                        materialZ.color = zUnSelectedCol;
                        break;
                    }
                    else if (hits[i].collider.name == "Y")
                    {
                        materialY.color = SelectedCol;
                        materialX.color = xUnSelectedCol;
                        materialZ.color = zUnSelectedCol;
                        break;
                    }
                    else if (hits[i].collider.name == "Z")
                    {
                        materialZ.color = SelectedCol;
                        materialX.color = xUnSelectedCol;
                        materialY.color = yUnSelectedCol;

                        break;
                    }
                    else
                    {
                        materialX.color = xUnSelectedCol;
                        materialY.color = yUnSelectedCol;
                        materialZ.color = zUnSelectedCol;
                    }
                }
            }
        }
    }
}
