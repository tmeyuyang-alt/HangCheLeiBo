using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DeviceSignalPoint
{
    public string displayName;
    public DeviceSignalDataType dataType = DeviceSignalDataType.BOOL;
    public string address;
    public bool isWrite = false;
    public bool isPulse = false;
    public DeviceSignalAlarmLevel alarmLevel = DeviceSignalAlarmLevel.None;
    public bool isHistoryData = false;
}

public enum DeviceSignalDataType
{
    BOOL,
    DINT,
    REAL,
    INT,
    LINT
}

public enum DeviceSignalAlarmLevel
{
    None,
    Level1,
    Level2,
    Level3
}
