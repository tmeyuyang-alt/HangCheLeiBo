using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class RSAUtil
{
    //public void Start()
    //{
    //    //string str_Public_Key;
    //    //string str_Private_Key;
    //    //string outStr = RSA_Encrypt("你好呀", out str_Public_Key,out str_Private_Key);

    //    //Debug.Log(outStr);
    //    //Debug.Log("公钥"+str_Public_Key);
    //    //Debug.Log("私钥"+str_Private_Key);
    //    //Main();

    //    string str_Public_Key;
    //    string str_Private_Key;

    //    GetKeyPair1(out str_Public_Key, out str_Private_Key);

    //    Debug.Log("公钥:" + str_Public_Key);
    //    Debug.Log("私钥:" + str_Private_Key);

    //    string password = "这是一个密码，你需要加密";

    //    string eText = Encrypt(str_Public_Key, password);

    //    Debug.Log("密文:" + eText);

    //    Debug.Log("解密:" + Decrypt(str_Private_Key, eText));
    //}

    /// <summary>
    /// 生成一对公钥和私钥
    /// </summary>
    /// <returns></returns>
    public static void GetKeyPair1(out string str_Public_Key, out string str_Private_Key)
    {
        RSACryptoServiceProvider rsaKeyGenerator = new RSACryptoServiceProvider(1024);
        str_Public_Key = rsaKeyGenerator.ToXmlString(false);
        str_Private_Key = rsaKeyGenerator.ToXmlString(true);
    }

    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="privatekey"></param>
    /// <param name="byEncrypted"></param>
    /// <returns></returns>
    public static byte[] Decrypt(string privatekey, byte[] byEncrypted)
    {
        RSACryptoServiceProvider rsaToDecrypt = new RSACryptoServiceProvider();
        rsaToDecrypt.FromXmlString(privatekey);
        byte[] byDecrypted = rsaToDecrypt.Decrypt(byEncrypted, false);
        return byDecrypted;
    }
    /// <summary>
    /// 解密
    /// </summary>
    /// <param name="privatekey"></param>
    /// <param name="byEncrypted"></param>
    /// <returns></returns>
    public static string Decrypt(string privatekey, string base64)
    {
        byte[] byEncrypted = Convert.FromBase64String(base64);

        RSACryptoServiceProvider rsaToDecrypt = new RSACryptoServiceProvider();

        rsaToDecrypt.FromXmlString(privatekey);
        byte[] byDecrypted = rsaToDecrypt.Decrypt(byEncrypted, false);
        UTF8Encoding utf8encoder = new UTF8Encoding();
        string str_Plain_Text = utf8encoder.GetString(byDecrypted);
        return str_Plain_Text;
    }
    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="publickey"></param>
    /// <param name="Plain_Text"></param>
    /// <returns></returns>
    public static string Encrypt(string publickey, string Plain_Text)
    {
        UTF8Encoding utf8encoder = new UTF8Encoding();

        RSACryptoServiceProvider rsaToEncrypt = new RSACryptoServiceProvider();
        rsaToEncrypt.FromXmlString(publickey);

        byte[] byEncrypted = rsaToEncrypt.Encrypt(utf8encoder.GetBytes(Plain_Text), false);

        //使用Base64
       return Convert.ToBase64String(byEncrypted);
    }
}
