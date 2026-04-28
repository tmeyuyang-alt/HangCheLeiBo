using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanImageEffect : ImageEffectBase
{
    public override void OnEnable()
    {
        base.OnEnable();

        GetComponent<Camera>().depthTextureMode = DepthTextureMode.DepthNormals;
    }
    public override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        base.OnRenderImage(source, destination);
    }
}
