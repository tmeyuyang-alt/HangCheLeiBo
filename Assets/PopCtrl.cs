using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PopCtrl : MonoBehaviour
{
    public static PopCtrl Instance;
    public GameObject window;
    public GameObject warningWindow;


    public Text info, warningInfo;

    private void Awake()
    {
        Instance = this;
    }
    public void ShowPop(string arg)
    {
        window.SetActive(true);
        info.text = arg;
    }
    public void ShowWarningPop(string arg)
    {
        warningWindow.SetActive(true);
        warningInfo.text = arg;
    }
    public async void DelayClose()
    {
        await Task.Delay(1000);
        window.SetActive(false);
    }

}
