using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
public class UserDTO
{
    public int uid = -1;
    public string realName;//真实名称
    /// <summary>
    /// 权限级别
    /// 注：权限等级1可删可改可上传 权限权限等级2可删可上次自己的数据
    /// </summary>
    public int permission;
    public string token = string.Empty;
}