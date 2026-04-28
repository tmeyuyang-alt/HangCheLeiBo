using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class SocketModel
{
    public string senderID =string.Empty;

    public int type = -1;

    public int area = -1;

    public int command = -1;

    public string message = string.Empty;

    public string token = string.Empty;

    public SocketModel(){}
    
    public SocketModel(int type, int area, int cmd, string msg)
    {
        this.type = type;
        this.area = area;
        this.command = cmd;
        this.message = msg;
    }

}