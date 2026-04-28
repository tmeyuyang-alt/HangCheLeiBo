using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using GameServer;
using System.Threading;
/*


处理与服务器的连接以及对数据的解析



*/

public class ClientManager : MonoBehaviour
{
    public Socket socketClient = null;

    public static ClientManager getInstance;

    //回调函数

    public System.Action<string> OnMessageCallback;

    public System.Action<bool> OnLoadCallback = null;

    public System.Action<string> timeoutCallback = null;

    private System.Action<bool> connectCallback = null;

    private int currentLoadType = -1;

    private Thread connectThread;

    /// <summary>
    /// 获取当前在Loading的信息类型
    /// </summary>
    public int getCurrentLoadType { get { return currentLoadType; } }

    public string serverIP = "10.0.1.111";

    public int serverPort = 50551;

    public bool onAwakeConnect = false;

    private EndPoint romte = null;

    private EndPoint localEndPoint = null;

    private ConcurrentQueue<string> message = new ConcurrentQueue<string>();

    private byte[] buffer = new byte[1024];

    private List<byte> messageBuffer = new List<byte>();

    private static int intSize = sizeof(int);

    private bool enableConnect = false;

    private float timer = 0;

    private float reconnectTime = 5; //断线重连事件间隔


    public bool IsOnReloadDestory= false;


    void Awake()
    {
        getInstance = this;

        //判断是否存在文件
        string serverConfig = Application.streamingAssetsPath + "/server_config.txt";
        if (System.IO.File.Exists(serverConfig))
        {
            string serverText = System.IO.File.ReadAllText(serverConfig);
            string[] ipport = serverText.Split(':');
            if (ipport.Length > 1)
            {
                this.serverIP = ipport[0];
                this.serverPort = int.Parse(ipport[1]);
            }
        }

        //TODO 初始化Socket
        socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        localEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
        socketClient.Bind(localEndPoint);
        romte = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
        if (onAwakeConnect) StartConnect();

        //Debug.Log("登录"+ System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress())

        if (!IsOnReloadDestory)
            DontDestroyOnLoad(gameObject);
    }

    private int maxExecuteNumer = 30;

    private int currentExecuteNumber = 0;

    private void Update()
    {
        while (message.Count > 0)
        {
            string data = "";

            if (message.TryDequeue(out data))
            {
                if (OnMessageCallback != null)
                {
                    //OnMessageCallback(data);

                    //TODO:调用
                    Delegate[] array = OnMessageCallback.GetInvocationList();
                    foreach (Action<string> dl in array)
                    {
                        try
                        {
                            dl(data);
                        }
                        catch { }
                    }
                }

                //TODO解析成SocketModel

                SocketModel model = LitJson.JsonMapper.ToObject<SocketModel>(data);

                //TODO解析数据
                LogicHanlder.getInstance.Analysis(model);

                //判断是否要关闭Loading
                if (currentLoadType != -1 && currentLoadType == model.type)
                {
                    if (OnLoadCallback != null)
                        OnLoadCallback(false);
                    currentLoadType = -1;
                }
            }

            currentExecuteNumber++;

            if (currentExecuteNumber >= maxExecuteNumer)
            {
                currentExecuteNumber = 0;
                break;
            }
        }
        //TODO:更新定时系统
        TimerManager.Instance.UpdateTime();

        if (enableConnect)
        {
            timer += Time.deltaTime;

            if (timer >= reconnectTime)
            {
                if (socketClient != null)
                    if (!socketClient.Connected)
                    {
                        timer = 0.0f;
                        try
                        {
                            ConnectServer();
                        }
                        catch
                        {
                            Debug.LogWarning("重连失败！");
                        }
                    }
            }
        }
    }

