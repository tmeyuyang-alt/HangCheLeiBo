using UnityEngine;
using System.Reflection;

public class PLCConfigSetter : MonoBehaviour
{
    [Header("指定要替换的 plcConfigManager")]
    public PLCConfigManager targetPlcConfigManager;

    [ContextMenu("替换所有子物体组件里的PLCConfigManager")]
    public void ReplaceAll()
    {
        if (targetPlcConfigManager == null)
        {
            Debug.LogError("请先在 Inspector 中指定 targetPlcConfigManager");
            return;
        }

        // 遍历当前物体以及所有子物体
        Component[] allComponents = GetComponentsInChildren<Component>(true);
        foreach (Component comp in allComponents)
        {
            if (comp == null) continue;

            // 获取类型
            System.Type type = comp.GetType();
            // 查找是否有 plcConfigManager 这个字段
            FieldInfo field = type.GetField("plcConfigManager",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null && field.FieldType == typeof(PLCConfigManager))
            {
                field.SetValue(comp, targetPlcConfigManager);
                Debug.Log($"已替换 {comp.GetType().Name} 在 {comp.gameObject.name} 上的 plcConfigManager");
            }
        }

        Debug.Log("替换完成！");
    }
}