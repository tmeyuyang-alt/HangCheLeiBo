using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraneSwitchButton : MonoBehaviour
{
    public PLCConfigManager plcConfigManager;
    public Button button;
    public TextMeshProUGUI label;
    public bool createRuntimeButtonIfMissing = false;
    public string labelFormat = "主控：{0}";

    private void OnEnable()
    {
        PLCConfigManager.OnActiveCraneChanged += OnActiveCraneChanged;
    }

    private void Start()
    {
        if (plcConfigManager == null)
        {
            plcConfigManager = PLCConfigManager.Instance;
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button == null && createRuntimeButtonIfMissing)
        {
            CreateRuntimeButton();
        }

        if (label == null && button != null)
        {
            label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (button != null)
        {
            button.onClick.RemoveListener(SwitchCrane);
            button.onClick.AddListener(SwitchCrane);
        }

        RefreshLabel();
    }

    private void OnDisable()
    {
        PLCConfigManager.OnActiveCraneChanged -= OnActiveCraneChanged;
        if (button != null)
        {
            button.onClick.RemoveListener(SwitchCrane);
        }
    }

    private void OnActiveCraneChanged(int craneIndex)
    {
        RefreshLabel();
    }

    private void SwitchCrane()
    {
        if (plcConfigManager == null)
        {
            plcConfigManager = PLCConfigManager.Instance;
        }

        if (plcConfigManager == null)
        {
            return;
        }

        plcConfigManager.SwitchToNextCrane();
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (plcConfigManager == null)
        {
            plcConfigManager = PLCConfigManager.Instance;
        }

        if (label != null && plcConfigManager != null)
        {
            label.text = plcConfigManager.GetActiveCraneDisplayName();
        }
    }

    private void CreateRuntimeButton()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        GameObject buttonObject = new GameObject("CraneSwitchButton_Runtime");
        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-24f, -24f);
        rectTransform.sizeDelta = new Vector2(180f, 44f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.26f, 0.42f, 0.92f);

        button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.18f, 0.36f, 0.55f, 1f);
        colors.pressedColor = new Color(0.08f, 0.2f, 0.34f, 1f);
        button.colors = colors;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.offsetMin = Vector2.zero;
        textRectTransform.offsetMax = Vector2.zero;

        // label = textObject.AddComponent<TextMeshProUGUI>();
        // label.alignment = TextAnchor.MiddleCenter;
        // label.color = Color.white;
        // label.fontSize = 18;
        // label.resizeTextForBestFit = true;
        // label.resizeTextMinSize = 12;
        // label.resizeTextMaxSize = 18;
        //label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
