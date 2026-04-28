using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : UIPanel
{

    public Dropdown identity;
    public InputField accountInput;
    public InputField passwordInput;

    public Button loginBtn;
    public Text hintText;
    void Start()
    {
       
        RequestManager.RequestPublicKey((key) => {
            //Debug.Log(key);
            GlobalInfo.publicKey = key;
        });
        loginBtn.onClick.AddListener(() =>{

            if (GlobalInfo.publicKey == "" || GlobalInfo.publicKey == null)
            {
                RequestManager.RequestPublicKey((key) =>
                {
                    GlobalInfo.publicKey = key;
                    Login();
                });
            }
            else
            {
                Login();
            }
        });
    }


    public void Login()
    {
        //Account acc = Sql_Connect.Instance.Login(accountInput.text, passwordInput.text);
        int type = 3 - identity.value;
        string passWord = string.Empty;
        
        if(GlobalInfo.publicKey!=string.Empty)
        passWord = RSAUtil.Encrypt(GlobalInfo.publicKey, passwordInput.text);

        var dto = new LoginDTO() { userName = accountInput.text, passWord = passWord, account_type = type };

        RequestManager.Login(dto, (packtin) =>
        {
            //Debug.Log(packtin.message);

            GlobalInfo.user = SerializeTool.Decode<UserDTO>(packtin.message);

            if (packtin.code == CodeConstant.OK)
            {
                hintText.text = "登录成功！";
                hintText.gameObject.SetActive(true);
                StartCoroutine(DelayHidden(hintText.gameObject, 1.0f));
                //DataCenter.Instance.ConnectStaticConfigPlc();
                //DataCenter.Instance.GetOtherPlcConfig();
                AppManager.Instance.RequestOtherPlcConfig();
                this.gameObject.SetActive(false);
                //进入场景加载UI
                UIManager.Instance.OpenPanel<MainPanel>(null);
                UIManager.Instance.OpenPanel<TitlePanel>(null, "Top");


            }
            else
            {
                hintText.text = "登录失败！";
                hintText.gameObject.SetActive(true);
                StartCoroutine(DelayHidden(hintText.gameObject, 1.0f));
            }
        });
    }
    IEnumerator DelayHidden(GameObject go, float time)
    {
        yield return new WaitForSeconds(time);
        go.SetActive(false);
    }

    public int IdTypeStrToInt(string str)
    {
        switch (str)
        {
            case "操作员":
                return 1;

            case "工程师":
                return 2;

            case "超级管理员":
                return 3;
        }

        return 0;
    }
}
