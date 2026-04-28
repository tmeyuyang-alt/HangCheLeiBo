using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PeiLiaoPopCtrl : MonoBehaviour
{
    public static PeiLiaoPopCtrl Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    [Header("UI References")]
    [SerializeField] private GameObject dialogPanel; // 弹窗面板
    [SerializeField] private Text messageText;       // 消息文本
    [SerializeField] private Button confirmButton;   // 确定按钮
    [SerializeField] private Button cancelButton;    // 取消按钮

    public bool? choiceResult = null; // 用户选择结果

    void Start()
    {
        // 初始化隐藏弹窗
        dialogPanel.SetActive(false);

        // 绑定按钮事件
        confirmButton.onClick.AddListener(() => OnButtonClick(true));
        cancelButton.onClick.AddListener(() => OnButtonClick(false));
    }

    // 显示弹窗并启动协程等待结果
    public IEnumerator ShowDialog(string message)
    {
        print("Show-"+message);
        dialogPanel.SetActive(true);
        messageText.text = message;
        choiceResult = null; // 重置结果

        // 等待直到用户点击按钮
        while (choiceResult == null)
        {
            yield return null;
        }

        dialogPanel.SetActive(false);
    }

    // 按钮点击处理
    private void OnButtonClick(bool result)
    {
        choiceResult = result;
    }
}
