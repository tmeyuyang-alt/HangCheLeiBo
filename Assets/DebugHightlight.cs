using HighlightPlus;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DebugHightlight : MonoBehaviour
{
    public HighlightEffect[] mAll;
    public bool IsDebug=false;
    
    public GameObject mCurrent;
    void Start()
    {
        if (IsDebug)
        {
            return;
        }
        mAll = GetComponentsInChildren<HighlightEffect>(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsDebug)
        {
            return;
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool raycast = Physics.Raycast(ray, out hit);
        if (raycast)
        {
            GameObject go = hit.collider.gameObject;

            if (mCurrent != null)
            {
                if (go!=mCurrent)
                {
                    mCurrent.GetComponent<HighlightEffect>().highlighted = false;
                    mCurrent = null;
                }
            }
          
            
            if (go.GetComponent<HighlightEffect>()!=null)
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    mCurrent = go;
                    // foreach (HighlightEffect effect in mAll)
                    // {
                    //     if (effect.tag!="PosObj")
                    //     {
                    //         effect.highlighted = false;
                    //     }
                    //  
                    // }
                    go.GetComponent<HighlightEffect>().highlighted = true;
                }         
            }
            // else
            // {
            //     foreach (HighlightEffect effect in mAll)
            //     {
            //         if (effect.tag!="PosObj")
            //         {
            //             effect.highlighted = false;
            //         }
            //     }
            // }
        }
        else
        {
            if (mCurrent!=null)
            {
                mCurrent.GetComponent<HighlightEffect>().highlighted = false;
                mCurrent = null;
            }
            // foreach (HighlightEffect effect in mAll)
            // {
            //     if (effect.tag!="PosObj")
            //     {
            //         effect.highlighted = false;
            //     }
            // }
        }
    }
}
