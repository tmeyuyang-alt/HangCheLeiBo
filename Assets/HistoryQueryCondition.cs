using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HistoryQueryCondition : MonoBehaviour
{
    public List<HistoryToggleItem> devices;
    public List<HistoryToggleItem> conditions;

    [ContextMenu("Add")]
    public void Add()
    {
        // items=new List<HistoryToggleItem>();
        // items = transform.GetComponentsInChildren<HistoryToggleItem>().ToList();
    }
}
