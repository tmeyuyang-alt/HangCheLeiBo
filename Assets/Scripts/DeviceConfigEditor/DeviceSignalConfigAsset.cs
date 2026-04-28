using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DeviceSignalConfigAsset", menuName = "Config/Device Signal Config")]
public class DeviceSignalConfigAsset : ScriptableObject
{
    public string sharedIpAddress = string.Empty;
    public List<DeviceSignalPoint> points = new List<DeviceSignalPoint>();
}
