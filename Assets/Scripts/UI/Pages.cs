using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Pages : MonoBehaviour
{
    public Button LeftArrowBtn;
    public Button RightArrowBtn;

    private int m_IndexNumber = -1;

    public int MaxPageNum = 10;

    public Transform Group;

    private int dataCount = 0;

    public Sprite Normal;
    public Sprite Selected;

    private int limitNumber = 23;

    private int pagesIndex = 0;
    private int maxpagesSum = 10;//页码翻页次数
    public int IndexNumber
    {
        set {
            int LastIndex = IndexNumber;
            m_IndexNumber = value;
            UpdateUI(LastIndex);
        }
        get { return m_IndexNumber; }
    }

    public System.Action<int> OnPageNumChanged;

    private void UpdateUI(int LastIndex)
    {
        if (Group != null)
        {
            if (LastIndex != -1)
            {
                Group.GetChild(LastIndex+1).GetComponent<Image>().sprite = Normal;
            }
            Group.GetChild(m_IndexNumber+1).GetComponent<Image>().sprite = Selected;

        }
    }

    public void UpdatePageNumber(int limitNumber, int Sum)
    {
        //int pageNum = Sum / limitNumber;

        //int temp = Sum - pageNum * limitNumber;
        //if (temp > 0)
        //{
        //    pageNum = pageNum + 1;
        //}
        this.limitNumber = limitNumber;

        int pageNum = Mathf.CeilToInt(Sum * 1.0f / limitNumber);

        IndexNumber = 0;
        dataCount = Sum;

        maxpagesSum = Mathf.CeilToInt(pageNum * 1.0f / MaxPageNum);


        for (int i = 1; i < Group.childCount - 1; i++)
        {
            if (i <= pageNum)
                Group.GetChild(i).gameObject.SetActive(true);
            else
                Group.GetChild(i).gameObject.SetActive(false);
        }


    }

    private void UpdatePage()
    {
        //叶总数
        int pageNum = Mathf.CeilToInt(dataCount * 1.0f / limitNumber);

        int count = MaxPageNum;

        if (pagesIndex >= maxpagesSum - 1)
        {
            count = pageNum - (maxpagesSum - 1) * MaxPageNum;
        }

        for (int i = 1; i < Group.transform.childCount - 1; i++)
        {
            if (count >= i)
            {
                Group.GetChild(i).gameObject.SetActive(true);
                Group.GetChild(i).GetComponentInChildren<Text>().text = (i + pagesIndex * MaxPageNum).ToString();
            }
            else
            {
                Group.GetChild(i).gameObject.SetActive(false);
                Group.GetChild(i).GetComponentInChildren<Text>().text = (i + pagesIndex * MaxPageNum).ToString();
            }
        }
    }
    public void Awake()
    {
        LeftArrowBtn.onClick.AddListener(() =>
        {
            pagesIndex--;
            pagesIndex = Mathf.Max(pagesIndex, 0);
            IndexNumber = 0;
            UpdatePage();

            if (this.OnPageNumChanged != null)
                this.OnPageNumChanged(0 + pagesIndex * MaxPageNum);
        });
        RightArrowBtn.onClick.AddListener(() =>
        {
            pagesIndex++;
            pagesIndex = Mathf.Min(pagesIndex, maxpagesSum-1);
            IndexNumber = 0;
            UpdatePage();

            if (this.OnPageNumChanged != null)
                this.OnPageNumChanged(0 + pagesIndex * MaxPageNum);
        });


        for (int i = 1; i < Group.transform.childCount - 1; i++)
        {

            int index = i - 1;
            Group.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
            {
                IndexNumber = index;
                if (this.OnPageNumChanged != null)
                {
                    this.OnPageNumChanged(index+ pagesIndex*MaxPageNum);
                }
            });
        }

        for (int i = 1; i < Group.transform.childCount-1; i++)
        {
            Group.GetChild(i).gameObject.SetActive(true);
            Group.GetChild(i).GetComponentInChildren<Text>().text = i.ToString();
        }

    }
    public void OnDestroy()
    {
        this.OnPageNumChanged = null;
    
    }
}
