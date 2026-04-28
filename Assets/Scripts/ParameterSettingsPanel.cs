using Protocols;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ParameterSettingsPanel : UIPanel
{

    public UGuiTable table;

    public Button button;

    public string value = "";

    public override void OnEnter(object param)
    {
        base.OnEnter(param);

        var headers = new UGuiTable.TableHeader[] { new UGuiTable.TableHeader("参数名称", "Input"),
                                                    new UGuiTable.TableHeader("参考值", "Input"),
        };

        table.Col = 2;
        table.SetHeader(headers);

        table.Inital();

        //读取参数
        table.GetItem(1, 0).GetComponent<InputField>().text = "电流参考值";
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
            SetParam();
        });
    }

    private void SetParam()
    {
        SetPlcDataDTO dto = new SetPlcDataDTO();

        dto.plcDatas = new List<PLCData>();
        dto.config = "Electricity";



        var addr = GlobalInfo.m_StaticConfig.staticConfigs["Electricity"].DataBlocks["SetWorkElectricity"];

        string type = DataUtil.PlcToCsharpType(addr);

        dto.plcDatas.Add(new PLCData() { Name = "SetWorkElectricity", Type = type, Value = value });

        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        //model.area = DataProtocol.;
        model.command = DataProtocol.SET_PLC_DATA;
        model.message = SerializeTool.Encode2Str(dto);
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }
}
