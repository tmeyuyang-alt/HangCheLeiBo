using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DatePickerUI : MonoBehaviour
{
    public Transform Group;

    public Text YearText;
    public Text MonthText;
    
    public NumberSelector YearSelector;
    public NumberSelector MonthSelector;

    public NumberSelector HoursSelector;
    public NumberSelector MinutesSelector;
    public NumberSelector SecondsSelector;

    public Color DeepColour;
    public Color LightColour;
    public Transform Checkbox;

    int CurrentYear = 2021;
    int CurrentMonth = 7;
    int CurrentDay = 1;
    int Hours = 0;
    int Minutes = 0;
    int Seconds = 0;

    int CurrentHours;
    int CurrentMinutes;
    int CurrentSeconds;

    private System.Action<DateTime> OnDateChanged;

    private static DatePickerUI _Instance;

    public bool mouseEnter = false;
    
    public static DatePickerUI Instance
    {
        get
        {
            if (_Instance == null)
            {
                GameObject go = GameObject.Instantiate(Resources.Load("DatePicker")) as GameObject;
                _Instance = go.GetComponent<DatePickerUI>();
                go.transform.SetParent(GameObject.Find("Popup").transform);
     
            }
            return _Instance;
        }
    }
    void Awake()
    {
        for (int i = 7; i < 49; i++)
        {
            Transform tf = Group.GetChild(i);
            Group.GetChild(i).GetComponent<Button>().onClick.AddListener(() => OnClickDay(tf));
        }

        if (YearSelector != null)
            YearSelector.OnValueChanged += YearValueChanged;
        if (MonthSelector != null)
            MonthSelector.OnValueChanged += MonthValueChanged;

        HoursSelector.OnValueChanged += (value) =>{Hours = value; ShowDate(CurrentYear, CurrentMonth, CurrentDay); };
        MinutesSelector.OnValueChanged += (value) =>{Minutes = value; ShowDate(CurrentYear, CurrentMonth, CurrentDay); };
        SecondsSelector.OnValueChanged += (value) =>{Seconds = value; ShowDate(CurrentYear, CurrentMonth, CurrentDay); };
        ToDay();
    }

    public void ToDay()
    {
        //获取年月
        YearSelector?.SetSelectionValue(System.DateTime.Now.Year);
        MonthSelector?.SetSelectionValue(System.DateTime.Now.Month);
        CurrentDay = System.DateTime.Now.Day;
        ShowDate(CurrentYear, CurrentMonth, CurrentDay);
    }

    public void Show(System.Action<DateTime> OnChanged,DateTime defaultTime)
    {
        CurrentYear = defaultTime.Year;
        CurrentMonth = defaultTime.Month;
        CurrentDay = defaultTime.Day;

        this.transform.position = Input.mousePosition+new Vector3(0,-25f,0);
        //判断是否超出边界
        var rectTr = this.GetComponent<RectTransform>() ;

        float xMax = Input.mousePosition.x + rectTr.sizeDelta.x;
        float yMax = Input.mousePosition.y - rectTr.sizeDelta.y;

        if (xMax > Screen.width)
        {
            Vector3 v = this.transform.position;
            v.x -= rectTr.sizeDelta.x;
            transform.transform.position = v;
        }
        if (yMax < 0)
        {
            Vector3 v = this.transform.position;
            v.y += rectTr.sizeDelta.y;
            transform.transform.position = v;
        }

        OnDateChanged = OnChanged;
        Show();
    }
    public void Show()
    {
        this.gameObject.SetActive(true);
        ShowDate(CurrentYear, CurrentMonth, CurrentDay);
    }
    public void ClickYear()
    {
        YearSelector.gameObject.SetActive(!YearSelector.gameObject.activeSelf);
    }
    public void ClickMonth()
    {
        MonthSelector.gameObject.SetActive(!MonthSelector.gameObject.activeSelf);
    }
    void YearValueChanged(int value)
    {
        CurrentYear = value;
        ShowDate(CurrentYear, CurrentMonth, CurrentDay);
    }
    void MonthValueChanged(int value)
    {
        CurrentMonth = value;
        ShowDate(CurrentYear, CurrentMonth, CurrentDay);
    }
    public void ShowDate(int Year, int Month, int Day)
    {
        YearText.text = Year + "年";
        MonthText.text = Month + "月";

        string datetime = string.Format("{0}-{1}-1", Year, Month);

        var date = DateTime.Parse(datetime);
        int week = (int)date.DayOfWeek;
        int day = GetDay(date);

        //OnDateChanged?.Invoke(DateTime.Parse(string.Format("{0}-{1}-{2}", Year, Month, Day)));
        OnDateChanged?.Invoke(DateTime.Parse(string.Format("{0}-{1}-{2} {3}:{4}:{5}", Year, Month, Day,
                                                                                       Hours,Minutes,Seconds)));
        int tempYear = Month - 1 == 0 ? Year - 1 : Year;
        int tempMonth = Month - 1 == 0 ? 12 : Month-1;

        var lastMonthDay = GetDay(DateTime.Parse(string.Format("{0}-{1}-1", tempYear, tempMonth)));

        Checkbox.SetParent(Group.GetChild(7+week+ Day-1), false);

        for (int i = 7; i < 49; i++)
        {
            Transform item = Group.GetChild(i);
            int index = i - 7;

            int number = (index - week);

            item.GetComponent<Text>().text = (number).ToString();
            if (number >= 0)
            {
                item.GetComponent<Text>().text = ((number % day) + 1).ToString();

                item.GetComponent<Text>().color = DeepColour;

                item.name = ((number % day) + 1).ToString(); 

                item.GetComponent<Button>().interactable = true;
            }
            else
            {
                item.GetComponent<Text>().text = (lastMonthDay + number+1).ToString();

                item.GetComponent<Text>().color = LightColour;


                item.GetComponent<Button>().interactable = false;
            }

            if (number >= day)
            {
                item.GetComponent<Text>().color = LightColour;

                item.GetComponent<Button>().interactable = false;
            }
        }
    }

    public int GetDay(DateTime dt)
    {
        int day = DateTime.DaysInMonth(dt.Year, dt.Month);
        return day;
    }


    public void SetMonth(int offset = 1)
    {
        CurrentMonth += offset;

        if (CurrentMonth <= 0)
            CurrentMonth = 12;
        else if (CurrentMonth > 12)
            CurrentMonth = 1;

        ShowDate(CurrentYear, CurrentMonth, CurrentDay);
    }

    public void SetYear(int offset)
    {
        ShowDate(CurrentYear += offset, CurrentMonth, CurrentDay);
    }
    // Update is called once per frame
    void Update()
    {
        if(!mouseEnter)
        {
            if (Input.GetMouseButton(0))
            { 
                this.gameObject.SetActive(false);
            }
        }
        //月份控制
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {

            CurrentMonth--;
            if (CurrentMonth == 0)
            {
                CurrentMonth = 12;
                --CurrentYear;
            }
            ShowDate(CurrentYear, CurrentMonth, CurrentDay);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            CurrentMonth++;
            if (CurrentMonth == 13)
            {
                CurrentMonth = 1;
                ++CurrentYear;
            }
            ShowDate(CurrentYear, CurrentMonth, CurrentDay);
        }
        //年份控制
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ShowDate(--CurrentYear, CurrentMonth, CurrentDay);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ShowDate(++CurrentYear, CurrentMonth, CurrentDay);
        }
    }

    public void ClosePanel()
    {
        YearSelector.gameObject.SetActive(false);
        MonthSelector.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
        OnDateChanged = null;
    }

    public void OnClickDay(Transform tf)
    {
        CurrentDay = int.Parse(tf.name);
        Checkbox.SetParent(tf, false);
        ShowDate(CurrentYear, CurrentMonth, CurrentDay);
    }

    public void MouseEnterPanel()
    {
        mouseEnter = true;
    }
    public void MouseExitPanel()
    {
        mouseEnter = false;
    }

    public void OnDestroy()
    {
        OnDateChanged = null;
    }
}
