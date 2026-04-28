using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneHelp : MonoBehaviour
{
    public string NextScene;


    public void LoadNext()
    {
        SceneManager.LoadSceneAsync(NextScene);
    }
    public void MinApplication()
    {
       // ShowWindow(GetForegroundWindow(), SW_SHOWMINIMIZED);
    }
    public void Close()
    {
        Application.Quit();
    }
}
