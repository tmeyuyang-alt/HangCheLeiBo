using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditablePosUI : MonoBehaviour
{

    public string key = "";

    private void Start()
    {
        Vector3 pos;

        if (PosManager.GetPos(key, out pos))
        {
            transform.position = pos;
        }
    }
    /// <summary>
    /// 取消选中
    /// </summary>
    public void Selected()
    { 

    }

    /// <summary>
    /// 选中
    /// </summary>
    public void Unselected()
    { 
        
    }

    public void Moved()
    { 
    
    }

    //public void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.S))
    //    {
    //        Position p = new Position();
    //        p.x = transform.position.x;
    //        p.y = transform.position.y;
    //        p.z = transform.position.z;

    //        PosManager.SetPos(key,p);
    //       PosManager.Save();
    //    }
    //}
}
public class Position
{
    public float x;
    public float y;
    public float z;

    //public Position(float x,float y,float z)
    //{
    //    this.x = x;
    //    this.y = y;
    //    this.z = z;
    //}
    //public Position(Vector3 v)
    //{ x = v.x; y = v.y; z = v.z; }
    //public Vector3 ToUnity()
    //{
    //    return new Vector3(x, y, z);
    //}
}