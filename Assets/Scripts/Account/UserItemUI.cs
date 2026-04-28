using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用户列表中单条记录的 UI 组件。
///
/// Prefab 结构（示例）：
///   UserItem (GameObject)           ← 根节点可挂一个 Image 作为背景（用于选中高亮）
///     ├─ txtName       Text  姓名
///     ├─ txtRole       Text  职务
///     ├─ txtAccount    Text  账号
///     ├─ txtContact    Text  联系方式
///     ├─ txtCreateDate Text  创建日期
///     └─ btnSelect     Button  选中按钮
///
/// 由 UserManagePanel 在运行时调用 Bind() 绑定数据。
/// 点击"选中"后通知 UserManagePanel，由其将用户数据加载进 AddUserPanel。
/// </summary>
public class UserItemUI : MonoBehaviour
{
    [Header("显示控件")]
    public Text txtName;
    public Text txtRole;
    public Text txtAccount;
    public Text txtContact;
    public Text txtCreateDate;

    [Header("操作")]
    public Button btnSelect;

    // 根节点背景 Image，用于选中高亮；如果 Prefab 没有单独背景 Image 可不挂
    [Header("选中高亮（可选）")]
    public Image backgroundImage;
    public Color selectedColor   = new Color(0.18f, 0.60f, 1f,  0.25f);
    public Color normalColor     = new Color(0f,    0f,    0f,  0f);

    // ------------------------------------------------------------------

    private UserManagePanel _panel;
    private UserAccount     _user;

    // ------------------------------------------------------------------

    private void Awake()
    {
        if (btnSelect != null)
            btnSelect.onClick.AddListener(OnSelect);
    }

    // ------------------------------------------------------------------
    // 绑定数据
    // ------------------------------------------------------------------

    public void Bind(UserAccount user, UserManagePanel panel)
    {
        _panel = panel;
        _user  = user;

        if (txtName       != null) txtName.text       = user.name;
        if (txtRole       != null) txtRole.text        = user.role;
        if (txtAccount    != null) txtAccount.text     = user.account;
        if (txtContact    != null) txtContact.text     = user.contact;
        if (txtCreateDate != null) txtCreateDate.text  = user.createDate;

        SetSelected(false);
    }

    // ------------------------------------------------------------------
    // 选中 / 取消选中外观
    // ------------------------------------------------------------------

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    // ------------------------------------------------------------------

    private void OnSelect()
    {
        if (_panel == null || _user == null) return;
        _panel.SelectUser(this, _user);
    }
}
