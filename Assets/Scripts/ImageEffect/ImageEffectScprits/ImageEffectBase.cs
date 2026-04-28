using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ImageEffectBase : MonoBehaviour
{
    public Material mMaterial;
    public Shader mShader;
    public string ShaderName;

    public virtual void OnEnable()
    {
        if (ShaderName != null&&mShader==null)
        {
            mShader = Shader.Find(ShaderName);
        }
        if (mMaterial == null&&mShader!=null)
        {
            mMaterial = new Material(mShader);
        }
    }

    public virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (mMaterial != null)
        {
            Graphics.Blit(source, destination, mMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
