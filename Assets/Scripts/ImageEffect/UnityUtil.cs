using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;

public static class UnityUtil
{
    public static Transform GetChildNode<T>(Transform t,string gameName) where T : MonoBehaviour
    {

        return null;
    }

    /// <summary>
    /// 用某个轴去朝向物体
    /// </summary>
    /// <param name="tr_self">朝向的本体</param>
    /// <param name="lookPos">朝向的目标</param>
    /// <param name="directionAxis">方向轴，取决于你用那个方向去朝向</param>
    public static void AxisLookAt(this Transform tr_self, Vector3 lookPos, Vector3 directionAxis)
    {
        var rotation = tr_self.rotation;
        var targetDir = lookPos - tr_self.position;
        //指定哪根轴朝向目标,自行修改Vector3的方向
        var fromDir = tr_self.rotation * directionAxis;
        //计算垂直于当前方向和目标方向的轴
        var axis = Vector3.Cross(fromDir, targetDir).normalized;
        //计算当前方向和目标方向的夹角
        var angle = Vector3.Angle(fromDir, targetDir);
        //将当前朝向向目标方向旋转一定角度，这个角度值可以做插值
        tr_self.rotation = Quaternion.AngleAxis(angle, axis) * rotation;
        tr_self.localEulerAngles = new Vector3(0, tr_self.localEulerAngles.y, 90);//后来调试增加的，因为我想让x，z轴向不会有任何变化
    }

    public static void GetIpAndPort(string input, out string ipaddress, out int port)
    {
        string[] temp = input.Split(':');
        ipaddress = temp[0];
        port = int.Parse(temp[1]);
    }
    public static T FindParentComponent<T>(Transform root) where T : Component
    {
        T component = root.GetComponent<T>();

        if (component != null)
        {
            return component;
        }
        else if (root.parent != null)
        {
            return FindParentComponent<T>(root.parent);
        }

        return null;
    }

    public static void GetAllComponent<T>(Transform root, List<T> list) where T : Component
    {
        if (list == null)
        {
            list = new List<T>();
        }
        T t = root.GetComponent<T>();
        if (t != null)
        {
            list.Add(t);
        }

        int childCount = root.childCount;

        for (int i = 0; i < childCount; i++)
        {
            GetAllComponent<T>(root.GetChild(i), list);
        }
    }
    /// <summary>
    /// 递归找物体
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="gameobjectName"></param>
    /// <returns></returns>
    public static Transform Find(Transform parent, string gameobjectName)
    {
        if (parent == null) return null;
        int count = parent.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = parent.GetChild(i);

            if (gameobjectName == child.name)
                return child;

            if (child != null)
            {
               Transform t =  Find(child, gameobjectName);
                if (t != null) return t;
            }
        }
        return null;
    }

    /// <summary>
    /// 递归找组件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parent"></param>
    /// <param name="gameobjectName"></param>
    /// <returns></returns>
    public static T Find<T>(Transform parent, string gameobjectName) where T : MonoBehaviour
    {
        if (parent == null) return null;
        int count = parent.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = parent.GetChild(i);

            if (gameobjectName == child.name)
            {
                T t = child.GetComponent<T>();
                if (t != null)
                    return t;
            }
            if (child != null)
            {
                if (child.childCount > 0)
                {
                    T t = Find<T>(child, gameobjectName);
                    if (t != null) return t;
                }
            }
        }
        return null;
    }
    /// <summary>
    /// 字典转Object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fieldDic"></param>
    /// <returns></returns>
    public static T DicToObject<T>(Dictionary<string, object> fieldDic) where T : new()
    {
        if (fieldDic == null) return default(T);

        var obj = new T();

        foreach (var d in fieldDic)
        {
            try
            {
                var value = d.Value;

                obj.GetType().GetField(d.Key).SetValue(obj, value);

            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        return obj;
    }
    public static Bounds CalculationAABB(Transform root)
    {
        Vector3 center = Vector3.zero;
        Renderer[] renders = root.GetComponentsInChildren<Renderer>();

        foreach ( Renderer child in renders)
        {
            center += child.bounds.center;
        }

        center /= renders.Length;

        Bounds bounds = new Bounds(center, Vector3.zero);

        foreach (Renderer child in renders)
        {
            bounds.Encapsulate(child.bounds);
        }

        return bounds;
    }
    /// <summary>
    /// 设置物体的所有层
    /// </summary>
    /// <param name="root"></param>
    /// <param name="layer"></param>
    public static void SetLayer(Transform root,int layer,bool includeSelf = true)
    {
        if (includeSelf) root.gameObject.layer = layer;
        foreach (Transform tran in root.GetComponentsInChildren<Transform>())
        {
            tran.gameObject.layer = layer;
        }
    }

    public static void SetPosX(this Transform m_transform, float value)
    {
        Vector3 vec = m_transform.position;
        vec.x = value;
        m_transform.position = vec;
    }

    public static void SetPosY(this Transform m_transform, float value)
    {
        Vector3 vec = m_transform.position;
        vec.y = value;
        m_transform.position = vec;
    }

    public static void SetPosZ(this Transform m_transform, float value)
    {
        Vector3 vec = m_transform.position;
        vec.z = value;
        m_transform.position = vec;
    }

    public static void SetRotX(this Transform m_transform, float value)
    {
        Vector3 euler = m_transform.localEulerAngles;
        euler.x = value;
        m_transform.localEulerAngles = euler;
    }

    public static void SetRotY(this Transform m_transform, float value)
    {
        Vector3 euler = m_transform.localEulerAngles;
        euler.y = value;
        m_transform.localEulerAngles = euler;
    }
    public static void SetRotZ(this Transform m_transform, float value)
    {
        Vector3 euler = m_transform.localEulerAngles;
        euler.z = value;
        m_transform.localEulerAngles = euler;
    }


    public static bool HasGo(Vector3 position, Vector3 size, string tag = "DynamicObject")
    {
        Collider[] colliders = Physics.OverlapBox(position, size, Quaternion.identity);
        bool hasDo = false;
        for (int j = 0; j < colliders.Length; j++)
        {
            if (colliders[j].CompareTag(tag)) hasDo = true;
        }

        return hasDo;
    }
}
