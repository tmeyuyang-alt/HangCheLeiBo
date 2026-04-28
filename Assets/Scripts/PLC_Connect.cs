using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using S7.Net;
using System;

public class PLC_Connect : MonoBehaviour
{
    private Plc plc;

    public static PLC_Connect Instance;

    public string ipaddrees = "127.0.0.1";

    public int port = 6500;

    public short rack = 0;
    public short slot = 0;

    public CpuType type;

    void Awake()
    {
        Instance = this;
    }

    public void Open()
    {
        if (plc != null)
        {
            plc.Close();
        }

        plc = new Plc(type, ipaddrees, port, rack, slot);

        plc.Open();
    }

    public bool IsConnected()
    {
        if (plc == null) return false;
        return plc.IsConnected;
    }


    public void Close()
    {
        if (plc != null) plc.Close();
    }
    /// <summary>
    /// ∂¡»°
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public object Read(string address)
    {
        if (plc == null || !IsConnected() ||address=="")
            return null;

        try
        {
            object go = plc.Read(address);

            return go;
        }
        catch(Exception ex)
        {
            Debug.LogWarning(ex.Message +" "+ address);
            return null;
        }
    }
    /// <summary>
    /// –¥»Î
    /// </summary>
    /// <param name="address"></param>
    /// <param name="value"></param>
    public void Write(string address,object value)
    {
        if (plc == null || !IsConnected())
            return ;

        plc.Write(address, value);
    }

}
