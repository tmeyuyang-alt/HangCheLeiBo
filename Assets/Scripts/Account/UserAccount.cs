using System;

/// <summary>
/// 本地账号数据模型。序列化为 StreamingAssets/accounts.json。
/// </summary>
[Serializable]
public class UserAccount
{
    /// <summary>姓名</summary>
    public string name     = "";
    /// <summary>职务：管理员 / 操作员</summary>
    public string role     = "操作员";
    /// <summary>登录账号</summary>
    public string account  = "";
    /// <summary>密码（明文存储）</summary>
    public string password = "";
    /// <summary>联系方式</summary>
    public string contact  = "";
    /// <summary>创建日期 yyyy-MM-dd</summary>
    public string createDate = "";

    /// <summary>是否为管理员角色</summary>
    public bool IsAdmin => role == "管理员";

    public UserAccount() { }

    public UserAccount(string name, string role, string account,
                       string password, string contact)
    {
        this.name       = name;
        this.role       = role;
        this.account    = account;
        this.password   = password;
        this.contact    = contact;
        this.createDate = DateTime.Now.ToString("yyyy-MM-dd");
    }
}
