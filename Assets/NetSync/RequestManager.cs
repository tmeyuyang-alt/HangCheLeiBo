using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Protocols;
public class RequestManager : MonoBehaviour
{
    private static RequestManager instance = null;

    public static RequestManager getInstance
    {
        get
        {
            if (instance == null)
                instance = new RequestManager();
            return instance;
        }
    }

    /// <summary>
    /// 请求公钥
    /// </summary>
    /// <param name="onResponse"></param>
    public static void RequestPublicKey(System.Action<string> onResponse)
    {
        ClientManager.getInstance.SendServer(Protocol.Login, LoginProtocol.LOGIN, LoginCommandProtocol.REQ_PK, "");

        LoginHandler.getInstance.OnReceiveKey = onResponse;
    }
    /// <summary>
    /// 登录请求
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="callback"></param>
    public static void Login(LoginDTO dto, System.Action<PackIn> callback)
    {
        string message = LitJson.JsonMapper.ToJson(dto);
        ClientManager.getInstance.SendServer(Protocol.Login, LoginProtocol.LOGIN, LoginCommandProtocol.LOGIN, message);
        LoginHandler.getInstance.LoginStateCallback = callback;
    }
    /// <summary>
    /// 重置密码
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="callback"></param>
    public static void RestPassword(RestPasswordDTO dto, System.Action<PackIn> callback)
    {
        string message = LitJson.JsonMapper.ToJson(dto);
        ClientManager.getInstance.SendServer(Protocol.Login, LoginProtocol.LOGIN, LoginCommandProtocol.REST_PSW, message);
        LoginHandler.getInstance.RestPswStateCallback = callback;
    }
}
