using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用户新增 / 编辑面板。
///
/// 两种模式：
///   ① 新增模式（_editingAccount == null）：表单为空，只有"新增"按钮可用。
///   ② 编辑模式（_editingAccount 已设置）  ：表单填入所选用户数据，"保存"和"删除"可用。
///
/// Inspector 配置：
///   inputName      InputField  姓名
///   inputAccount   InputField  账号（编辑模式下只读）
///   inputPassword  InputField  密码（编辑模式下留空表示不修改原密码）
///   inputContact   InputField  联系方式
///   dropdownRole   Dropdown    职务（0=管理员 / 1=操作员）
///   txtCreateDate  Text        创建日期（自动生成，只读展示）
///
///   btnAdd         Button      新增 —— 总是可点击；若在编辑模式则先切回新增模式再执行
///   btnSave        Button      保存 —— 仅在编辑模式下可用，将当前表单写入已有账号
///   btnDelete      Button      删除 —— 仅在编辑模式下可用，删除当前所选账号
///
///   txtMessage     Text        操作结果提示文字
///
/// 由 UserManagePanel 持有引用，并在 Awake 中调用 Init(panel)。
/// </summary>
public class AddUserPanel : MonoBehaviour
{
    [Header("输入控件")]
    public InputField inputName;
    public InputField inputAccount;
    public InputField inputPassword;
    public InputField inputContact;
    public Dropdown   dropdownRole;
    public Text       txtCreateDate;

    [Header("三个操作按钮")]
    public Button btnAdd;
    public Button btnSave;
    public Button btnDelete;

    [Header("提示文字")]
    public Text txtMessage;

    // ------------------------------------------------------------------
    // 内部状态
    // ------------------------------------------------------------------

    private string           _editingAccount;   // null = 新增模式
    private UserManagePanel  _ownerPanel;
    private string           _editingOrigDate;  // 编辑时保留原始创建日期

    // ------------------------------------------------------------------
    // 初始化
    // ------------------------------------------------------------------

    private void Awake()
    {
        if (btnAdd    != null) btnAdd.onClick.AddListener(OnAdd);
        if (btnSave   != null) btnSave.onClick.AddListener(OnSave);
        if (btnDelete != null) btnDelete.onClick.AddListener(OnDelete);

        // 初始化角色下拉菜单（防止 Inspector 未配置）
        if (dropdownRole != null && dropdownRole.options.Count == 0)
        {
            dropdownRole.options.Add(new Dropdown.OptionData("管理员"));
            dropdownRole.options.Add(new Dropdown.OptionData("操作员"));
        }

        SetNewMode();
    }

    /// <summary>由 UserManagePanel 在 Awake 中调用，传入父面板引用。</summary>
    public void Init(UserManagePanel panel)
    {
        _ownerPanel = panel;
    }

    // ------------------------------------------------------------------
    // 公开接口：由 UserManagePanel 调用
    // ------------------------------------------------------------------

    /// <summary>将表单切换到新增模式（清空输入、禁用保存/删除）。</summary>
    public void SetNewMode()
    {
        _editingAccount  = null;
        _editingOrigDate = "";
        ClearInputs();
        RefreshDate();
        UpdateButtonStates();
        SetMessage("", Color.white, false);
    }

    /// <summary>将表单切换到编辑模式，并填入所选用户的数据。</summary>
    public void LoadUser(UserAccount user)
    {
        if (user == null) { SetNewMode(); return; }

        _editingAccount  = user.account;
        _editingOrigDate = user.createDate;

        if (inputName     != null) inputName.text     = user.name;
        if (inputAccount  != null)
        {
            inputAccount.text         = user.account;
            inputAccount.interactable = false;   // 账号作为主键不可修改
        }
        if (inputPassword != null) inputPassword.text = "";   // 留空 = 不改密码
        if (inputContact  != null) inputContact.text  = user.contact;
        if (txtCreateDate != null) txtCreateDate.text = user.createDate;

        // 同步 Dropdown
        if (dropdownRole != null)
        {
            int idx = dropdownRole.options.FindIndex(
                o => o.text == user.role);
            dropdownRole.value = idx >= 0 ? idx : 1;
            dropdownRole.RefreshShownValue();
        }

        UpdateButtonStates();
        SetMessage("", Color.white, false);
    }

    // ------------------------------------------------------------------
    // 按钮回调
    // ------------------------------------------------------------------

