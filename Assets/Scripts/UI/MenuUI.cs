using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{

    public System.Action<string> OnSelected;
    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            int index = i;
            transform.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
            {
                OnSelected?.Invoke(transform.GetChild(index).name);

                this.gameObject.SetActive(false);
            });
        }
    }
}
