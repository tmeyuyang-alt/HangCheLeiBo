using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UMP;
using UnityEngine;
using UnityEngine.UI;

public class MonitorPanel : MonoBehaviour
{
    private enum ResizeMode
    {
        None,
        Left,
        Right,
        Top,
        Bottom,
        LeftTop,
        RightTop,
        LeftBottom,
        RightBottom
    }

    [Header("Media")]
    public UniversalMediaPlayer mediaPlayer;
    public RawImage videoImage;
    public string cameraKey;
    public bool playOnEnable = true;
    public bool stopOnDisable = true;

    [Header("Window")]
    public RectTransform dragArea;
    public RectTransform dragHandle;
    public Vector2 minSize = new Vector2(320, 180);
    public Vector2 maxSize = new Vector2(1600, 900);
    public float defaultDragHandleHeight = 38f;
    public float resizeHandleSize = 18f;
    public bool clampToDragArea = true;

    public static MonitorPanel ActivePanel;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private bool _dragging;
    private bool _resizing;
    private ResizeMode _resizeMode;
    private Vector2 _lastPointerLocalPosition;
    private Vector2 _initialSize;
    private Vector2 _initialAnchoredPosition;
    private bool _initialized;

    private void Awake()
    {
        Initialize();
        LoadConfiguredAddress();
    }

    private void OnEnable()
    {
        Initialize();
        LoadConfiguredAddress();

        if (playOnEnable)
            DelayPlay();
    }

    private void OnDisable()
    {
        _dragging = false;
        _resizing = false;
        _resizeMode = ResizeMode.None;

        if (stopOnDisable && mediaPlayer != null)
            mediaPlayer.Stop();
    }

    private void Update()
    {
        HandleWindowInput();
    }

    public void Play()
    {
        if (mediaPlayer == null)
        {
            Debug.LogWarning("[MonitorPanel] UniversalMediaPlayer is not assigned.");
            return;
        }

        string url = GetConfiguredUrl();
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning($"[MonitorPanel] Monitor address is empty. key={GetCameraKey()}");
            return;
        }

