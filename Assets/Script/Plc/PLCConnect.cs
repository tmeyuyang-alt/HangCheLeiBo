using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using S7.Net;
using System;

public class PLCConnect
{
    private Plc plc;

    public string ipaddrees = "127.0.0.1";

    public int port = 6500;

    public short rack = 0;
    public short slot = 0;

    public CpuType type= CpuType.S71500;
    public async void OpenAsync()
    {
        if (plc != null)
        {
            plc.Close();
        }

        plc = new Plc(type, ipaddrees, port, rack, slot);
        plc.ReadTimeout = 100;
        plc.WriteTimeout = 100;
        //plc.Open();
        await plc.OpenAsync();
    }
    public void  Open()
    {
        if (plc != null)
        {
            plc.Close();
        }

        plc = new Plc(type, ipaddrees, port, rack, slot);
        plc.ReadTimeout = 100;
        plc.WriteTimeout = 100;
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
    /// 读取
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

    public object Read(DataType type, int dbNumber, int startByte, VarType varType, byte bitNumber)
    {
        if (plc == null || !IsConnected())
            return null;

        try
        {
            object go = plc.Read(type, dbNumber, startByte, varType, 1, bitNumber);

            return go;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.Message + " " + startByte + " " + varType);
            return null;
        }
    }
    //plcConnect.Read(adr.DataType, adr.DbNumber, adr.StartByte, adr.VarType, 1, (byte)adr.BitNumber);

    public byte[] ReadBytes(string address)
    {
        //plc.ReadBytes(DataType.DataBlock, 0, 2);
        //object go = plc.Read(address);
        return null;
    }
    /// <summary>
    /// 写入（自动记录操作日志）
    /// </summary>
    /// <param name="address">PLC 地址，如 "DB1.DBW0"</param>
    /// <param name="value">写入的值</param>
    /// <param name="description">可读描述，如 "1#称 启动"（可不填）</param>
    public void Write(string address, object value, string description = "")
    {
        if (plc == null || !IsConnected())
            return;

        plc.Write(address, value);
        PLCOperationLogger.Instance?.Log(address, value?.ToString() ?? "", description);
    }
    public void WriteNoLog(string address, object value, string description = "")
    {
        if (plc == null || !IsConnected())
            return;

        plc.Write(address, value);
       // PLCOperationLogger.Instance?.Log(address, value?.ToString() ?? "", description);
    }


    /// <summary>
    /// 写入（DataType 重载，自动记录操作日志）
    /// </summary>
    /// <param name="description">可读描述，如 "1#称 启动"（可不填）</param>
    public void Write(DataType type, int db, int startByteAdr, object value, int bitAdr = -1, string description = "")
    {
        // BOOL：走 bit 写
        if (value is bool b)
        {
            // bitAdr 必须 0~7
            plc.WriteBit(type, db, startByteAdr, bitAdr, b);

            string boolAddr = $"DB{db}.DBX{startByteAdr}.{bitAdr}";
            PLCOperationLogger.Instance?.Log(boolAddr, b.ToString(), description);
            return;
        }

        // 非 BOOL：严禁走带 bitAdr 的重载
        plc.Write(type, db, startByteAdr, value);

        string addr = $"DB{db}.DBW{startByteAdr}";
        PLCOperationLogger.Instance?.Log(addr, value?.ToString() ?? "", description);
    }

    public Plc GetPlc()
    {
        return plc;
    }

}
