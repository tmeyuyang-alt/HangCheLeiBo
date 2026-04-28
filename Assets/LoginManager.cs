using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 账号管理单例。
/// - 在场景切换时保持存活（DontDestroyOnLoad）
/// - 从 StreamingAssets/accounts.json 读写用户列表
/// - CurrentUser 记录当前已登录的用户，其他脚本通过 LoginManager.Instance.CurrentUser 获取
/// </summary>
public class LoginManager : MonoBehaviour
{
    // ------------------------------------------------------------------
    // 单例
    // ------------------------------------------------------------------
    public static LoginManager Instance { get; private set; }

    // ------------------------------------------------------------------
    // 当前登录用户（登录成功后赋值，登出后清空）
    // ------------------------------------------------------------------
    public UserAccount CurrentUser { get; private set; }

    /// <summary>是否已登录</summary>
    public bool IsLoggedIn => CurrentUser != null;

    /// <summary>当前用户是否为管理员</summary>
    public bool IsAdmin => CurrentUser != null && CurrentUser.IsAdmin;

    // ------------------------------------------------------------------
    // 用户列表
    // ------------------------------------------------------------------
    private List<UserAccount> _users = new List<UserAccount>();

    private static readonly string AccountsFileName = "accounts.json";
    private string AccountsFilePath =>
        Path.Combine(Application.streamingAssetsPath, AccountsFileName);

    // ------------------------------------------------------------------
    // 生命周期
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadUsers();
    }

    // ------------------------------------------------------------------
    // 文件读写
    // ------------------------------------------------------------------

    private void LoadUsers()
    {
        if (!File.Exists(AccountsFilePath))
        {
            // 首次运行：创建默认管理员账号
            _users = new List<UserAccount>
            {
                new UserAccount("系统管理员", "管理员", "admin", "admin123", "")
            };
            SaveUsers();
            Debug.Log("[LoginManager] 首次运行，已创建默认管理员账号 admin / admin123");
            return;
        }

        try
        {
            string json = File.ReadAllText(AccountsFilePath, Encoding.UTF8);
            _users = JsonConvert.DeserializeObject<List<UserAccount>>(json)
                     ?? new List<UserAccount>();
            Debug.Log($"[LoginManager] 加载 {_users.Count} 个账号");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] 读取账号文件失败: {ex.Message}");
            _users = new List<UserAccount>();
        }
    }

    public void SaveUsers()
    {
        try
        {
            string json = JsonConvert.SerializeObject(_users, Formatting.Indented);
            File.WriteAllText(AccountsFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LoginManager] 保存账号文件失败: {ex.Message}");
        }
    }

    // ------------------------------------------------------------------
    // 账号操作
    // ------------------------------------------------------------------

    /// <summary>
    /// 登录验证。成功返回 true 并设置 CurrentUser，失败返回 false。
    /// </summary>
    public bool Login(string account, string password)
    {
        UserAccount user = _users.Find(
            u => u.account == account && u.password == password);

        if (user == null) return false;

        CurrentUser = user;
        Debug.Log($"[LoginManager] 登录成功: {user.name}（{user.role}）");
        return true;
    }

    /// <summary>登出，清空 CurrentUser。</summary>
    public void Logout()
    {
        Debug.Log($"[LoginManager] 登出: {CurrentUser?.name}");
        CurrentUser = null;
    }

    /// <summary>
    /// 新增用户。账号不能重复。
    /// 返回 (成功, 错误信息)。
    /// </summary>
    public (bool ok, string error) AddUser(UserAccount user)
    {
        if (string.IsNullOrWhiteSpace(user.account))
            return (false, "账号不能为空");
        if (string.IsNullOrWhiteSpace(user.password))
            return (false, "密码不能为空");
        if (string.IsNullOrWhiteSpace(user.name))
            return (false, "姓名不能为空");

        if (_users.Exists(u => u.account == user.account))
            return (false, $"账号 "+user.account+" 已存在");

        if (string.IsNullOrWhiteSpace(user.createDate))
            user.createDate = DateTime.Now.ToString("yyyy-MM-dd");

        _users.Add(user);
        SaveUsers();
        return (true, "");
    }

    /// <summary>删除指定账号的用户。</summary>
    public bool DeleteUser(string account)
    {
        int removed = _users.RemoveAll(u => u.account == account);
        if (removed > 0) SaveUsers();
        return removed > 0;
    }

    /// <summary>
    /// 修改密码。
    /// </summary>
    public bool ChangePassword(string account, string newPassword)
    {
        UserAccount user = _users.Find(u => u.account == account);
        if (user == null) return false;
        user.password = newPassword;
        SaveUsers();
        return true;
    }

    /// <summary>获取所有用户的只读副本。</summary>
    public List<UserAccount> GetAllUsers()
    {
        return new List<UserAccount>(_users);
    }

    /// <summary>根据账号查找用户。</summary>
    public UserAccount FindUser(string account)
    {
        return _users.Find(u => u.account == account);
    }

    /// <summary>
    /// 更新已有用户的信息（账号不可变，作为主键）。
    /// 若新密码为空则保留原密码。
    /// 若被修改的是当前登录用户，同步更新 CurrentUser。
    /// </summary>
    public (bool ok, string error) UpdateUser(UserAccount updated)
    {
        if (string.IsNullOrWhiteSpace(updated.account))
            return (false, "账号不能为空");
        if (string.IsNullOrWhiteSpace(updated.name))
            return (false, "姓名不能为空");

        int idx = _users.FindIndex(u => u.account == updated.account);
        if (idx < 0) return (false, "用户不存在");

        // 密码为空时保留旧密码
        if (string.IsNullOrWhiteSpace(updated.password))
            updated.password = _users[idx].password;

        // 保留原始创建日期
        if (string.IsNullOrWhiteSpace(updated.createDate))
            updated.createDate = _users[idx].createDate;

        _users[idx] = updated;

        // 同步当前登录用户信息
        if (CurrentUser != null && CurrentUser.account == updated.account)
            CurrentUser = updated;

        SaveUsers();
        return (true, "");
    }

    // ------------------------------------------------------------------
    // 兼容旧代码的属性（isAdmin）
    // ------------------------------------------------------------------
    /// <summary>兼容旧脚本 LoginManager.Instance.isAdmin</summary>
    public bool isAdmin => IsAdmin;
}
