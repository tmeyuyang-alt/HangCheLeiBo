using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class XML2UIEditor : EditorWindow
{
    [MenuItem("Window/Xml2UI")]
    static void OpenWindow()
    {
        CreateWindow<XML2UIEditor>();
    }

    TextAsset textAsset;
    Font defulatFont;
    public void OnGUI()
    {
        GUILayout.Label("XML文件");
        
        textAsset = EditorGUILayout.ObjectField(textAsset, typeof(TextAsset), true) as TextAsset;
        defulatFont = EditorGUILayout.ObjectField(defulatFont, typeof(Font), true) as Font;

        if (GUILayout.Button("生成UI"))
        {
            XML2UI.Generate(textAsset, defulatFont);
        }
        GUILayout.Space(10);
        if (GUILayout.Button("重新构建组的边缘"))
        {
            XML2UI.RebuildBounds(Selection.activeTransform);
        }
    }

    //public static void GetChildAllTransform(List<Transform> list, Transform parent)
    //{
    //   list.Add(parent);

    //    for (int i = 0; i < parent.childCount; i++)
    //    {
    //        Transform child = parent.GetChild(i);
    //        if (child != null)
    //        {
    //            GetChildAllTransform(list, child);
    //        }
    //    }
    //}

    //public static void RebuildBounds(Transform activeTransform)
    //{
    //    List<Transform> list = new List<Transform>();

    //    GetChildAllTransform(list, activeTransform);

    //    Transform root = list[0];

    //    list.RemoveAt(0);

    //    Vector2 max = new Vector2(float.MinValue, float.MinValue);
    //    Vector2 min = new Vector2(float.MaxValue,float.MaxValue);

    //    foreach (Transform child in list)
    //    {
    //        if (child.GetComponent<Image>() || child.GetComponent<Text>())
    //        {
    //            Vector2 sizeDelta = child.GetComponent<RectTransform>().sizeDelta;
    //            Vector2 position = child.GetComponent<RectTransform>().position;

    //            Vector2 cmin = position - sizeDelta*0.5f;
    //            Vector2 cmax = position + sizeDelta*0.5f;

    //            min = Vector2.Min(min, cmin);
    //            max = Vector2.Max(max, cmax);

    //        }
    //    }

    //    List<Transform> temps = new List<Transform>();
    //    //把子物体移除
    //    int childCount = root.childCount;
    //    for (int i = 0; i < childCount; i++)
    //    {
    //        temps.Add(root.GetChild(0));
    //        root.GetChild(0).SetParent(null);
    //    }

    //    RectTransform rect = root.GetComponent<RectTransform>();
    //    rect.sizeDelta = (max - min);
    //    rect.position = min + (max - min) * 0.5f;

    //    for (int i = 0; i < temps.Count; i++)
    //    {
    //        temps[i].SetParent(root,true);
    //    }
    //}
}
