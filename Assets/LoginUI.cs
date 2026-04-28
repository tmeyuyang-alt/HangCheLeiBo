using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 登录界面（本地账号验证版）。
///
/// Inspector 配置：
///   - inputAccount   : InputField  账号输入框
///   - inputPassword  : InputField  密码输入框（可设为 Password 类型）
///   - toggleShowPwd  : Toggle      显示/隐藏密码
///   - btnLogin       : Button      登录按钮
///   - txtError       : GameObject  错误提示（默认隐藏）
///   - mainSceneName  : 登录成功后加载的场景名，默认 "1.Main2"
/// </summary>
public class LoginUI : MonoBehaviour
{
    [Header("UI 控件")]
    public InputField inputAccount;
    public InputField inputPassword;
    public Toggle     toggleShowPwd;
    public Button     btnLogin;
    public GameObject wrongInfo;        // 错误提示对象

    [Header("场景")]
    public string mainSceneName = "1.Main2";

    // ------------------------------------------------------------------

    private void Start()
    {
        if (wrongInfo != null)
            wrongInfo.SetActive(false);

        if (toggleShowPwd != null)
            toggleShowPwd.onValueChanged.AddListener(OnToggleShowPwd);

        if (btnLogin != null)
            btnLogin.onClick.AddListener(Check);
    }

    // ------------------------------------------------------------------
    // 显示 / 隐藏密码
    // ------------------------------------------------------------------

    private void OnToggleShowPwd(bool show)
    {
        if (inputPassword == null) return;
        inputPassword.contentType = show
            ? InputField.ContentType.Standard
            : InputField.ContentType.Password;
        // 刷新显示
        inputPassword.gameObject.SetActive(false);
        inputPassword.gameObject.SetActive(true);
    }

    // ------------------------------------------------------------------
    // 登录验证
    // ------------------------------------------------------------------

    public void Check()
    {
        if (wrongInfo != null)
            wrongInfo.SetActive(false);

        string account  = inputAccount  != null ? inputAccount.text.Trim()  : "";
        string password = inputPassword != null ? inputPassword.text         : "";

        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
        {
            if (wrongInfo != null) wrongInfo.SetActive(true);
            return;
        }

        bool ok = LoginManager.Instance.Login(account, password);

        if (ok)
        {
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            if (wrongInfo != null) wrongInfo.SetActive(true);
        }
    }
}
