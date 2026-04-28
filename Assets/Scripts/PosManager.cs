using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PosManager : MonoBehaviour
{

    static Dictionary<string, Position> m_PosDic = new Dictionary<string, Position>();

    private void Awake()
    {
        Load();
    }

    public static bool GetPos(string key, out Vector3 vec)
    {
        if (m_PosDic.ContainsKey(key))
        {
            vec = new Vector3(m_PosDic[key].x, m_PosDic[key].y, m_PosDic[key].z);
            return true;
        }
        vec = Vector3.zero;
        return false;
    }

    public static void SetPos(string key, Position pos)
    {
        if (m_PosDic.ContainsKey(key))
        {
            m_PosDic[key] = pos;
        }
        else
        {
            m_PosDic.Add(key, pos);
        }
        Save();
    }

    public static void Save() {
        string path = Application.streamingAssetsPath + "/pos_config.yaml";

        DataUtil.Serializer<Dictionary<string, Position>>(path, m_PosDic);
    }

    public static void Load()
    {
        string path = Application.streamingAssetsPath + "/pos_config.yaml";

        m_PosDic = DataUtil.Deserializer<Dictionary<string, Position>>(path);
    }
}
