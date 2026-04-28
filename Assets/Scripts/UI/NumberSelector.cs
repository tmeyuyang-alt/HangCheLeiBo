using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class NumberSelector : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler,IDropHandler
{
    bool enter = false;

    private GridLayoutGroup gridlayerGroup;
    private float targetTop = 0;

    private float currentTop = 0;
    private bool smooth = false;

    public int centerNumber = 2021;

    public Vector2Int Limit = new Vector2Int(0, 5000);
    public System.Action<int> OnValueChanged = null;
    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).GetComponent<Text>().text = (centerNumber + i - transform.childCount / 2).ToString();
        }

        transform.GetChild(transform.childCount >> 1).GetComponent<Text>().color = new Color(117 / 255.0f, 133 / 255.0f, 254 / 255.0f);

        gridlayerGroup = GetComponent<GridLayoutGroup>();
        currentTop = gridlayerGroup.padding.top;
        gridlayerGroup.padding.top = -30;

    }

    public void SetSelectionValue(int value)
    {
        centerNumber = value;
        UpdateText();
    }
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("拖拽");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        enter = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        enter = false;
    }

    public void Update()
    {
        if (enter)
        {
            float v = Input.GetAxis("Mouse ScrollWheel");

            if (v > 0)
            {
                if (centerNumber - 2 <= Limit.x-2)
                {
                    return;
                }
                targetTop += 30;
            }
            else if (v < 0)
            {
                if (centerNumber + 3 > Limit.y+2)
                {
                    return;
                }
                targetTop -= 30;
            }
            //播放动画
            if(v!=0.0f)
            smooth = true;
        }

        if (smooth)
        {
            currentTop = Mathf.Lerp(currentTop, targetTop, Time.deltaTime * 10);

            if (Mathf.Abs(currentTop) >=29)
            {
                int sig = currentTop < 0 ? 1:- 1;
                currentTop = 0;
                targetTop =targetTop + 30* sig;

                smooth = false;
                currentTop = 0;
                targetTop = 0;

                centerNumber = centerNumber + 1* sig;

                //for (int i = 0; i < transform.childCount; i++)
                //{
                //    float value = (centerNumber + i - transform.childCount / 2);
                //    if (value < Limit.x || value > Limit.y)
                //    {
                //        transform.GetChild(i).GetComponent<Text>().text = "";
                //    }
                //    else
                //    {
                //        transform.GetChild(i).GetComponent<Text>().text = value.ToString();
                //    }
                //}
                UpdateText();

                OnValueChanged?.Invoke(centerNumber);
            }
            gridlayerGroup.padding.top = (int)(currentTop-30);
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
    public void UpdateText()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            float value = (centerNumber + i - transform.childCount / 2);
            if (value < Limit.x || value > Limit.y)
            {
                transform.GetChild(i).GetComponent<Text>().text = "";
            }
            else
            {
                transform.GetChild(i).GetComponent<Text>().text = value.ToString();
            }
        }
    }
    public void OnDestroy()
    {
        OnValueChanged = null;
    }
}
