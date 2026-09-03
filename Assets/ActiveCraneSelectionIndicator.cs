using System;
using UnityEngine;
using UnityEngine.UI;

public class ActiveCraneSelectionIndicator : MonoBehaviour
{
    [Serializable]
    public class IndicatorTarget
    {
        public int craneNumber = 1;
        public Graphic graphic;
        public SpriteRenderer spriteRenderer;
        public Renderer renderer;
        public Color selectedColor = new Color(0.18f, 0.75f, 1f, 1f);
        public Color unselectedColor = new Color(1f, 1f, 1f, 0.35f);

        public void Apply(bool isSelected)
        {
            Color color = isSelected ? selectedColor : unselectedColor;

            if (graphic != null)
            {
                graphic.color = color;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }

            if (renderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", color);
                block.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(block);
            }
        }
    }

    public PLCConfigManager plcConfigManager;
    public IndicatorTarget[] targets =
    {
        new IndicatorTarget { craneNumber = 1 },
        new IndicatorTarget { craneNumber = 2 }
    };

    private void OnEnable()
    {
        PLCConfigManager.OnActiveCraneChanged += OnActiveCraneChanged;
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        PLCConfigManager.OnActiveCraneChanged -= OnActiveCraneChanged;
    }

    private void OnActiveCraneChanged(int craneIndex)
    {
        ApplySelection(craneIndex + 1);
    }

    public void Refresh()
    {
        if (plcConfigManager == null)
        {
            plcConfigManager = PLCConfigManager.Instance;
        }

        int activeCraneNumber = plcConfigManager != null ? plcConfigManager.GetActiveCraneNumber() : 1;
        ApplySelection(activeCraneNumber);
    }

    public void SelectCrane1()
    {
        SwitchToCraneNumber(1);
    }

    public void SelectCrane2()
    {
        SwitchToCraneNumber(2);
    }

    public void SwitchToCraneNumber(int craneNumber)
    {
        if (plcConfigManager == null)
        {
            plcConfigManager = PLCConfigManager.Instance;
        }

        if (plcConfigManager == null)
        {
            return;
        }

        plcConfigManager.SwitchToCrane(craneNumber - 1);
    }

    private void ApplySelection(int activeCraneNumber)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
            {
                continue;
            }

            targets[i].Apply(targets[i].craneNumber == activeCraneNumber);
        }
    }
}
