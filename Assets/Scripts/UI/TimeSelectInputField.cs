using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeSelectInputField : MonoBehaviour
{
    public Text text;

    public void Awake()
    {
        //Show
        GetComponent<Button>().onClick.AddListener(() =>
        {
            var date =System.DateTime.Now;
            if (text.text != "")
            {
                date = System.DateTime.Parse(text.text);
            }
            DatePickerUI.Instance.Show((data) =>
            {
                text.text = data.ToString();
            },date);
        });
    }


}