        mediaPlayer.Path = url.Trim();
        mediaPlayer.Play();
    }

    public void Stop()
    {
        if (mediaPlayer != null)
            mediaPlayer.Stop();
    }

    public void Hidden()
    {
        gameObject.SetActive(false);
    }

    public void RestPanel()
    {
        Initialize();
        _rectTransform.sizeDelta = _initialSize;
        _rectTransform.anchoredPosition = _initialAnchoredPosition;
        gameObject.SetActive(true);
    }

    public async void DelayPlay()
    {
        await Task.Delay(300);

        if (this == null || !isActiveAndEnabled)
            return;

        Play();
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        if (mediaPlayer == null)
            mediaPlayer = GetComponentInChildren<UniversalMediaPlayer>(true);

        if (videoImage == null)
            videoImage = GetComponentInChildren<RawImage>(true);

        if (mediaPlayer != null && videoImage != null)
        {
            mediaPlayer.RenderingObjects = new[] { videoImage.gameObject };
            mediaPlayer.AutoPlay = false;
            mediaPlayer.Loop = true;
        }

        _initialSize = _rectTransform.sizeDelta;
        _initialAnchoredPosition = _rectTransform.anchoredPosition;
        _initialized = true;
    }

    private void LoadConfiguredAddress()
    {
        string url = GetConfiguredUrl();
        if (mediaPlayer != null && !string.IsNullOrWhiteSpace(url))
            mediaPlayer.Path = url.Trim();
    }

    private string GetConfiguredUrl()
    {
        string key = GetCameraKey();

        if (LiveCameraConfig.instance != null)
        {
            string url = LiveCameraConfig.instance.GetPath(key);
            if (!string.IsNullOrWhiteSpace(url))
                return url;
        }

        string path = Path.Combine(Application.streamingAssetsPath, "camera.config");
        if (!File.Exists(path))
            return string.Empty;

        try
        {
            Dictionary<string, string> config = DataUtil.Deserializer<Dictionary<string, string>>(path);
            if (config != null && config.TryGetValue(key, out string url))
                return url;
        }
        catch
        {
            Debug.LogWarning("[MonitorPanel] Failed to read camera.config.");
        }

        return string.Empty;
    }

    private string GetCameraKey()
    {
        return string.IsNullOrWhiteSpace(cameraKey) ? gameObject.name : cameraKey.Trim();
    }

    private void HandleWindowInput()
    {
        if (_rectTransform == null)
            return;

        Vector2 screenPosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, screenPosition, GetEventCamera()))
                return;

            if (!TryGetPointerLocalPosition(screenPosition, out _lastPointerLocalPosition))
                return;

            ActivePanel = this;
            transform.SetAsLastSibling();
            _resizeMode = GetResizeMode(screenPosition);
            _resizing = _resizeMode != ResizeMode.None;
            _dragging = !_resizing && CanDrag(screenPosition);
        }

        if (Input.GetMouseButton(0))
        {
            if (!TryGetPointerLocalPosition(screenPosition, out Vector2 localPosition))
                return;

            Vector2 delta = localPosition - _lastPointerLocalPosition;

            if (_resizing)
                Resize(delta);
            else if (_dragging)
                Drag(delta);

            _lastPointerLocalPosition = localPosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            _dragging = false;
            _resizing = false;
            _resizeMode = ResizeMode.None;
        }
    }

    private bool CanDrag(Vector2 screenPosition)
    {
        if (dragHandle != null)
            return RectTransformUtility.RectangleContainsScreenPoint(dragHandle, screenPosition, GetEventCamera());

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPosition, GetEventCamera(), out Vector2 localPoint))
            return false;

        Rect rect = _rectTransform.rect;
        return localPoint.y >= rect.yMax - defaultDragHandleHeight && localPoint.y <= rect.yMax;
    }

    private void Drag(Vector2 delta)
    {
        _rectTransform.anchoredPosition += delta;
        ClampToArea();
    }

    private void Resize(Vector2 delta)
    {
        Vector2 size = _rectTransform.sizeDelta;
        Vector2 position = _rectTransform.anchoredPosition;

        bool left = _resizeMode == ResizeMode.Left || _resizeMode == ResizeMode.LeftTop || _resizeMode == ResizeMode.LeftBottom;
        bool right = _resizeMode == ResizeMode.Right || _resizeMode == ResizeMode.RightTop || _resizeMode == ResizeMode.RightBottom;
        bool top = _resizeMode == ResizeMode.Top || _resizeMode == ResizeMode.LeftTop || _resizeMode == ResizeMode.RightTop;
        bool bottom = _resizeMode == ResizeMode.Bottom || _resizeMode == ResizeMode.LeftBottom || _resizeMode == ResizeMode.RightBottom;

        if (left)
        {
            size.x -= delta.x;
            position.x += delta.x * 0.5f;
        }
        else if (right)
        {
            size.x += delta.x;
            position.x += delta.x * 0.5f;
        }

        if (top)
        {
            size.y += delta.y;
            position.y += delta.y * 0.5f;
        }
        else if (bottom)
        {
            size.y -= delta.y;
            position.y += delta.y * 0.5f;
        }

        size.x = Mathf.Clamp(size.x, minSize.x, maxSize.x);
        size.y = Mathf.Clamp(size.y, minSize.y, maxSize.y);

        _rectTransform.sizeDelta = size;
        _rectTransform.anchoredPosition = position;
        ClampToArea();
    }

    private ResizeMode GetResizeMode(Vector2 screenPosition)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, screenPosition, GetEventCamera(), out Vector2 localPoint))
            return ResizeMode.None;

        Rect rect = _rectTransform.rect;
        bool left = Mathf.Abs(localPoint.x - rect.xMin) <= resizeHandleSize;
        bool right = Mathf.Abs(localPoint.x - rect.xMax) <= resizeHandleSize;
        bool top = Mathf.Abs(localPoint.y - rect.yMax) <= resizeHandleSize;
        bool bottom = Mathf.Abs(localPoint.y - rect.yMin) <= resizeHandleSize;

        if (left && bottom) return ResizeMode.LeftBottom;
        if (right && bottom) return ResizeMode.RightBottom;
        if (dragHandle != null)
        {
            if (left && top) return ResizeMode.LeftTop;
            if (right && top) return ResizeMode.RightTop;
            if (left) return ResizeMode.Left;
            if (right) return ResizeMode.Right;
            if (top) return ResizeMode.Top;
            if (bottom) return ResizeMode.Bottom;
        }
        return ResizeMode.None;
    }

    private void ClampToArea()
    {
        RectTransform area = GetDragArea();
        if (!clampToDragArea || area == null)
            return;

        Vector3[] areaCorners = new Vector3[4];
        Vector3[] panelCorners = new Vector3[4];
        area.GetWorldCorners(areaCorners);
        _rectTransform.GetWorldCorners(panelCorners);

        Vector3 offset = Vector3.zero;
        if (panelCorners[0].x < areaCorners[0].x) offset.x = areaCorners[0].x - panelCorners[0].x;
        if (panelCorners[2].x > areaCorners[2].x) offset.x = areaCorners[2].x - panelCorners[2].x;
        if (panelCorners[0].y < areaCorners[0].y) offset.y = areaCorners[0].y - panelCorners[0].y;
        if (panelCorners[2].y > areaCorners[2].y) offset.y = areaCorners[2].y - panelCorners[2].y;

        _rectTransform.position += offset;
    }

    private bool TryGetPointerLocalPosition(Vector2 screenPosition, out Vector2 localPosition)
    {
        RectTransform reference = GetDragArea();
        if (reference == null)
        {
            localPosition = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, screenPosition, GetEventCamera(), out localPosition);
    }

    private RectTransform GetDragArea()
    {
        if (dragArea != null)
            return dragArea;

        if (_rectTransform != null && _rectTransform.parent is RectTransform parentRect)
            return parentRect;

        if (_canvas != null)
            return _canvas.transform as RectTransform;

        return null;
    }

    private Camera GetEventCamera()
    {
        if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return _canvas.worldCamera;
    }
}
