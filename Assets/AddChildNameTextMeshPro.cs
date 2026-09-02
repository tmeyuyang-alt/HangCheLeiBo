using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AddChildNameTextMeshPro : MonoBehaviour
{
    [ContextMenu("Add Child Name TextMeshPro")]
    public void AddChildNameTexts()
    {
        SyncFirstChildToOtherChildren();
    }

    [ContextMenu("Sync First Child Components And Children")]
    public void SyncFirstChildToOtherChildren()
    {
        if (transform.childCount == 0)
        {
            Debug.LogWarning("No child objects found.", this);
            return;
        }

        Transform firstChild = transform.GetChild(0);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            bool isTemplateChild = child == firstChild;

#if UNITY_EDITOR
            if (!isTemplateChild)
            {
                SyncComponents(firstChild.gameObject, child.gameObject);
                SyncChildObjects(firstChild, child);
            }
#endif

            SetChildText(child, child.name);
        }

        Debug.Log("Synced first child components and child objects to " + (transform.childCount - 1) + " child object(s).", this);
    }

    private static void SetChildText(Transform parent, string value)
    {
        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            SetText(text, value);
        }
    }

    private static void SyncComponents(GameObject templateObject, GameObject targetObject)
    {
#if UNITY_EDITOR
        Component[] templateComponents = templateObject.GetComponents<Component>();
        for (int i = 0; i < templateComponents.Length; i++)
        {
            Component templateComponent = templateComponents[i];
            if (templateComponent == null || templateComponent is Transform)
            {
                continue;
            }

            Component[] targetComponents = targetObject.GetComponents<Component>();
            Component targetComponent = FindMatchingComponent(templateComponent, templateComponents, targetComponents, i);
            if (targetComponent == null)
            {
                targetComponent = Undo.AddComponent(targetObject, templateComponent.GetType());
            }
            else
            {
                Undo.RecordObject(targetComponent, "Sync Component");
            }

            EditorUtility.CopySerialized(templateComponent, targetComponent);
            EditorUtility.SetDirty(targetComponent);
        }

        RemoveExtraComponents(templateComponents, targetObject.GetComponents<Component>());
#endif
    }

    private static void RemoveExtraComponents(Component[] templateComponents, Component[] targetComponents)
    {
#if UNITY_EDITOR
        Dictionary<Type, int> templateCounts = GetComponentCounts(templateComponents);
        Dictionary<Type, int> targetCounts = new Dictionary<Type, int>();

        for (int i = 0; i < targetComponents.Length; i++)
        {
            Component targetComponent = targetComponents[i];
            if (targetComponent == null || targetComponent is Transform)
            {
                continue;
            }

            Type componentType = targetComponent.GetType();
            int currentCount = targetCounts.ContainsKey(componentType) ? targetCounts[componentType] + 1 : 1;
            targetCounts[componentType] = currentCount;

            int allowedCount = templateCounts.ContainsKey(componentType) ? templateCounts[componentType] : 0;
            if (currentCount > allowedCount)
            {
                Undo.DestroyObjectImmediate(targetComponent);
            }
        }
#endif
    }

    private static Dictionary<Type, int> GetComponentCounts(Component[] components)
    {
        Dictionary<Type, int> counts = new Dictionary<Type, int>();
        foreach (Component component in components)
        {
            if (component == null || component is Transform)
            {
                continue;
            }

            Type componentType = component.GetType();
            counts[componentType] = counts.ContainsKey(componentType) ? counts[componentType] + 1 : 1;
        }

        return counts;
    }

    private static Component FindMatchingComponent(
        Component templateComponent,
        Component[] templateComponents,
        Component[] targetComponents,
        int templateIndex)
    {
#if UNITY_EDITOR
        Type componentType = templateComponent.GetType();
        int typeIndex = 0;
        for (int i = 0; i < templateIndex; i++)
        {
            if (templateComponents[i] != null && templateComponents[i].GetType() == componentType)
            {
                typeIndex++;
            }
        }

        int currentIndex = 0;
        foreach (Component targetComponent in targetComponents)
        {
            if (targetComponent == null || targetComponent.GetType() != componentType)
            {
                continue;
            }

            if (currentIndex == typeIndex)
            {
                return targetComponent;
            }

            currentIndex++;
        }
#endif

        return null;
    }

    private static void SyncChildObjects(Transform templateParent, Transform targetParent)
    {
#if UNITY_EDITOR
        RemoveExtraChildObjects(templateParent, targetParent);

        for (int i = 0; i < templateParent.childCount; i++)
        {
            Transform templateChild = templateParent.GetChild(i);
            Transform targetChild = FindMatchingChild(templateParent, targetParent, i);

            if (targetChild == null)
            {
                GameObject clone = Instantiate(templateChild.gameObject, targetParent, false);
                clone.name = templateChild.name;
                Undo.RegisterCreatedObjectUndo(clone, "Sync Child Object");
                targetChild = clone.transform;
            }
            else
            {
                CopyGameObjectSettings(templateChild.gameObject, targetChild.gameObject);
                CopyTransformSettings(templateChild, targetChild);
                SyncComponents(templateChild.gameObject, targetChild.gameObject);
                SyncChildObjects(templateChild, targetChild);
            }

            Undo.RecordObject(targetChild, "Sync Child Order");
            targetChild.SetSiblingIndex(i);
        }
#endif
    }

    private static void RemoveExtraChildObjects(Transform templateParent, Transform targetParent)
    {
#if UNITY_EDITOR
        Dictionary<string, int> templateCounts = GetChildNameCounts(templateParent);
        Dictionary<string, int> targetCounts = new Dictionary<string, int>();
        List<GameObject> extraChildren = new List<GameObject>();

        for (int i = 0; i < targetParent.childCount; i++)
        {
            Transform targetChild = targetParent.GetChild(i);
            int currentCount = targetCounts.ContainsKey(targetChild.name) ? targetCounts[targetChild.name] + 1 : 1;
            targetCounts[targetChild.name] = currentCount;

            int allowedCount = templateCounts.ContainsKey(targetChild.name) ? templateCounts[targetChild.name] : 0;
            if (currentCount > allowedCount)
            {
                extraChildren.Add(targetChild.gameObject);
            }
        }

        foreach (GameObject extraChild in extraChildren)
        {
            Undo.DestroyObjectImmediate(extraChild);
        }
#endif
    }

    private static Dictionary<string, int> GetChildNameCounts(Transform parent)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        for (int i = 0; i < parent.childCount; i++)
        {
            string childName = parent.GetChild(i).name;
            counts[childName] = counts.ContainsKey(childName) ? counts[childName] + 1 : 1;
        }

        return counts;
    }

    private static Transform FindMatchingChild(Transform templateParent, Transform targetParent, int templateIndex)
    {
        Transform templateChild = templateParent.GetChild(templateIndex);
        int nameIndex = 0;
        for (int i = 0; i < templateIndex; i++)
        {
            if (templateParent.GetChild(i).name == templateChild.name)
            {
                nameIndex++;
            }
        }

        int currentIndex = 0;
        for (int i = 0; i < targetParent.childCount; i++)
        {
            Transform targetChild = targetParent.GetChild(i);
            if (targetChild.name != templateChild.name)
            {
                continue;
            }

            if (currentIndex == nameIndex)
            {
                return targetChild;
            }

            currentIndex++;
        }

        return null;
    }

    private static void CopyGameObjectSettings(GameObject templateObject, GameObject targetObject)
    {
#if UNITY_EDITOR
        Undo.RecordObject(targetObject, "Sync GameObject Settings");
        targetObject.SetActive(templateObject.activeSelf);
        targetObject.tag = templateObject.tag;
        targetObject.layer = templateObject.layer;
        GameObjectUtility.SetStaticEditorFlags(targetObject, GameObjectUtility.GetStaticEditorFlags(templateObject));
        EditorUtility.SetDirty(targetObject);
#endif
    }

    private static void CopyTransformSettings(Transform templateTransform, Transform targetTransform)
    {
#if UNITY_EDITOR
        Undo.RecordObject(targetTransform, "Sync Transform");

        RectTransform templateRect = templateTransform as RectTransform;
        RectTransform targetRect = targetTransform as RectTransform;
        if (templateRect != null && targetRect != null)
        {
            targetRect.anchorMin = templateRect.anchorMin;
            targetRect.anchorMax = templateRect.anchorMax;
            targetRect.anchoredPosition3D = templateRect.anchoredPosition3D;
            targetRect.sizeDelta = templateRect.sizeDelta;
            targetRect.pivot = templateRect.pivot;
        }
        else
        {
            targetTransform.localPosition = templateTransform.localPosition;
        }

        targetTransform.localRotation = templateTransform.localRotation;
        targetTransform.localScale = templateTransform.localScale;
        EditorUtility.SetDirty(targetTransform);
#endif
    }

    private static void SetText(TMP_Text text, string value)
    {
#if UNITY_EDITOR
        Undo.RecordObject(text, "Set Child Name Text");
#endif
        text.text = value;

#if UNITY_EDITOR
        EditorUtility.SetDirty(text);
#endif
    }
}
