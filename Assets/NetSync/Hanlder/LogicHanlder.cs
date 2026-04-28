using Protocols;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicHanlder {

    private static LogicHanlder instance = null;

    public static LogicHanlder getInstance
    {
        get
        {
            if (instance == null)
                instance = new LogicHanlder();
            return instance;
        }
    }
    /// <summary>
    /// 解析数据
    /// </summary>
    /// <param name="json"></param>
    public void Analysis(SocketModel model)
    {
        PutMessage(model);
    }

    private void PutMessage(SocketModel model)
    {
        switch (model.type)
        {
            case 0:
          
                break;
            case Protocol.Login:
                LoginHandler.getInstance.Execute(model);
                break;
            case Protocol.Data:
                DataHandler.getInstance.Execute(model);
                break;
            case Protocol.Message:
                var packin = LitJson.JsonMapper.ToObject<PackIn>(model.message);
                MessageOutput(packin.code);
                break;
        }
    }

    public void MessageOutput(int code)
    {
        if (code == CodeConstant.NOT_LOGGED_IN)
        {
            //MessageBoxPanel.Show("提示", "未登录！", null);
        }
    }
}
