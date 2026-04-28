using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用户管理面板。
///
/// 布局说明：
///   左侧滚动列表  ——  每行一个 UserItemUI Prefab，点击"选中"后右侧表单加载该用户。
///   右侧表单     ——  AddUserPanel（新增 / 保存 / 删除三按钮，始终可见）。
///
/// Inspector 配置：
///   scrollContent   Transform    ScrollView 的 Content（挂 VerticalLayoutGroup + ContentSizeFitter）
///   userItemPrefab  GameObject   UserItemUI Prefab
///   addUserPanel    AddUserPanel 右侧编辑表单（直接拖脚本引用，不是 GameObject）
///   btnClose        Button       关闭本面板
///   txtEmpty        Text         列表为空时的提示文字
/// </summary>
public class UserManagePanel : MonoBehaviour
{
    [Header("用户列表")]
    public Transform  scrollContent;
    public GameObject userItemPrefab;

    [Header("编辑表单")]
    public AddUserPanel addUserPanel;

    [Header("按钮 & 提示")]
    public Button btnClose;
    public Text   txtEmpty;

    // ------------------------------------------------------------------

    private readonly List<GameObject> _items       = new List<GameObject>();
    private          UserItemUI       _selectedItem = null;

    // ------------------------------------------------------------------

    private void Awake()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        // 将自身引用传给 AddUserPanel，使其操作后能回调刷新列表
        if (addUserPanel != null)
            addUserPanel.Init(this);
    }

    private void OnEnable()
    {
        _selectedItem = null;
        addUserPanel?.SetNewMode();
        RefreshList();
    }

    // ------------------------------------------------------------------
    // 列表刷新
    // ------------------------------------------------------------------

    /// <summary>清空并重建所有用户条目。AddUserPanel 操作成功后会调用此方法。</summary>
    public void RefreshList()
    {
        foreach (var go in _items)
        {
            if (go != null) Destroy(go);
        }
        _items.Clear();

        if (LoginManager.Instance == null || scrollContent == null || userItemPrefab == null)
            return;

        List<UserAccount> users = LoginManager.Instance.GetAllUsers();
        bool isEmpty = users == null || users.Count == 0;

        if (txtEmpty != null) txtEmpty.gameObject.SetActive(isEmpty);
        if (isEmpty) return;

        foreach (UserAccount user in users)
        {
            GameObject item = Instantiate(userItemPrefab, scrollContent);
            item.SetActive(true);

            UserItemUI ui = item.GetComponent<UserItemUI>();
            if (ui != null) ui.Bind(user, this);

            _items.Add(item);
        }
    }

    // ------------------------------------------------------------------
    // 选中逻辑（由 UserItemUI 调用）
    // ------------------------------------------------------------------

    /// <summary>
    /// 点击某行"选中"按钮后由 UserItemUI 调用。
    /// 切换选中状态并将用户信息加载进 AddUserPanel。
    /// </summary>
    public void SelectUser(UserItemUI item, UserAccount user)
    {
        // 再次点击同一行 → 取消选中，切回新增模式
        if (_selectedItem == item)
        {
            ClearSelection();
            return;
        }

        // 取消旧选中项
        if (_selectedItem != null)
            _selectedItem.SetSelected(false);

        // 高亮新选中项
        _selectedItem = item;
        item.SetSelected(true);

        // 加载用户数据到表单
        addUserPanel?.LoadUser(user);
    }

    /// <summary>取消当前选中并让 AddUserPanel 回到新增模式。</summary>
    public void ClearSelection()
    {
        if (_selectedItem != null)
        {
            _selectedItem.SetSelected(false);
            _selectedItem = null;
        }
        addUserPanel?.SetNewMode();
    }
}
