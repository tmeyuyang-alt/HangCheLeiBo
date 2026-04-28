using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PasswordShow:MonoBehaviour
{
    public Sprite[] icons;
    public bool status = false;
    public InputField input;
    void Start()
    {
        
        GetComponent<Button>().onClick.AddListener(() =>
        {
            status = !status;

            if (status)
            {
                input.contentType = InputField.ContentType.Standard;

                GetComponent<Image>().sprite = icons[0];
            }
            else
            {
                input.contentType = InputField.ContentType.Password;

                GetComponent<Image>().sprite = icons[1];
            }
            input.Select();
        });   
    }
}
