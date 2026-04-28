using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;


public class NetTool
{

    public static long GetTimeStamp()
    {
        TimeSpan ts = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1);

        return (long)ts.TotalMilliseconds;
    }

    /// <summary>        

    /// 获取操作系统已用的端口号        

    /// </summary>        

    /// <returns></returns>        

    public static IList PortIsUsed()
    {

        //获取本地计算机的网络连接和通信统计数据的信息            

        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

        //返回本地计算机上的所有Tcp监听程序            

        IPEndPoint[] ipsTCP = ipGlobalProperties.GetActiveTcpListeners();

        //返回本地计算机上的所有UDP监听程序            

        IPEndPoint[] ipsUDP = ipGlobalProperties.GetActiveUdpListeners();

        //返回本地计算机上的Internet协议版本4(IPV4 传输控制协议(TCP)连接的信息。            

        TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

        IList allPorts = new ArrayList();

        foreach (IPEndPoint ep in ipsTCP)
        {

            allPorts.Add(ep.Port);

        }

        foreach (IPEndPoint ep in ipsUDP)
        {

            allPorts.Add(ep.Port);

        }

        foreach (TcpConnectionInformation conn in tcpConnInfoArray)
        {

            allPorts.Add(conn.LocalEndPoint.Port);

        }

        return allPorts;

    }

    public static int GetPort()
    {
        Random rand = new Random();

        IList used = PortIsUsed();

        int port = rand.Next(1024, 65535);

        for (int i = 0; i < used.Count; i++)
        {
            if ((int)used[i] == port)
            {
                port = rand.Next(1024, 65535);

                i = 0;
            }
        }

        return port;
    }

    public static string GetIP()
    {

        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface adater in adapters)
        {

            if (adater.Supports(NetworkInterfaceComponent.IPv4))
            {
                UnicastIPAddressInformationCollection UniCast = adater.GetIPProperties().UnicastAddresses;

                if (UniCast.Count > 0)
                {

                    foreach (UnicastIPAddressInformation uni in UniCast)
                    {
                        if (uni.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return uni.Address.ToString();

                        }

                    }

                }

            }

        }

        return null;

    }
}

