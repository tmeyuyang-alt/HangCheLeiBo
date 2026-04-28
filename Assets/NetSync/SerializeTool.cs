using System;

public class SerializeTool
{
    public static byte[] Encode(object arg)
    {
        return System.Text.Encoding.UTF8.GetBytes(LitJson.JsonMapper.ToJson(arg));
    }
    public static string Encode2Str(object arg)
    {
        return LitJson.JsonMapper.ToJson(arg);
    }

    public static T Decode<T>(byte[] buffer)
    {
        return LitJson.JsonMapper.ToObject<T>(System.Text.Encoding.UTF8.GetString(buffer));
    }

    public static T Decode<T>(string msg)
    {
        return LitJson.JsonMapper.ToObject<T>(msg);
    }
}
