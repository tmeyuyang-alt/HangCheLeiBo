using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;          // for Text
using TMPro;                   // for TextMeshProUGUI

public class WaringDataDisplay : MonoBehaviour
{
    [Header("依赖")]
    public PLCConfigManager plcConfigManager;

    [Header("故障条目预制体")]
    public GameObject itemPrefab;   // 至少指定一个
    public GameObject itemPrefab2;  // 可选：存在时交替使用

    [Header("父容器")]
    public Transform content;       // 实例化到此容器下

    [Header("轮询设置")]
    [Tooltip("轮询故障信号的间隔（秒）")]
    public float pollInterval = 0.3f;

    [Header("分页设置")]
    [Tooltip("每页最多显示的条目数量")]
    public int pageSize = 6;
    [Tooltip("每页停留时间（秒）")]
    public float pageInterval = 5f;

    // 记录已激活的（正在显示的）故障项：key=故障名称，value=实例化的GameObject
    private readonly Dictionary<string, GameObject> _activeItems = new Dictionary<string, GameObject>();
    // 为了稳定分页顺序，维护出现顺序（或你也可改成按名称排序）
    private readonly List<string> _order = new List<string>();

    private float _pollTimer;
    private float _pageTimer;
    private int _currentPage;

    private void Awake()
    {
        if (plcConfigManager == null)
        {
            Debug.LogWarning("[WaringDataDisplay] plcConfigManager 未赋值，脚本将无法读取故障状态。");
        }
        if (content == null)
        {
            Debug.LogError("[WaringDataDisplay] 请在 Inspector 指定 content（用于挂载报警条目的父物体）。");
        }
        if (itemPrefab == null && itemPrefab2 == null)
        {
            Debug.LogError("[WaringDataDisplay] 请至少指定一个预制体（itemPrefab 或 itemPrefab2）。");
        }
        if (pageSize <= 0) pageSize = 6;
        if (pageInterval <= 0f) pageInterval = 5f;
    }

    private void Update()
    {
        // 周期性轮询
        _pollTimer += Time.deltaTime;
        if (_pollTimer >= pollInterval)
        {
            _pollTimer = 0f;
            TryRefreshOnce();
        }

        // 周期性翻页
        _pageTimer += Time.deltaTime;
        if (_pageTimer >= pageInterval)
        {
            _pageTimer = 0f;
            ShowNextPage();
        }
    }

    /// <summary>
    /// 单次刷新：遍历所有“故障信号”，按状态生成/销毁条目。
    /// </summary>
    private void TryRefreshOnce()
    {
        if (plcConfigManager == null || plcConfigManager.plcConfigs == null)
            return;

        // 本次扫描到的 Key
        var seenThisTick = HashSetPool<string>.Get();

        try
        {
            foreach (var item in plcConfigManager.plcConfigs)
            {
                string key = item.Key;
                PLCConfig cfg = item.Value;

                if (string.IsNullOrEmpty(key) || cfg == null) continue;

                string shortName = cfg.ShortName;
                bool isFaultPoint = key.Contains("故障") ||
                                    (!string.IsNullOrEmpty(shortName) && shortName.Contains("故障"));

                // 只处理 PLCConfigManager 中点位名字带“故障”的地址
                if (!isFaultPoint) continue;

                bool isFault = false;
                try
                {
                    isFault = plcConfigManager.GetBool(key);
                }
                catch (Exception e)
                {
                    // 读取出错不影响其它项
                    Debug.LogWarning($"[WaringDataDisplay] GetBool('{key}') 失败：{e.Message}");
                    continue;
                }

                seenThisTick.Add(key);
                string displayName = string.IsNullOrEmpty(shortName) ? key : shortName;
              

                if (isFault)
                {
                    if (!_activeItems.ContainsKey(key))
                    {
                        var go = Instantiate(ChoosePrefab(), content);
                        go.name = key;
                        go.SetActive(true);
                        SetFirstChildText(go, displayName);
                        _activeItems[key] = go;
                        _order.Add(key); // 记录顺序
                    }
                    else
                    {
                        // 已存在：更新文本，保持和创建时一致的展示规则
                        SetFirstChildText(_activeItems[key], displayName);
                    }
                }
                else
                {
                    // 故障恢复：销毁并移除
                    if (_activeItems.TryGetValue(key, out var go) && go != null)
                    {
                        Destroy(go);
                    }
                    if (_activeItems.Remove(key))
                    {
                        _order.Remove(key);
                    }
                }
            }

            // 清理：如果某个故障名称这次没被遍历到（比如配置被删了），也把它移除
            _toRemove.Clear();
            foreach (var kv in _activeItems)
            {
                if (!seenThisTick.Contains(kv.Key))
                {
                    if (kv.Value != null) Destroy(kv.Value);
                    _toRemove.Add(kv.Key);
                }
            }
            foreach (var k in _toRemove)
            {
                _activeItems.Remove(k);
                _order.Remove(k);
            }

            // 刷新分页可见性（当数据量变化时，确保当前页显示正确）
            RefreshPageVisibility();
        }
        finally
        {
            HashSetPool<string>.Release(seenThisTick);
        }
    }

