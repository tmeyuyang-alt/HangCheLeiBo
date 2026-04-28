using S7.Net;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YamlDotNet.Serialization;
public class DataUtil
{
    /// <summary>
    /// 使用Yaml序列话对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="path"></param>
    public static void Serializer<T>(string path,T obj)
    {
        StreamWriter yamlWriter = File.CreateText(path);
        Serializer yamlSerializer = new Serializer();
        yamlSerializer.Serialize(yamlWriter, obj);
        yamlWriter.Close();
    }

    public static string SerializerToString<T>(T obj)
    {
        Serializer yamlSerializer = new Serializer();
        return yamlSerializer.Serialize(obj);
    }
    /// <summary>
    /// 使用Yaml反序列化
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    public static T Deserializer<T>(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException();

        StreamReader yamlReader = File.OpenText(path);

        Deserializer yamlDeserializer = new Deserializer();
     
        T info = yamlDeserializer.Deserialize<T>(yamlReader);
        
        yamlReader.Close();
        
        return info;
    }







    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="config"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static T GetConfigData<T>(List<object> config, int index) where T : ConfigDataBase, new()
    {
        var item = config[index];

        T cdb;
        if (item is Dictionary<object, object>)
        {
            cdb = DicToConfingData<T>(item as Dictionary<object, object>);
            config[index] = cdb;
        }
        else
        {
            cdb = item as T;
        }
        return cdb;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static T DicToConfingData<T>(Dictionary<object, object> data) where T : new()
    {
        T t = new T();

        foreach (var item in data)
        {
            var field = t.GetType().GetField(item.Key.ToString());

            if (field == null) continue;

            if (field.FieldType == typeof(bool))
            {
                field.SetValue(t, bool.Parse(item.Value.ToString()));
            }
            else
            {
                field.SetValue(t, item.Value);
            }
        }
        return t;
    }

    public static int GetBitNumber(string variable)
    {
        var adr = new PLCAddress(variable);

        switch (adr.VarType)
        {
            case VarType.Bit:
                return 1;
            case VarType.Int:
            case VarType.Word:
                return 16;
            case VarType.DInt:
            case VarType.DWord:
                return 32;
            case VarType.Real:
                return 64;
            case VarType.LReal:
                return 64;
        }
        return 0;
    }
    public static string ToCsharpType(string variable)
    {
        var adr = new PLCAddress(variable);

        switch (adr.VarType)
        {
            case VarType.Bit:
                return "Bit";
            case VarType.Int:
            case VarType.Word:
                return "Int16";
            case VarType.DInt:
            case VarType.DWord:
                return "Int32";
            case VarType.Real:
                return "Float";
        }
        return "int";
    }

    public static string PlcToCsharpType(string address)
    {
       var tempAddr = address.Split('&');

        if (tempAddr.Length == 2)
        {
            switch (tempAddr[1].ToUpper())
            {
                case "REAL":
                    return "Float";
                case "INT":
                case "WORD":
                    return "Int16";
                case "DINT":
                case "DWORD":
                    return "INT32";
                case "BOOL":
                    return "BOOL";
                case "BYTE":
                    return "BYTE";
                default:
                    return "INT";
            }
        }
        return "INT";
    }
}
