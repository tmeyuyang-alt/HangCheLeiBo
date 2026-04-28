using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TipsCtrl : MonoBehaviour
{
    public GameObject normal,run,error;
    public void SetNoaml()
    {
        normal.SetActive(true);
        run.SetActive(false);
        error.SetActive(false);
    }
    public void SetRun()
    {
        normal.SetActive(false);
        run.SetActive(true);
        error.SetActive(false);
    }
    public void SetError()
    {
        normal.SetActive(false);
        run.SetActive(false);
        error.SetActive(true);
    }

}
