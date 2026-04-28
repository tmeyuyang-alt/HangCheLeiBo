using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AccountManager : UIPanel
{
    public Dropdown dropdown;
    public InputField accountInput;
    public InputField passwordInput;

    public Button CreateBtn;
    public Button ModifiyBtn;

    public Text message;
    void Start()
    {
        CreateBtn.onClick.AddListener(CreateAccount);
        ModifiyBtn.onClick.AddListener(ModifyAccount);


        LoginHandler.getInstance.OnAccountOperateCallback += OnAccountOperateCallback;
    }


    void OnAccountOperateCallback(PackIn packin)
    {
        //TODO输出信息
        Debug.Log(packin.message);
        if (packin.code == CodeConstant.OK)
        {
            message.color = Color.green;
        }
        else
        {
            message.color = Color.red;
        }
        message.text = packin.message;
    }
    void CreateAccount()
    {
        message.text = "";

        CreateAccountDTO accountDTO = new CreateAccountDTO();

        accountDTO.account = accountInput.text;
        accountDTO.password = passwordInput.text;
        accountDTO.account_type = (3 - dropdown.value);

        SocketModel model = new SocketModel();

        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;

        model.type = Protocols.Protocol.Login;
        model.command = Protocols.LoginCommandProtocol.CREATE_ACCOUNT;
        model.area = Protocols.Protocol.Login;
        model.message = LitJson.JsonMapper.ToJson(accountDTO);
        ClientManager.getInstance.SendServer(model);
    }

    void ModifyAccount()
    {
        message.text = "";
        ModifiyAccountDTO accountDTO = new ModifiyAccountDTO();

        accountDTO.account = accountInput.text;
        accountDTO.password = passwordInput.text;
        accountDTO.account_type = (3 - dropdown.value);

        SocketModel model = new SocketModel();

        model.senderID = GlobalInfo.user.uid.ToString();
        model.token = GlobalInfo.user.token;

        model.type = Protocols.Protocol.Login;
        model.area = Protocols.Protocol.Login;
        model.command = Protocols.LoginCommandProtocol.MODIFIY_ACCOUNT;
        model.message = LitJson.JsonMapper.ToJson(accountDTO);
        ClientManager.getInstance.SendServer(model);
    }

    public override void OnClose()
    {
        base.OnClose();

        LoginHandler.getInstance.OnAccountOperateCallback -= OnAccountOperateCallback;
    }
}
