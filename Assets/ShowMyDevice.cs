using HedgehogTeam.EasyTouch;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShowMyDevice : MonoBehaviour
{
    public List<GameObject> mAllPidai;
    [Tooltip("真正需要检测的 3D 层")]
    public LayerMask worldLayers = ~0;          // 缺省为全部

    public float rayDistance = 100f;

    public List<QuickTap> mALLObjs;

    private void Start()
    {
        foreach (var obj in mALLObjs)
        {
            obj.onTap.AddListener(OnTap);
        }
    }

    private void OnTap(Gesture arg0)
    {
        ShowMyPidai(arg0.pickedObject.name);
    }



    public void ShowMyPidai(string arg)
    {
        print(arg);
        foreach (var ctrl in mAllPidai)
        {
            if (ctrl.name == arg)
            {
                ctrl.gameObject.SetActive(true);
            }
            else
            {
                ctrl.gameObject.SetActive(false);
            }
        }
    }
    void Update()
    {
        //if (Input.GetMouseButtonDown(0)) // 检测鼠标左键点击
        //{
        //    if (EventSystem.current.IsPointerOverGameObject(-1))
        //        return;     // 指针在 UI 上 ? 不做 3D 射线
        //    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
        //    if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, worldLayers))
        //    {
        //        // 在这里处理点击到的 3D 物体
        //        Debug.Log($"Hit {hit.collider.name}");
        //        if (hit.collider.gameObject != null)
        //        {
        //            ShowMyPidai(hit.collider.gameObject.name);
        //        }
        //    }
          
        //}
    }
}
