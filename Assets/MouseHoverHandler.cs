using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MouseHoverHandler : MonoBehaviour
{
    public UnityEvent OnClick;
    
    private void OnMouseDown()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("点了一下");
            OnClick.Invoke();
            EventSystem.current.UpdateModules();
            // my code 
        }
        else
        {
           
        }
       
    }
    public static bool IsPointerOverGameObject(GameObject gameObject)
   {
    PointerEventData eventData = new PointerEventData(EventSystem.current);
    eventData.position = Input.mousePosition;
    List<RaycastResult> raysastResults = new List<RaycastResult>();
    EventSystem.current.RaycastAll(eventData, raysastResults);
    return raysastResults.Any(x => x.gameObject == gameObject);
    }

}

