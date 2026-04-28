using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTransparent : ImageEffectBase
{
    /// <summary>
    /// 效果层
    /// </summary>
    public LayerMask effectLayer;
    public Resolution resolution;
    public RenderTexture rendertexture;
    public Color backgroundColor;
    public Texture backgroundTexture;

    public System.Action AimEnd = null;
    public System.Action AimStart = null;
    [Range(0, 1)]
    public float Alpha = 1;

    private float _TargetAlpha = 1;

    private void Awake()
    {

        if (transform.Find("childCamGo") == null)
        {
            Camera camera = GetComponent<Camera>();
            GameObject childCamGo = new GameObject("childCamGo");
            Camera childCam = childCamGo.AddComponent<Camera>();
            childCam.fieldOfView = camera.fieldOfView;
            childCam.nearClipPlane = camera.nearClipPlane;
            childCam.farClipPlane = camera.farClipPlane;
            childCam.transform.SetParent(transform,false);
            childCam.cullingMask = effectLayer;
            childCam.clearFlags = CameraClearFlags.Color;
            childCam.backgroundColor = new Color(0, 0, 0, 0);
            rendertexture = CreateRT();
            childCam.targetTexture = rendertexture;
        }
    }
    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if(rendertexture!=null)
        this.mMaterial.SetTexture("_Source", rendertexture);
        this.mMaterial.SetFloat("_Alpha", Alpha);
        this.mMaterial.SetColor("_backgroundColor", backgroundColor);
        if(backgroundTexture != null)
        this.mMaterial.SetTexture("_backgroundTexture", backgroundTexture);
        base.OnRenderImage(source, destination);
    }

    private RenderTexture CreateRT()
    {
        int res = (int)this.resolution;

        RenderTexture rt = new RenderTexture(Screen.width/ res,Screen.height/res,16,RenderTextureFormat.ARGB32);

        return rt;
    }
    private bool startPlay = false;
    public void SetValue(float v)
    {
        AimStart?.Invoke();
        startPlay = true;
        _TargetAlpha = v;
    }
    private void Update()
    {
        if(startPlay)
        Alpha = Mathf.Lerp(Alpha, _TargetAlpha,Time.deltaTime*3);

        if (startPlay)
        {
            if (Alpha - _TargetAlpha < 0.01f)
            {     
                Alpha = _TargetAlpha;
                startPlay = false;
                AimEnd?.Invoke();
            }
        }
    }
    private void OnDestroy()
    {
        rendertexture.Release();
        rendertexture = null;
    }
}
public enum Resolution
{
    Full =1,
    Half =2,
    Quarter=4
}
