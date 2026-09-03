using System;
using UnityEngine;

public class HangChePosSync : MonoBehaviour
{
    [Header("数据源")]
    public PLCConfigManager plcConfigManager;
    public PLCValueSource valueSource = PLCValueSource.ActiveCrane;
    public int craneNumber = 0;
    public string BigCarPosKey;
    public string SmallCarPosKey;
    public string ZhuaDouPosKey;

    [Header("目标物体")]
    public bool isLeft = true;
    public Transform BigCar;
    public Transform SmallCar;
    public Transform ZhuaDou;

    [Header("平滑参数")]
    [Tooltip("平滑时间，值越大越“黏”，越慢越稳")]
    [Range(0.01f, 2f)] public float smoothTime = 0.15f;

    [Tooltip("最大追随速度（单位：本地坐标单位/秒）")]
    public float maxSpeed = 100f;

    [Tooltip("当距离超过该阈值时直接瞬移，避免从很远的地方慢慢追")]
    public float teleportThreshold = 10f;

    [Tooltip("使用SmoothDamp（更自然）还是Lerp（线性）")]
    public bool useSmoothDamp = true;
    
    

    // SmoothDamp 需要的速度缓存
    private Vector3 _bigVel, _smallVel, _zhuaVel;
    
    public AnimatorScrubber scrubber;

    public string ZhuaDouOpenKey;

    private void Reset()
    {
        // 合理缺省值
        smoothTime = 0.15f;
        maxSpeed = 100f;
        teleportThreshold = 10f;
        useSmoothDamp = true;
        isLeft = true;
    }

    private void OnEnable()
    {
        PLCConfigManager.OnActiveCraneChanged += OnActiveCraneChanged;
        ApplyCraneValueSource();
    }

    private void Start()
    {
        ApplyCraneValueSource();
    }

    private void OnDisable()
    {
        PLCConfigManager.OnActiveCraneChanged -= OnActiveCraneChanged;
    }

    private void OnActiveCraneChanged(int craneIndex)
    {
        ApplyCraneValueSource();
    }

    private void ApplyCraneValueSource()
    {
        if (craneNumber <= 0)
        {
            return;
        }

        if (plcConfigManager == null)
        {
            plcConfigManager = PLCConfigManager.Instance;
        }

        if (plcConfigManager == null)
        {
            return;
        }

        valueSource = plcConfigManager.GetValueSourceForCraneNumber(craneNumber);
    }

    private void Update()
    {
        UpdatePositionInterpolated();
    }

    private void UpdatePositionInterpolated()
    {
        if (plcConfigManager == null) return;
        if (BigCar == null || SmallCar == null || ZhuaDou == null) return;

        // 读取 PLC 目标值（与原脚本方向一致）
        float big = plcConfigManager.GetFloatValue(BigCarPosKey, valueSource)-2.5f;
        float small = plcConfigManager.GetFloatValue(SmallCarPosKey, valueSource);
        float zhua = plcConfigManager.GetFloatValue(ZhuaDouPosKey, valueSource);

        if (big<=0)
        {
            big = 0;
        }

        if (small <= 0)
        {
            small = 0;
        }

        if (zhua <= 0)
        {
            zhua = 0;
        }
        
        

        Vector3 bigTarget   = isLeft ? new Vector3(0, -big, 0) : new Vector3(0, big, 0);
        Vector3 smallTarget = new Vector3(-small, 0, 0);
        Vector3 zhuaTarget  = new Vector3(0, -zhua, 0);

        // 分别对三者做平滑追随
        BigCar.localPosition   = SmoothFollow(BigCar.localPosition,   bigTarget,   ref _bigVel);
        SmallCar.localPosition = SmoothFollow(SmallCar.localPosition, smallTarget, ref _smallVel);
        ZhuaDou.localPosition  = SmoothFollow(ZhuaDou.localPosition,  zhuaTarget,  ref _zhuaVel);
        
        scrubber.SetProgress(plcConfigManager.GetFloatValue(ZhuaDouOpenKey, valueSource));
        
    }

    private Vector3 SmoothFollow(Vector3 current, Vector3 target, ref Vector3 velocity)
    {
        // 跳变过大时直接瞬移，避免拖尾
        if ((target - current).sqrMagnitude > teleportThreshold * teleportThreshold)
        {
            velocity = Vector3.zero;
            return target;
        }

        if (useSmoothDamp)
        {
            // SmoothDamp：趋近自然、无过冲
            return Vector3.SmoothDamp(current, target, ref velocity, smoothTime, maxSpeed, Time.deltaTime);
        }
        else
        {
            // Lerp：线性插值（t 应当与帧率无关）
            float t = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.0001f, smoothTime));
            Vector3 next = Vector3.Lerp(current, target, t);

            // 可选：限制每帧最大位移，近似最大速度
            float maxStep = maxSpeed * Time.deltaTime;
            Vector3 delta = next - current;
            if (delta.magnitude > maxStep)
                next = current + delta.normalized * maxStep;

            return next;
        }
    }
}
