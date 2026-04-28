using Protocols;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GearsParameterSettingsPanel : UIPanel
{
    public UGuiTable table;

    public Button button;

    public string value = "";

    public override void OnEnter(object param)
    {
        base.OnEnter(param);

        var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                    new UGuiTable.TableHeader("值", "Input"),
        };

        table.Col = 2;
        table.SetHeader(headers);

        table.Inital();

        //读取参数
        table.GetItem(1, 0).GetComponent<InputField>().text = "挡位";
        table.GetItem(1, 1).GetComponent<InputField>().text = param.ToString();
        
        value = param.ToString();

        table.OnTextChanged += (x, y, str) =>
        {
            value = str;
        };
    }
    void Start()
    {


        button.onClick.AddListener(() =>
        {
            int iValue = int.Parse(value);
            SetGears(iValue);
            this.OnClose();
        });
    }

    public void SetGears(int intValue)
    {
        SetPlcDataDTO dto = new SetPlcDataDTO();

        dto.plcDatas = new List<PLCData>();

        dto.config = "Gears";

        if (GlobalInfo.m_StaticConfig != null && GlobalInfo.m_StaticConfig.staticConfigs.ContainsKey("Gears"))
        {
            var gearsAddr = GlobalInfo.m_StaticConfig.staticConfigs["Gears"].DataBlocks["Gears"];

            var tempAddr = gearsAddr.Split('&');
            
            if (tempAddr.Length > 1)
                gearsAddr = tempAddr[0];

            if (DataUtil.GetBitNumber(gearsAddr) == 16)
            {
                dto.plcDatas.Add(new PLCData() { Name = "Gears", Type = "Int16", Value = intValue.ToString() });
            }
            else
            {
                dto.plcDatas.Add(new PLCData() { Name = "Gears", Type = "int", Value = intValue.ToString() });
            }
        }
        else
        {
            dto.plcDatas.Add(new PLCData() { Name = "Gears", Type = "int", Value = intValue.ToString() });
        }
        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.SET_PLC_DATA;
        model.message = SerializeTool.Encode2Str(dto);
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }

}
