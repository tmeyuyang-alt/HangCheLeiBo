using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
public class LoginHandler
{
    private static LoginHandler _Instance = null;

    public static LoginHandler getInstance
    {
        get
        {
            if (_Instance == null)
                _Instance = new LoginHandler();
            return _Instance;
        }
    }

    /// <summary>
    /// 当获取到公钥时调用
    /// </summary>
    public System.Action<string> OnReceiveKey;
    /// <summary>
    /// 登录状态回调
    /// </summary>
    public System.Action<PackIn> LoginStateCallback;
    /// <summary>
    /// 重置密码状态回调
    /// </summary>
    public System.Action<PackIn> RestPswStateCallback;

    public System.Action<PackIn> OnAccountOperateCallback;

    public void Execute(SocketModel model)
    { 
        switch(model.command)
        {
            case Protocols.LoginCommandProtocol.REQ_PK:
                var info = JsonMapper.ToObject<PackIn>(model.message);
                //GlobalInfo.publicKey = info.message;
                OnReceiveKey?.Invoke(info.message);
                break;
            case Protocols.LoginCommandProtocol.LOGIN:
                LoginStateCallback?.Invoke(JsonMapper.ToObject<PackIn>(model.message));
                break;
            case Protocols.LoginCommandProtocol.REST_PSW:
                RestPswStateCallback?.Invoke(JsonMapper.ToObject<PackIn>(model.message));
                break;
            case Protocols.LoginCommandProtocol.CREATE_ACCOUNT:
            case Protocols.LoginCommandProtocol.MODIFIY_ACCOUNT:
                OnAccountOperateCallback?.Invoke(JsonMapper.ToObject<PackIn>(model.message));
                break;
        }
    }
}