    /// <summary>新增 —— 若当前已在编辑模式，先切回新增模式。</summary>
    private void OnAdd()
    {
        if (_editingAccount != null)
        {
            // 当前在编辑模式，切回新增模式，清空表单
            SetNewMode();
            _ownerPanel?.ClearSelection();
            return;
        }

        // 新增模式下，执行创建
        string name     = inputName     != null ? inputName.text.Trim()    : "";
        string account  = inputAccount  != null ? inputAccount.text.Trim() : "";
        string password = inputPassword != null ? inputPassword.text        : "";
        string contact  = inputContact  != null ? inputContact.text.Trim() : "";
        string role     = GetSelectedRole();

        UserAccount user = new UserAccount(name, role, account, password, contact);
        user.createDate = DateTime.Now.ToString("yyyy-MM-dd");

        var (ok, error) = LoginManager.Instance.AddUser(user);

        if (ok)
        {
            SetMessage("新增成功！", Color.green, true);
            ClearInputs();
            RefreshDate();
            _ownerPanel?.RefreshList();
        }
        else
        {
            SetMessage(error, Color.red, true);
        }
    }

    /// <summary>保存 —— 仅在编辑模式下执行，更新已有用户信息。</summary>
    private void OnSave()
    {
        if (_editingAccount == null) return;

        string name     = inputName     != null ? inputName.text.Trim()    : "";
        string password = inputPassword != null ? inputPassword.text        : "";
        string contact  = inputContact  != null ? inputContact.text.Trim() : "";
        string role     = GetSelectedRole();

        UserAccount updated = new UserAccount(name, role, _editingAccount, password, contact);
        updated.createDate = _editingOrigDate;

        var (ok, error) = LoginManager.Instance.UpdateUser(updated);

        if (ok)
        {
            SetMessage("保存成功！", Color.green, true);
            // 刷新列表，但保持编辑模式（用最新数据重载）
            _ownerPanel?.RefreshList();
            // 重新加载一次避免密码占位符混乱
            if (inputPassword != null) inputPassword.text = "";
        }
        else
        {
            SetMessage(error, Color.red, true);
        }
    }

    /// <summary>删除 —— 仅在编辑模式下执行，删除当前所选用户。</summary>
    private void OnDelete()
    {
        if (_editingAccount == null) return;

        // 禁止删除自己
        if (LoginManager.Instance.CurrentUser != null
            && LoginManager.Instance.CurrentUser.account == _editingAccount)
        {
            SetMessage("不能删除当前登录账号", Color.red, true);
            return;
        }

        bool deleted = LoginManager.Instance.DeleteUser(_editingAccount);

        if (deleted)
        {
            SetMessage("删除成功", Color.green, true);
            _ownerPanel?.ClearSelection();
            _ownerPanel?.RefreshList();
            SetNewMode();
        }
        else
        {
            SetMessage("删除失败", Color.red, true);
        }
    }

    // ------------------------------------------------------------------
    // 辅助
    // ------------------------------------------------------------------

    private void UpdateButtonStates()
    {
        bool isEditMode = _editingAccount != null;

        if (btnSave   != null) btnSave.interactable   = isEditMode;
        if (btnDelete != null) btnDelete.interactable = isEditMode;

        // "新增"按钮文字随模式变化：编辑模式下显示"取消编辑"语义
        if (btnAdd != null)
        {
            Text lbl = btnAdd.GetComponentInChildren<Text>();
            if (lbl != null)
                lbl.text = isEditMode ? "新增（清空）" : "新增";
        }

        // 账号输入框：新增模式可编辑，编辑模式锁定
        if (inputAccount != null)
            inputAccount.interactable = !isEditMode;
    }

    private string GetSelectedRole()
    {
        if (dropdownRole == null) return "操作员";
        int idx = dropdownRole.value;
        if (idx >= 0 && idx < dropdownRole.options.Count)
            return dropdownRole.options[idx].text;
        return "操作员";
    }

    private void ClearInputs()
    {
        if (inputName     != null) inputName.text     = "";
        if (inputAccount  != null) { inputAccount.text = ""; inputAccount.interactable = true; }
        if (inputPassword != null) inputPassword.text = "";
        if (inputContact  != null) inputContact.text  = "";
        if (dropdownRole  != null) { dropdownRole.value = 1; dropdownRole.RefreshShownValue(); }
    }

    private void RefreshDate()
    {
        if (txtCreateDate != null)
            txtCreateDate.text = DateTime.Now.ToString("yyyy-MM-dd");
    }

    private void SetMessage(string msg, Color col, bool visible)
    {
        if (txtMessage == null) return;
        txtMessage.text  = msg;
        txtMessage.color = col;
        txtMessage.gameObject.SetActive(visible);
    }
}
