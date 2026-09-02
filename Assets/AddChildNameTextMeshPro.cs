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
        if (transform.childCount == 0)
        {
            Debug.LogWarning("No child objects found.", this);
            return;
        }

        Transform firstChild = transform.GetChild(0);
        TMP_Text templateText = firstChild.GetComponentInChildren<TMP_Text>(true);
        if (templateText == null)
        {
            Debug.LogWarning("The first child does not contain a TextMeshPro object.", firstChild);
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            bool isTemplateChild = child == firstChild;
            TMP_Text text = isTemplateChild ? templateText : FindDirectChildText(child);

            if (text == null)
            {
                GameObject textObject = Instantiate(templateText.gameObject, child, false);
                textObject.name = templateText.gameObject.name;
                text = textObject.GetComponent<TMP_Text>();

#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(textObject, "Add Child Name TextMeshPro");
#endif
            }
            else if (!isTemplateChild)
            {
                CopyTemplateSettings(templateText, text);
            }

            SetText(text, child.name);
        }
    }

    private static TMP_Text FindDirectChildText(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            TMP_Text text = parent.GetChild(i).GetComponent<TMP_Text>();
            if (text != null)
            {
                return text;
            }
        }

        return null;
    }

    private static void CopyTemplateSettings(TMP_Text templateText, TMP_Text targetText)
    {
#if UNITY_EDITOR
        if (templateText.GetType() != targetText.GetType())
        {
            Debug.LogWarning("TextMeshPro type is different, skipped style sync: " + targetText.name, targetText);
            return;
        }

        Undo.RecordObject(targetText, "Sync TextMeshPro Settings");
        EditorUtility.CopySerialized(templateText, targetText);
        CopyTransformSettings(templateText.transform, targetText.transform);
        EditorUtility.SetDirty(targetText);
#endif
    }

    private static void CopyTransformSettings(Transform templateTransform, Transform targetTransform)
    {
#if UNITY_EDITOR
        Undo.RecordObject(targetTransform, "Sync TextMeshPro Transform");

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
        targetTransform.gameObject.name = templateTransform.gameObject.name;
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
