using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public float speedAngle = 10;
    public float selfSpeedAngle = 10;

    private Camera m_camera;
    private Transform Target;

    public bool SingleClick = false;

    private System.DateTime LastClickTime;

    public bool LockTranslate = false;

    public float CameraDistance = 0;

    public float Angle = 0;

    public float CameraHeight = 0;

    public LimitTranslateMode limitTranslateMode = LimitTranslateMode.LimitCenter;

    private CameraParam initalCamreaParam;

    /*
     *当前状态信息
     */
    private float CurrentCameraDistance = 0;
    private float CurrentHeight = 0;
    private Vector3 CurrrentCenter;


    private Vector2 LimitDistance = new Vector2(13, 82);
    private float initalCameraDistance = 0;
    private float initalCameraHeighte = 0;

    /// <summary>
    /// 平移速度
    /// </summary>
    public float translationSpeed = 0.1f;

    public Vector3 SelectionOffset = Vector3.zero;
    //目标中心点
    public Vector3 CenterOffset;
    /// <summary>
    /// 锁定缩放
    /// </summary>
    public bool lockDistance = true;
    /// <summary>
    /// 平移范围限制
    /// </summary>
    public Vector4 TranslateRange = new Vector4(100, 100, 0, 0);
    /// <summary>
    /// 点击事件
    /// </summary>
    public System.Action<GameObject> OnClicked;
    /// <summary>
    /// 
    /// </summary>
    public System.Func<GameObject> OnSelected;


    public bool LockPitch = true;

    private int lastHashCode = 0;

    private Vector3 LastMousePos;

    /// <summary>
    /// 仰角
    /// </summary>
    public float Pitch = 0;
    private float CurrentPitch = 0;


    void Start()
    {
        if (CameraDistance <= 0)
            CameraDistance = Vector3.Distance(transform.position, CurrrentCenter);

        CurrentCameraDistance = CameraDistance;

        initalCameraDistance = CameraDistance;

        //Angle = Vector3.Angle(Vector3.right, (transform.position - CurrrentCenter).normalized);

        //CameraHeight = transform.position.y;

        initalCameraHeighte = CameraHeight;

        CurrentHeight = CameraHeight;

        m_camera = Camera.main.GetComponent<Camera>();


        //保存初始化相机控制参数
        initalCamreaParam.CenterOffset = CenterOffset;
        initalCamreaParam.Angle = Angle;
        initalCamreaParam.CameraDistance = CameraDistance;
        initalCamreaParam.CameraHeight = CameraHeight;
        initalCamreaParam.selfSpeedAngle = selfSpeedAngle;
        initalCamreaParam.speedAngle = speedAngle;

    }
    public bool clickInUI=false;

    public bool IgnoreUI = true;

    void LateUpdate()
    {
        //float maxXPos = TranslateRange.x * 0.5f + TranslateRange.z;
        //float minXPos = -TranslateRange.x * 0.5f + TranslateRange.z;
        //float maxYPos = TranslateRange.y * 0.5f + TranslateRange.w;
        //float minYPos = -TranslateRange.y * 0.5f + TranslateRange.w;


        
        if (Input.GetMouseButtonDown(0))
        {
            LastMousePos = Input.mousePosition;

            //鼠标是否在UI上
            if (EventSystem.current.IsPointerOverGameObject())
            {
                clickInUI = true;
                return;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            clickInUI = false;
        }
        
        if (clickInUI&& !IgnoreUI)
            return;

        if (Target != null)
        {
            CurrrentCenter = Vector3.Lerp(CurrrentCenter, Target.position + SelectionOffset, Time.deltaTime * 2);
        }
        else
        {
            CurrrentCenter = Vector3.Lerp(CurrrentCenter, CenterOffset, Time.deltaTime * 2);
        }

        float radian = 0;
        //旋转功能
        if (Input.GetMouseButton(0))
        {
            Vector3 move = Input.mousePosition - LastMousePos;

            Angle -= speedAngle * move.x * 0.05f; //Time.deltaTime * 

            radian = Angle * Mathf.Deg2Rad;

            //上下旋转
            if(!LockPitch)
            Pitch -= move.y * Time.deltaTime * 2;
        }
        else
        {
            Angle += Time.deltaTime * selfSpeedAngle;
            radian = Angle * Mathf.Deg2Rad;
        }

        //平移功能
        if (Input.GetMouseButton(1)&& !LockTranslate)
        {
            ProccessTranslate();
        }

        if (limitTranslateMode == LimitTranslateMode.LimitCamera)
        {
            LimitTranslate();
        }
        else
        {

            float maxXPos = TranslateRange.x * 0.5f + TranslateRange.z;
            float minXPos = -TranslateRange.x * 0.5f + TranslateRange.z;
            float maxYPos = TranslateRange.y * 0.5f + TranslateRange.w;
            float minYPos = -TranslateRange.y * 0.5f + TranslateRange.w;

            CenterOffset.x = Mathf.Max(Mathf.Min(CenterOffset.x, maxXPos), minXPos);
            CenterOffset.z = Mathf.Max(Mathf.Min(CenterOffset.z, maxYPos), minYPos);
        }

        //相机运动部分

        LastMousePos = Input.mousePosition;

        Vector3 backforward = (transform.position - CurrrentCenter).normalized;

        Vector3 targetPos = transform.position;

        //当前相机距离
        CurrentCameraDistance = Mathf.Lerp(CurrentCameraDistance, CameraDistance, Time.deltaTime * 2);

        CameraDistance = Mathf.Max( Mathf.Min(CameraDistance, LimitDistance.y),LimitDistance.x);
        CurrentCameraDistance = Mathf.Max( Mathf.Min(CurrentCameraDistance, LimitDistance.y),LimitDistance.x);

        //当前高度
        CurrentHeight = Mathf.Lerp(CurrentHeight, CameraHeight, Time.deltaTime * 2);

        targetPos = new Vector3(Mathf.Cos(radian) * CurrentCameraDistance, CurrentHeight,
                          Mathf.Sin(radian) * CurrentCameraDistance) + CurrrentCenter;

        //限制位移
        //pos.x = Mathf.Max(Mathf.Min(pos.x, maxXPos), minXPos);
        //pos.z = Mathf.Max(Mathf.Min(pos.z, maxYPos), minYPos);

        //设置相机位置
        transform.position = targetPos;

        //死死锁定 LookAt
        transform.LookAt(CurrrentCenter);


        //滚轮控制远近
        if (Input.GetAxis("Mouse ScrollWheel") != 0 && !lockDistance)
        {
            if(!EventSystem.current.IsPointerOverGameObject())
            CameraDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * 2000;
        }



        var eulerAngles = transform.eulerAngles;
        eulerAngles.x += CurrentPitch;
        transform.eulerAngles = eulerAngles;

        CurrentPitch = Mathf.Lerp(Pitch, CurrentPitch, Time.deltaTime * 5);
    }

    private void Update()
    {
        //点击功能
        if (Input.GetMouseButtonDown(0))
        {
            ProccessClickObj();
        }
    }

    private void ProccessClickObj()
    {
        double clickTime = (System.DateTime.Now - LastClickTime).TotalSeconds;

        if (clickTime > 0.6f)
            lastHashCode = 0; //表示第一次点击

        Ray ray = m_camera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            bool equal = (lastHashCode == hit.collider.gameObject.GetHashCode());

            if (clickTime < 0.6f && equal && lastHashCode!=0)
            {
                lastHashCode = 0;

                OnClicked?.Invoke(hit.collider.gameObject);

            }
            else if (equal == false && lastHashCode!=0)
            {
                lastHashCode = 0;
            }
            else
            {
                lastHashCode = hit.collider.gameObject.GetHashCode();
            }

            LastClickTime = System.DateTime.Now;

        }


    }

    private void ProccessTranslate()
    {
        Vector3 move = (Input.mousePosition - LastMousePos) * translationSpeed;

        Vector3 right = transform.right;
        right.y = 0;

        Vector3 forward = transform.forward;
        forward.y = 0;

        CenterOffset -= right * move.x + forward * move.y;
    }

    private void LimitTranslate()
    {

        float maxXPos = TranslateRange.x * 0.5f + TranslateRange.z;
        float minXPos = -TranslateRange.x * 0.5f + TranslateRange.z;
        float maxYPos = TranslateRange.y * 0.5f + TranslateRange.w;
        float minYPos = -TranslateRange.y * 0.5f + TranslateRange.w;

        //计算偏移后的位置
        Vector3 targetPos = new Vector3(Mathf.Cos(Angle * Mathf.Deg2Rad) * CameraDistance, CameraHeight,
               Mathf.Sin(Angle * Mathf.Deg2Rad) * CameraDistance) + CenterOffset;

        if (targetPos.x > maxXPos)
        {
            CenterOffset.x -= (targetPos.x - maxXPos);
        }
        else if (targetPos.x < minXPos)
        {
            CenterOffset.x += (minXPos - targetPos.x);
        }

        if (targetPos.z > maxYPos)
        {
            CenterOffset.z -= (targetPos.z - maxYPos);
        }
        else if (targetPos.z < minYPos)
        {
            CenterOffset.z += (minYPos - targetPos.z);
        }
    }

    private void LookAt(Transform m_selft, Vector3 _target, float dt)
    {
        Quaternion target_rotate = Quaternion.LookRotation(_target - m_selft.position, Vector3.up);

        m_selft.rotation = Quaternion.Lerp(m_selft.rotation, target_rotate, dt);
    }

    public void SetAllChildLayer(Transform root, int layer)
    {
        if (root == null)
            return;
        int count = root.childCount;
        root.gameObject.layer = layer;
        for (int i = 0; i < count; i++)
        {
            root.GetChild(i).gameObject.layer = layer;
        }
    }

    /// <summary>
    /// 包含设置子层级
    /// </summary>
    /// <param name="root"></param>
    /// <param name="layer"></param>
    public void SetLayer(Transform root, int layer, int filterLayer = -1)
    {
        int count = root.childCount;
        if (root.gameObject.layer != filterLayer)
            root.gameObject.layer = layer;
        for (int i = 0; i < count; i++)
        {
            SetLayer(root.GetChild(i), layer, filterLayer);
        }
    }
    private void MoveTo(Vector3 pos, float dt)
    {
        transform.position = Vector3.Lerp(transform.position, pos, dt);
    }

    private void SetTarget(Transform target)
    {
        this.Target = target;
    }

    /// <summary>
    /// 重置相机参数
    /// </summary>
    public void ResetCameraParam()
    {
        CameraHeight = initalCamreaParam.CameraHeight;
        CameraDistance = initalCamreaParam.CameraDistance;
        //Angle = initalCamreaParam.Angle;
        CenterOffset = initalCamreaParam.CenterOffset;
        selfSpeedAngle = initalCamreaParam.selfSpeedAngle;
        speedAngle = initalCamreaParam.speedAngle;
        Pitch = 0;
    }


    private void OnDrawGizmos()
    {
        Vector3 boxPos = new Vector3(TranslateRange.z, 0, TranslateRange.w);
        Vector3 boxSize = new Vector3(TranslateRange.x, 1, TranslateRange.y);
        Gizmos.DrawWireCube(boxPos, boxSize);

        Gizmos.DrawWireCube(CenterOffset, Vector3.one*0.1f);
    }

    public enum LimitTranslateMode
    { 
        LimitCamera,
        LimitCenter,
    }
}

public struct CameraParam
{
    public Vector3 CenterOffset;
    public float speedAngle;
    public float selfSpeedAngle;
    public float CameraDistance;
    public float Angle;
    public float CameraHeight;
}

