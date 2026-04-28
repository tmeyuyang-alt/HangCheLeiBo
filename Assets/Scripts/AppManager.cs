using Protocols;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AppManager : MonoBehaviour
{
    public Transform MainCamera;
    public Transform RoomCamera;

    private static AppManager m_Instance;
    public static AppManager Instance { get { return m_Instance; } }

    public CameraController controller;
    private int currentLayer = -1;

    private bool enableAnim = false;
    private float enableAnimTimer = 0;
    private float time = 10;
    private float animTimeline = 0;

    private float angleRange = 80f;
    public float idleRotateSpeed = 0.2f;

    public GameObject electrodePanels;
    public void Awake()
    {
        m_Instance = this;
    }

    private void Start()
    {
        DataHandler.getInstance.OnGetOtherPlcConfigCallback += OnGetOtherPlcConfig;
    }
    public void OnGetOtherPlcConfig(StaticConfig config)
    {
        GlobalInfo.m_StaticConfig = config;
    }
    /// <summary>
    /// 请求配置
    /// </summary>
    public void RequestOtherPlcConfig()
    {
        SocketModel model = new SocketModel();

        model.type = Protocol.Data;
        model.command = DataProtocol.GET_OTHER_PLC_CONFIG;
        model.message = null;
        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;
        ClientManager.getInstance.SendServer(model);
    }
    public void SelectedLayer(int layer)
    {
        currentLayer = layer;
    }

    private void Update()
    {
        if (Time.frameCount % 3000 == 0)
            System.GC.Collect();

        return;

        if (currentLayer == 2)
        {
            if (!enableAnim)
            {
                enableAnimTimer += Time.deltaTime;
                if (time < enableAnimTimer)
                {
                    enableAnim = true;

                    float tmp = (controller.Angle - 90) / angleRange;

                    tmp = Mathf.Clamp(tmp, -1, 1);

                    animTimeline = Mathf.Asin(tmp);

                }
            }
        }

        if (Input.anyKey || Input.mouseScrollDelta != new Vector2(0, 0))
        {
            enableAnim = false;
            enableAnimTimer = 0;
        }

        if (enableAnim)
        {
            controller.Angle = 90 + Mathf.Sin(animTimeline) * angleRange;
            animTimeline += Time.deltaTime * idleRotateSpeed ;
            animTimeline = animTimeline % (2 * Mathf.PI);
        }
    }

    public void EnterRoom()
    {
        RoomCamera.GetComponent<RoomCamrea>().RestParam();
        MainPanel.electrodePanels.SetActive(false);
        RoomCamera.gameObject.SetActive(true);
        MainCamera.gameObject.SetActive(false);
    }

    public void ExitRoom()
    {
        MainPanel.electrodePanels.SetActive(false);
        MainPanel.electrodePanels.SetActive(true);
        RoomCamera.gameObject.SetActive(false);
        MainCamera.gameObject.SetActive(true);
    }

    public void OnDestroy()
    {
        m_Instance = null;

        DataHandler.getInstance.OnGetOtherPlcConfigCallback -= OnGetOtherPlcConfig;
    }

}
