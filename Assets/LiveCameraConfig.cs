using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LiveCameraConfig : MonoBehaviour
{
    
    public static LiveCameraConfig instance;

    private void Awake()
    {
        instance = this;
        string configPath = Application.streamingAssetsPath + "/camera.config";
        if (File.Exists(configPath))
            config = DataUtil.Deserializer<Dictionary<string, string>>(configPath);
        else
            config = new Dictionary<string, string>();
    }


    public Dictionary<string,string> config = new Dictionary<string,string>();
    
    [ContextMenu("Spwan")]
    public void SpwanConfig()
    {
        string configPath = Application.streamingAssetsPath + "/camera.config";
        config = new Dictionary<string, string>();
        config.Add("209_01","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("209_02","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("209_03","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("倒料器","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("211B","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("211A","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("212A","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("8A","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("8B","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("6A","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("6B","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("5B","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("5A","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("7B","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
        config.Add("7A","rtsp://admin:a123456789@192.168.1.7:554/Streaming/Channels/301");
     
        DataUtil.Serializer<Dictionary<string, string>>(configPath, config);
    }

   

    public string GetPath(string arg)
    {
        string path = "";
        
        config.TryGetValue(arg, out path);
        
        return path;
    }
}
