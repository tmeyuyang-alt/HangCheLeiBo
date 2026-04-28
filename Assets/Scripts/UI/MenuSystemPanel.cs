using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuSystemPanel : MonoBehaviour
{
    private TitlePanel titlePanel;
    public Button[] buttons;

    void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[index].onClick.AddListener(() =>
            {
                if (titlePanel == null)
                    titlePanel = GameObject.FindObjectOfType<TitlePanel>();

                switch (index)
                {
                    case 0:
                      
                        break;
                    case 1:
                        UIManager.Instance.OpenPanel<HisWarningPanel>(null);
                        titlePanel?.HiddenClose();
                        break;
                    case 2:
                        UIManager.Instance.OpenPanel<HisDataPanel>(null);
                        titlePanel?.HiddenClose();
                        break;
                }
            });
        }
    }
}
