using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 客户信息
/// </summary>
public class CustomerInformation
{
    /// <summary>
    /// 序号
    /// </summary>
    public int serialNumber;
    /// <summary>
    /// 区域
    /// </summary>
    public string area="";
    /// <summary>
    /// 企业名称
    /// </summary>
    public string businessName="";
    /// <summary>
    /// 地址
    /// </summary>
    public string address="";
    /// <summary>
    /// 商务负责人
    /// </summary>
    public string businessDirector="";
    /// <summary>
    /// 电话号码
    /// </summary>
    public string telephone="";
    /// <summary>
    /// 联系时间
    /// </summary>
    public string contactTimes="{}";
    /// <summary>
    /// 是否加微信
    /// </summary>
    public bool isAddWeChat =false;
    /// <summary>
    /// 是否发送案例
    /// </summary>
    public bool isSendExample =false;
    /// <summary>
    /// 拜访时间
    /// </summary>
    public string visitTime= "1970-01-01";
    /// <summary>
    /// 方案制作人
    /// </summary>
    public string producer="";
    /// <summary>
    /// 汇报
    /// </summary>
    public bool report;
    /// <summary>
    /// 签单时间
    /// </summary>
    public string signingTime= "1970-01-01";
    /// <summary>
    /// 注释
    /// </summary>
    public string remarks="";
}


public class ContactTimes
{ 
    public List<string> times;
}