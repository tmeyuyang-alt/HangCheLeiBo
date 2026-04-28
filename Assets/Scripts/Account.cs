using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Account
{
    public int accountType;
    public string account;
    public string password;

    public enum AccountType
    {
        None = 0,
        Operator = 1,
        Engineer = 2,
        Administrators = 3,
    }
}
