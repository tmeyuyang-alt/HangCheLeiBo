using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HedgehogTeam.EasyTouch;
using HighlightPlus;
using UnityEngine.EventSystems;

public class BindingHightlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //public QuickEnterOverExist Tap;
    public HighlightEffect eff;

    public void OnPointerEnter(PointerEventData eventData)
    {
       // eff.highlighted = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //eff.highlighted = false;
    }
    private void Update()
    {
        // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // RaycastHit hit;
        // bool raycast = Physics.Raycast(ray, out hit);
        // if (raycast)
        // {
        //     GameObject go = hit.collider.gameObject;
        //     
        // }
      

    }
    private void Start()
    {
        //Tap=GetComponent<QuickEnterOverExist>();
        eff=GetComponent<HighlightEffect>();

       
    }

}