    /// <summary>
    /// 可手动触发刷新（保留原有入口）
    /// </summary>
    public void UpdateUI()
    {
        TryRefreshOnce();
    }

    // 在两个预制体之间交替，若只设置了一个，就用那个
    private bool _toggle;
    private GameObject ChoosePrefab()
    {
        if (itemPrefab != null && itemPrefab2 != null)
        {
            _toggle = !_toggle;
            return _toggle ? itemPrefab : itemPrefab2;
        }
        return itemPrefab != null ? itemPrefab : itemPrefab2;
    }

    // 将“第一个子物体”的文本设置为故障名称，兼容 Text 和 TextMeshProUGUI
    private static void SetFirstChildText(GameObject parent, string text)
    {
        if (parent == null || parent.transform.childCount == 0) return;

        var child = parent.transform.GetChild(0);
        if (child == null) return;

        var uiText = child.GetComponent<Text>();
        if (uiText != null)
        {
            uiText.text = text;
            return;
        }

        var tmp = child.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
            return;
        }

        // 如果既不是 Text 也不是 TMP，就尝试再找一层
        var uiTextDeep = child.GetComponentInChildren<Text>(true);
        if (uiTextDeep != null) { uiTextDeep.text = text; return; }

        var tmpDeep = child.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpDeep != null) { tmpDeep.text = text; return; }

        // 最后兜底：什么组件都没有，就在控制台提示
        Debug.LogWarning($"[WaringDataDisplay] 预制体的第一个子物体未找到 Text / TextMeshProUGUI 组件。无法设置文本：{text}");
    }

    /// <summary>
    /// 翻到下一页（自动或你也可在外部按钮调用）
    /// </summary>
    public void ShowNextPage()
    {
        int count = _order.Count;
        if (count == 0) return;

        int totalPages = Mathf.CeilToInt(count / (float)pageSize);
        if (totalPages <= 0) totalPages = 1;

        _currentPage = (_currentPage + 1) % totalPages;
        ApplyPageVisibility(_currentPage);
    }

    /// <summary>
    /// 当数据变化时，根据当前页刷新可见性；
    /// 如当前页越界（删除导致总页数减少），会回落到最后一页。
    /// </summary>
    private void RefreshPageVisibility()
    {
        int count = _order.Count;
        if (count == 0)
        {
            _currentPage = 0;
            return;
        }

        int totalPages = Mathf.CeilToInt(count / (float)pageSize);
        if (totalPages <= 0) totalPages = 1;
        if (_currentPage >= totalPages) _currentPage = totalPages - 1;

        ApplyPageVisibility(_currentPage);
    }

    /// <summary>
    /// 实际控制每个条目的显隐
    /// </summary>
    private void ApplyPageVisibility(int page)
    {
        int count = _order.Count;
        int totalPages = Mathf.CeilToInt(count / (float)pageSize);
        if (totalPages <= 0) totalPages = 1;
        page = Mathf.Clamp(page, 0, totalPages - 1);

        int start = page * pageSize;
        int end = Mathf.Min(start + pageSize, count);

        // 先全部隐藏
        for (int i = 0; i < count; i++)
        {
            var key = _order[i];
            if (_activeItems.TryGetValue(key, out var go) && go != null)
                go.SetActive(false);
        }

        // 再显示当前页
        for (int i = start; i < end; i++)
        {
            var key = _order[i];
            if (_activeItems.TryGetValue(key, out var go) && go != null)
                go.SetActive(true);
        }
    }

    // —— 小工具：避免分配的 HashSet 对象池 —— //
    private static readonly List<string> _toRemove = new List<string>();

    private static class HashSetPool<T>
    {
        private static readonly Stack<HashSet<T>> Pool = new Stack<HashSet<T>>();

        public static HashSet<T> Get()
        {
            return Pool.Count > 0 ? Pool.Pop() : new HashSet<T>();
        }

        public static void Release(HashSet<T> set)
        {
            set.Clear();
            Pool.Push(set);
        }
    }
}
