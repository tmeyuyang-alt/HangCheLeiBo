using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
[ExecuteInEditMode]
public class OutLineEffect:ImageEffectBase
{
    /// <summary>
    /// 效果层
    /// </summary>
    public LayerMask effectLayer;
    public Resolution resolution = Resolution.Full;
    public RenderTexture rendertexture;
    public Color OutlineColor;
    [Range(0,20)]
    public float Intensity =1;
    [Range(1, 5)]
    public float PixelWidth = 3;

    private Camera outlineCam;
    private CommandBuffer cmdbuffer = null;

    public bool Internal = false;

    private Dictionary<int,Renderer[]> meshRenderers;
    private Dictionary<int,Material> materials;

    private void Awake()
    {
        if (meshRenderers == null) meshRenderers = new Dictionary<int, Renderer[]>();
        if (materials == null) materials = new Dictionary<int, Material>();

        //if(colorMat==null) colorMat = new Material(Shader.Find("Unlit/Color"));
        if (mMaterial == null)
        {
            if (mShader == null)
                mShader = Shader.Find("Hidden/OutLineImageEffect");

            mMaterial = new Material(mShader);
        }
        Transform OutLineCamTF = transform.Find("OutLineCamGo");
        if (OutLineCamTF == null)
        {
            Camera camera = GetComponent<Camera>();
            GameObject childCamGo = new GameObject("OutLineCamGo");
            Camera childCam = childCamGo.AddComponent<Camera>();
            childCam.fieldOfView = camera.fieldOfView;
            childCam.nearClipPlane = camera.nearClipPlane;
            childCam.farClipPlane = camera.farClipPlane;
            childCam.transform.SetParent(transform, false);
            childCam.cullingMask = effectLayer;
            childCam.clearFlags = CameraClearFlags.Color;
            childCam.backgroundColor = new Color(0, 0, 0, 0);
            rendertexture = CreateRT(Screen.width, Screen.height);
            childCam.targetTexture = rendertexture;

            outlineCam = camera;
        }
        else
        {
            Camera cam = OutLineCamTF.GetComponent<Camera>();

            outlineCam = cam;

            if (cam.targetTexture != null)
            {
                this.CheckRTSize();
            }
            else
            {
                outlineCam.targetTexture = rendertexture = CreateRT(Screen.width, Screen.height);
            }
        }


        //添加OutLine
        cmdbuffer = new CommandBuffer();
        cmdbuffer.name = "outline";

        outlineCam.AddCommandBuffer(UnityEngine.Rendering.CameraEvent.AfterForwardAlpha, cmdbuffer);
    }

    private void Update()
    {
        this.CheckRTSize();
    }
    private void CheckRTSize()
    {
        int res = (int)this.resolution;

        if (Screen.width == 0 || Screen.height == 0) return;
        if (outlineCam.targetTexture.width != Screen.width / res || outlineCam.targetTexture.height != Screen.height / res)
        {
            outlineCam.targetTexture.Release();
            outlineCam.targetTexture = null;
            outlineCam.targetTexture = rendertexture = CreateRT(Screen.width, Screen.height);
        }
    }
    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        var rect = this.outlineCam.rect;
        this.mMaterial.SetVector("viewport_rect", new Vector4(rect.width, rect.height, rect.x, rect.y));

        //遮罩方法
        //for (int i = 0; i < 5; i++)
        //{
        //    this.blurMat.SetVector("viewport_rect", new Vector4(rect.width, rect.height, rect.x, rect.y));

        //    Graphics.Blit(rendertexture, tempTexture, this.blurMat);

        //    var temp = tempTexture;
        //    tempTexture = rendertexture;
        //    rendertexture = temp;
        //}

        if (rendertexture != null)
            this.mMaterial.SetTexture("_Source", rendertexture);


        //默认方法
        this.mMaterial.SetColor("_Color", OutlineColor);
        this.mMaterial.SetFloat("_Intensity", Intensity);
        this.mMaterial.SetFloat("_PixelWidth", PixelWidth);

        this.mMaterial.SetInt("_Internal", Internal?1:0);


        base.OnRenderImage(source, destination);
    }

    private RenderTexture CreateRT(int width,int height)
    {
        int res = (int)this.resolution;

        RenderTexture rt = new RenderTexture(width / res, height / res, 24, RenderTextureFormat.ARGBFloat);
        rt.filterMode = FilterMode.Point;
        rt.Create();
        return rt;
    }
    private void OnDestroy()
    {
        if (rendertexture != null)
            rendertexture.Release();
        rendertexture = null;
    }
    public Material AddObject(Renderer[] renerers, Color color, int hashCode)
    {
        if (materials == null)
            materials = new Dictionary<int, Material>();
        if (meshRenderers == null)
            meshRenderers = new Dictionary<int, Renderer[]>();

        //Material colorMat = new Material(Shader.Find("Unlit/Color"));

        Material colorMat = new Material(Shader.Find("Unlit/ColorAndDepth"));
        
        colorMat.color = color;

        if (colorMat == null || renerers == null || cmdbuffer == null)
            return null;

        if (!materials.ContainsKey(hashCode))
            materials.Add(hashCode, colorMat);
        if (!meshRenderers.ContainsKey(hashCode))
            meshRenderers.Add(hashCode, renerers);

        foreach (Renderer renderer in renerers)
        {
            cmdbuffer.DrawRenderer(renderer, colorMat);
        }
        return colorMat;
    }
    public Material AddObject(Renderer renderer, Color color,int hashCode)
    {
        Material mat = AddObject(new Renderer[] { renderer }, color, hashCode);

        return mat;
    }

    public void RemoveObject(int hashCode)
    {
        cmdbuffer.Clear();

        meshRenderers.Remove(hashCode);
        materials.Remove(hashCode);

        foreach (var item in meshRenderers)
        {
            foreach (var renders in meshRenderers[item.Key])
            {
                cmdbuffer.DrawRenderer(renders, materials[item.Key]);
            }
        }
    }
}
