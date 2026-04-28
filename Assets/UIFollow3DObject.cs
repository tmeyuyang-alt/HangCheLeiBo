using UnityEngine;

public class UIFollow3DObject : MonoBehaviour
{
    public Transform target3D;   // 需跟随的3D物体
    public RectTransform uiElement; // UI元素的RectTransform
    public Vector2 offset;        // UI偏移量

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (uiElement==null)
        {
            uiElement = this.GetComponent <RectTransform>();
        }
    }

    void Update()
    {
        if (target3D == null) return;

        // 将3D坐标转换为屏幕坐标
        Vector3 screenPos = mainCamera.WorldToScreenPoint(target3D.position);
        uiElement.position = screenPos + (Vector3)offset;

        // 可选：检测物体是否在相机视野内
        //Vector3 viewportPos = mainCamera.WorldToViewportPoint(target3D.position);
        //bool isVisible = viewportPos.z > 0 && viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1;
        //uiElement.gameObject.SetActive(isVisible);
    }
}
