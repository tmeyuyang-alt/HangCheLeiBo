using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaringDataListPanel : UIPanel
{
    public Transform Group;
    void Start()
    {
        DataHandler.getInstance.OnGetWarningDataListCallback += GetWarningDataList;

        InvokeRepeating("RequestData", 1, 0.2f);
    }
    public void RequestData()
    {
        SocketModel soketModel = new SocketModel();
        soketModel.type = Protocols.Protocol.Data;
        soketModel.area = -1;
        soketModel.command = Protocols.DataProtocol.Get_WARNINGDAT_LIST;

        soketModel.senderID = GlobalInfo.user.uid.ToString();
        soketModel.token = GlobalInfo.user.token;

        ClientManager.getInstance.SendServer(soketModel);
  
    }
    public void GetWarningDataList(List<WarningData> data)
    {
        int index = 0;
        foreach (WarningData warningData in data)
        {
            Group.GetChild(index).GetComponent<Text>().text = string.Format("¡ñ {0}", warningData.alarm_name);
            Group.GetChild(index).gameObject.SetActive(true);
            index++;
        }
        for (int i = index; i < Group.childCount; i++)
        {
            Group.GetChild(i).gameObject.SetActive(false);
        }
    }
    private void OnDestroy()
    {
        DataHandler.getInstance.OnGetWarningDataListCallback -= GetWarningDataList;
    }
}
