using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 外发光脚本
/// 需要配合OutLineEffect.cs使用，OutLineEffect.cs挂在到Camera即可
/// </summary>
public class OutlineObj : MonoBehaviour
{
    public Color color = Color.white;

    private Material material;

    private OutLineEffect outlineEffect;

    public bool AllChildMesh = false;

    private Renderer[] renderers;


    public bool breathing = false;

    private void Start()
    {
        outlineEffect = GameObject.FindObjectOfType<OutLineEffect>();

        if (this.enabled)
        {
            ActiveOutline();
        }
    }

    public void OnDisable()
    {
        outlineEffect?.RemoveObject(gameObject.GetHashCode());
    }
    public void OnEnable()
    {
        if (AllChildMesh)
        {
            var rendererlist = new List<Renderer>();
            UnityUtil.GetAllComponent<Renderer>(this.transform, rendererlist);
            renderers = rendererlist.ToArray();
        }
        else
        {
            renderers = new Renderer[] { this.GetComponent<MeshRenderer>() };
        }

        ActiveOutline();
    }
    /// <summary>
    /// 激活外发光
    /// </summary>
    public void ActiveOutline()
    {
        if (!AllChildMesh)
        {
            material = outlineEffect?.AddObject(renderers[0], color, gameObject.GetHashCode());
        }
        else
        {
            material = outlineEffect?.AddObject(renderers, color, gameObject.GetHashCode());
        }
    }
    public void OnValidate()
    {
        if (material != null)
            material.color = color;
    }

    public void SetColor(Color color)
    {
        this.color = color;

        if (this.material == null)
        {
            ActiveOutline();
            return;
        }
        material.color = color;
    }

    private void Update()
    {
        if (breathing)
        {
            material.color = Mathf.Abs(Mathf.Sin(Time.time*2)) * color;
        }
    }
}
