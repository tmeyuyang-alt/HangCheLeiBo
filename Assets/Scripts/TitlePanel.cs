using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitlePanel : UIPanel
{
    // Start is called before the first frame update
    public Button ParamBtn;
    public Button BackBtn;
    public Button AccountManagerBtn;
    public Button CloseBtn;

    public Text connectText;
    public void Start()
    {
        ParamBtn.interactable = GlobalInfo.user.permission == 3;//|| GlobalInfo.user.permission == 2;
        AccountManagerBtn.interactable = GlobalInfo.user.permission == 3;//|| GlobalInfo.user.permission == 2;

        BackBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.PopPanel();
            UIManager.Instance.GetPanel<MainPanel>().OnEnter(null);
        });

        InvokeRepeating("GetConnectStatus", 0, 1.0f);

        DataHandler.getInstance.OnGetConnectStatus += OnGetAllConnectStatusCallback;
    }
    public void OpenParamPanel()
    {
        //if (GlobalInfo.user.permission == 3)
        //    UIManager.Instance.OpenPanel<PLCSettingsPanel>(null);
        //else
        //    UIManager.Instance.OpenPanel<CommonParameterSettingsPanel>("");
        //选的是温度和电极
        var index = MainPanel.CurrentIndex;
        if (index >= 1000)
        {
            index = (index / 1000) * 1000;
        }
        if (index == 1 || index == 1000)
        UIManager.Instance.OpenPanel<CommonParameterSettingsPanel>("", "Popup");
    }

    public void CloseApp()
    {
        Application.Quit();
    }
    public void ConnectConfigParamPanel()
    {
        if (MainPanel.CurrentIndex == -1) { return; }

        if (GlobalInfo.user.permission == 3)
        {
            if (MainPanel.CurrentIndex == 5)
            {
                UIManager.Instance.OpenPanel<OtherPLCSettingsPanel>(null, "Popup");
            }
            else
            {
                UIManager.Instance.OpenPanel<PLCSettingsPanel>(null, "Popup");
            }
        }
        else
        {
            //选的是温度和电极
            var index = MainPanel.CurrentIndex;
            if (index >= 1000)
            {
                index = (index / 1000) * 1000;
            }
            if (index == 1 || index == 1000)
                UIManager.Instance.OpenPanel<CommonParameterSettingsPanel>("", "Popup");
        }
    }
    public void OpenAccountManager()
    {

        UIManager.Instance.OpenPanel<AccountManager>(null, "Popup");
    }

    public void Reload()
    {
        SceneManager.LoadScene(0);
    }

    public void HiddenClose()
    {
        CloseBtn.gameObject.SetActive(false);
    }

    public void ShowClose()
    {
        CloseBtn.gameObject.SetActive(true);
    }

    public void ConnectAll()
    {
        SocketModel soketModel = new SocketModel();
        soketModel.type = Protocols.Protocol.Data;
        soketModel.area = -1;
        soketModel.command = Protocols.DataProtocol.CONNECT_ALL;

        soketModel.senderID = GlobalInfo.user.uid.ToString();
        soketModel.token = GlobalInfo.user.token;

        ClientManager.getInstance.SendServer(soketModel);
    }

    public void GetConnectStatus()
    {
        SocketModel soketModel = new SocketModel();
        soketModel.type = Protocols.Protocol.Data;
        soketModel.area = -1;
        soketModel.command = Protocols.DataProtocol.GET_CONNECT_ALL_STATUS;

        soketModel.senderID = GlobalInfo.user.uid.ToString();
        soketModel.token = GlobalInfo.user.token;

        ClientManager.getInstance.SendServer(soketModel);
    }

    public void OnGetAllConnectStatusCallback(AllConnectStatusDTO dto)
    {
        connectText.text = string.Format("连接 ({0})", dto.connetedCount);
    }

    public void OnDestroy()
    {
        DataHandler.getInstance.OnGetConnectStatus -= OnGetAllConnectStatusCallback;
    }
}