    /// <summary>
    /// 数据接收
    /// </summary>
    /// <param name="ar"></param>
    private void OnReciveCmd(IAsyncResult ar)
    {
        if (!enableConnect) return;
        Socket socket = ar.AsyncState as Socket;
        try
        {
            int count = 0;

            if (socket == null)
                return;
            count = socket.EndReceive(ar);
            
            ar.AsyncWaitHandle.Close();

            //读取数据
            for (int i = 0; i < count; i++)
            {
                messageBuffer.Add(buffer[i]);
            }

            //拆分出数据
            while (messageBuffer.Count >= intSize)
            {
                byte[] lengthBytes = messageBuffer.GetRange(0, intSize).ToArray();

                int len = (int)BitConverter.ToUInt32(lengthBytes, 0);

                if (len <= messageBuffer.Count - intSize)
                {
                    byte[] msg_arr = new byte[len];
                    //拷贝成数组
                    for (int i = 0; i < len; i++) { msg_arr[i] = messageBuffer[i + intSize]; }
                    //Array.Copy(messageBuffer.ToArray(), intSize, msg_arr, 0, len);
                    //数据转成Json
                    string json = System.Text.Encoding.UTF8.GetString(msg_arr, 0, len);

                    message.Enqueue(json);
                    //删除uint_size + len 长度
                    messageBuffer.RemoveRange(0, intSize + len);

                }
                else
                {
                    break;
                }
            }
        }
        catch (SocketException ex)
        {
            Debug.Log(ex.Message);
            return;
        }
        if (socket != null)
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReciveCmd, socketClient);
    }
    /// <summary>
    /// 发送Json
    /// </summary>
    /// <param name="json"></param>
    public void SendJson(string json)
    {
        if (!enableConnect)
        {
            Debug.Log("无法发送数据！ 连接已关闭！");
            return;
        }
        try
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(json);

            List<byte> bufferList = new List<byte>();

            //加入长度
            bufferList.AddRange(BitConverter.GetBytes(buffer.Length));
            //加入数据体
            bufferList.AddRange(buffer);

            if (socketClient != null && socketClient.Connected)
            {
                socketClient.Send(bufferList.ToArray());
            }
            else
            {
                //Debug.LogWarning("未连接！服务器 重新连接...");
                if (socketClient != null)
                {
                    socketClient.Close();
                    socketClient = null;
                }
                ConnectServer();
            }
        }
        catch (SocketException ex)
        {
            Debug.LogWarning("发送失败！" + ex.Message);
        }
    }
    /// <summary>
    /// 发送消息到服务器
    /// </summary>
    /// <param name="type"></param>
    /// <param name="area"></param>
    /// <param name="command"></param>
    /// <param name="message"></param>
    public void SendServer(int type, int area, int command, string message)
    {
        SocketModel model = new SocketModel();

        //model.senderID = GlobalInfo.uid;

        model.type = type;

        model.area = area;

        model.command = command;

        ////判断是否调用Loading回调
        //if (model.type != RoomProtocol.Operation && model.type != 0)
        //{
        //    if (OnLoadCallback != null)
        //    {
        //        TimerManager.Instance.AddTimeEvent((int)(resqustOuttime * 10000000), LoadFailed);
        //        OnLoadCallback(true);
        //    }
        //    //赋值当前loading的类型
        //    currentLoadType = model.type;
        //}
        model.message = message;

        SendJson(LitJson.JsonMapper.ToJson(model));
    }
    /// <summary>
    /// 发送消息到服务器
    /// </summary>
    /// <param name="model"></param>
    public void SendServer(SocketModel model)
    {
        //model.senderID = GlobalInfo.uid;
        ////判断是否调用Loading回调
        //if (model.area != RoomProtocol.Operation && model.type != 0)
        //{
        //    if (OnLoadCallback != null)
        //    {
        //        TimerManager.Instance.AddTimeEvent((int)(resqustOuttime * 10000000), LoadFailed);
        //        OnLoadCallback(true);
        //    }
        //    //赋值当前loading的类型
        //    currentLoadType = model.type;
        //}

        SendJson(LitJson.JsonMapper.ToJson(model));
    }

    /// <summary>
    /// 请求失败
    /// </summary>
    /// <param name="param"></param>
    private void LoadFailed(object param)
    {
        if (currentLoadType != -1)
        {
            Debug.Log("请求失败 错误 type" + currentLoadType);

            currentLoadType = -1;
            OnLoadCallback(false);
            if (timeoutCallback != null)
            {
                timeoutCallback("请求超时");
            }
        }
    }
    /// <summary>
    /// 连接服务器
    /// </summary>
    private void ConnectServer()
    {

        enableConnect = true;
        if (socketClient == null)
        {
            socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socketClient.Bind(new IPEndPoint(IPAddress.Any, 0));
        }

        if (socketClient != null)
        {
            if (!socketClient.Connected)
            {
                try
                {
                    socketClient.BeginConnect(romte, AsyncConnectCallbak, socketClient);
                }
                catch { }
            }
            else
            {
                DisConnectServer(false);
                try
                {
                    socketClient.BeginConnect(romte, AsyncConnectCallbak, socketClient);
                }
                catch { }
            }
        }
        else
        {
            if (connectCallback != null)
                connectCallback(false);

            throw new Exception("连服务器器失败 Socket为空");
        }

    }
    /// <summary>
    /// 啓用綫程鏈接
    /// </summary>
    private void ConnectThread()
    {
        if (connectThread != null)
        {
            connectThread.Abort();
            connectThread = null;
        }
        if (connectThread == null)
            connectThread = new Thread(ConnectServer);
        connectThread.IsBackground = true;
        connectThread.Start();
    }
    /// <summary>
    /// 异步连接回调
    /// </summary>
    /// <param name="ar"></param>
    private void AsyncConnectCallbak(IAsyncResult ar)
    {
        if (enableConnect == false)
        {
            connectCallback(false);
            return;
        }
        Socket socket = ar.AsyncState as Socket;

        if (socket != null)
            if (socket.Connected)
            {
                try
                {
                    socket.EndConnect(ar);
                }
                catch
                {
                    return;
                }
                Debug.Log(string.Format("服务器连接成功! {0}", socket.RemoteEndPoint.ToString()));
                if (connectCallback != null)
                    connectCallback(true);
                ////TODO:发送Account
                //SendServer(Protocol.Login, LoginProtocol.LOGIN, 0, LitJson.JsonMapper.ToJson(account));

                if (socket != null)
                {
                    socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReciveCmd, socketClient);
                }

            }
    }

    /// <summary>
    /// 端口服务器连接
    /// </summary>
    /// <param name="reuse"></param>
    private void DisConnectServer(bool reuse)
    {
        enableConnect = false;

        if (socketClient == null) return;
        try
        {
            socketClient.Shutdown(SocketShutdown.Both);
            socketClient.Dispose();
            socketClient.Close();

        }
        catch { }

        socketClient = null;
    }

    /// <summary>
    /// 设置远端地址并且连接
    /// </summary>
    /// <param name="re"></param>
    public void SetRomteAndConnect(IPEndPoint re)
    {
        romte = re;

        ConnectServer();
    }

    /// <summary>
    /// 判断是否正常连接
    /// </summary>
    /// <returns></returns>
    public bool IsSocketConnected()
    {
        return !((socketClient.Poll(1000, SelectMode.SelectRead) && (socketClient.Available == 0)) || !socketClient.Connected);

    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="accID"></param>
    public void StartConnect()
    {
        ConnectThread();
    }
    /// <summary>
    /// 连接服务器
    /// </summary>
    public void StartConnect(System.Action<bool> callback)
    {
        ConnectThread();
        this.connectCallback = callback;
    }
    /// <summary>
    /// 关闭连接
    /// </summary>
    public void CloseConnect()
    {
        DisConnectServer(false);
        enableConnect = false;
        Debug.Log("关闭连接");
    }
    /// <summary>
    /// 重新连接
    /// </summary>
    public void ReConnect()
    {
        ConnectServer();
    }

    public void OnDestroy()
    {
        DisConnectServer(false);
    }
}