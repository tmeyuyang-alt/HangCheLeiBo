
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class XML2UI 
{
    public static TextAsset data;
    public static Font defulatFont;
    public static string AssetPath;


    public static void Generate(TextAsset textAsset, Font font)
    {
        data = textAsset;
        defulatFont = font;

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(data.text);

        //判断资源路径
        AssetPath = UnityEditor.AssetDatabase.GetAssetPath(data);
        int lastPos = AssetPath.LastIndexOf("/");
        AssetPath = AssetPath.Substring(0,lastPos+1);
        Debug.Log(AssetPath);

        XmlNodeList nodes = xmlDoc.SelectSingleNode("PSDUI").ChildNodes;

        Transform canvasRoot = Processing(nodes);

        //后处理 编译所有对象进行重新构建组边缘

        ProcessingGroup(canvasRoot);
    }
    public static Transform Processing(XmlNodeList nodes)
    {
        Transform canvasRoot = null;
        foreach (XmlNode node in nodes)
        {
            switch (node.Name)
            {
                case "psdSize":
                    Vector2Int size = GetSize(node);
                    canvasRoot = CreateCanvas(size.x, size.y);
                    break;
                case "layers":
                    LayersProcessing(node.ChildNodes, canvasRoot);
                    break;
            }
        }
        return canvasRoot;
    }

    public static void LayersProcessing(XmlNodeList nodes,Transform parent)
    {
        foreach (XmlNode layer in nodes)
        {
            LayerProcessing(layer, parent);
        }
    }
    public static void LayerProcessing(XmlNode nodes, Transform parent)
    {
        string type = "";
        string name = "";
        XmlNodeList layers = null;
        GameObject layerGo = null;

        foreach (XmlNode item in nodes)
        {
            switch (item.Name)
            {
                case "type":
                    type = item.InnerText;
                    break;
                case "name":
                    name = item.InnerText;
                    break;
                case "image":
                    layerGo = GetImage(item,parent);
                    break;
                case "layers":
                    layers = item.ChildNodes;
                    break;
            }
        }

        if (layerGo == null)
        {
            layerGo = new GameObject(name);
            layerGo.AddComponent<RectTransform>();
        }
        //layerGo.AddComponent<RectTransform>();
        layerGo.transform.SetParent(parent);
        
        if (layers == null) return;
        LayersProcessing(layers, layerGo.transform);
    }
    public static GameObject GetImage(XmlNode nodes,Transform parent)
    {
        string name = "";
        string imageSource = "";
        string imageType = "";
        List<string> arguments = new List<string>();
        Vector3 position = Vector3.zero;
        Vector2Int size = Vector2Int.zero;


        foreach (XmlNode item in nodes)
        {
            switch (item.Name)
            {
                case "name":
                    name = item.InnerText;
                    break;
                case "imageSource":
                    imageSource = item.InnerText;
                    break;
                case "imageType":
                    imageType = item.InnerText;
                    break;
                case "position":
                    position = GetPosition(item);
                    break;
                case "size":
                    size = GetSize(item);
                    break;
                case "arguments":
                    arguments = GetArguments(item);
                    break;
            }
        }

        GameObject go = new GameObject(name);

        if (imageType == "Label")
        {
            go.transform.SetParent(parent);

            Text text = go.AddComponent<Text>();
            text.text = name;
            text.font = defulatFont;

            if (arguments.Count >= 4)
            {
                Color outCol;
                if (ColorUtility.TryParseHtmlString("#" + arguments[0], out outCol))
                {
                    text.color = outCol;
                }

                text.fontSize = (int)float.Parse(arguments[2]);

                text.text = arguments[3];
            }
            text.alignment = TextAnchor.MiddleCenter;
            RectTransform rectTransform = go.GetComponent<RectTransform>();
            rectTransform.localPosition = position;
            rectTransform.sizeDelta = new Vector2(size.x, size.y) * 1.5f;

        }
        else if (imageType == "Image")
        {
            if (parent != null)
            {
                //判断是否是按钮
                if (go.name.Contains("btn"))
                {
                    go.AddComponent<Button>();
                }

                go.transform.SetParent(parent);

                Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath + name + ".png");

                go.AddComponent<Image>().sprite = sprite;

                RectTransform rectTransform = go.GetComponent<RectTransform>();
                rectTransform.localPosition = position;
                rectTransform.sizeDelta = size;
            }
        }

        return go;
    }

    public static List<string> GetArguments(XmlNode nodes)
    { 
        List<string> arguments = new List<string>();

        foreach (XmlNode item in nodes)
        {
            arguments.Add(item.InnerText);
        }
        return arguments;
    }

    public static Vector2 GetPosition(XmlNode nodes)
    {
        float x = 0, y = 0;
        foreach (XmlNode node in nodes)
        {
            switch (node.Name)
            {
                case "x":
                    x = float.Parse(node.InnerText);
                    break;
                case "y":
                    y = float.Parse(node.InnerText);
                    break;
            }
        }
        return new Vector2(x, y);
    }

    public static Vector2Int GetSize(XmlNode nodes)
    {
        int width = 0, height = 0;
        foreach (XmlNode node in nodes)
        {
            switch (node.Name)
            {
                case "width":
                    width = int.Parse(node.InnerText);
                    break;
                case "height":
                    height = int.Parse(node.InnerText);
                    break;
            }
        }
        return new Vector2Int(width, height);
    }
    public static Transform CreateCanvas(int width, int height)
    {
        GameObject canvasGo = new GameObject("Canvas");

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform rectTransform = canvas.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.localPosition = new Vector2(0, 0);

        return canvasGo.transform;
    }

    public static void GetChildAllTransform(List<Transform> list, Transform parent)
    {
        list.Add(parent);

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
            {
                GetChildAllTransform(list, child);
            }
        }
    }

    public static void RebuildBounds(Transform activeTransform)
    {
        if (activeTransform.childCount == 0) return;

        List<Transform> list = new List<Transform>();

        GetChildAllTransform(list, activeTransform);

        Transform root = list[0];

        list.RemoveAt(0);

        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);

        foreach (Transform child in list)
        {
            if (child.GetComponent<Image>() || child.GetComponent<Text>())
            {
                Vector2 sizeDelta = child.GetComponent<RectTransform>().sizeDelta;
                Vector2 position = child.GetComponent<RectTransform>().position;

                Vector2 cmin = position - sizeDelta * 0.5f;
                Vector2 cmax = position + sizeDelta * 0.5f;

                min = Vector2.Min(min, cmin);
                max = Vector2.Max(max, cmax);

            }
        }

        List<Transform> temps = new List<Transform>();
        //把子物体移除
        int childCount = root.childCount;
        for (int i = 0; i < childCount; i++)
        {
            temps.Add(root.GetChild(0));
            root.GetChild(0).SetParent(null);
        }

        RectTransform rect = root.GetComponent<RectTransform>();

        rect.sizeDelta = (max - min);
        rect.position = min + (max - min) * 0.5f;

        for (int i = 0; i < temps.Count; i++)
        {
            temps[i].SetParent(root, true);
        }
    }

    public static void ProcessingGroup(Transform root)
    {
        //if (root.name.Contains("_group"))
            RebuildBounds(root);

        for (int i = 0; i < root.childCount; i++)
        {
            ProcessingGroup(root.GetChild(i));
        }
    }
}