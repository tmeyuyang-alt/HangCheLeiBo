using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiaoControl : MonoBehaviour
{
    public bool OutLiao = false;
    public bool CollectLiao = false;

    public float CollectLiaoControl = 1f;
    public float backmoveVaule=0f;

    private float _move = 0;
    private float backmove = 0;
    public float speed = 0.01f;

    private float t = 0;
    private Renderer _renderer = null;
    public float move
    {
        get { return _move; }
        set
        {
            _move = value;
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
            _renderer.material.SetFloat("_Fill", move);
        }
    }

   
    void Start()
    {
        backmove = backmoveVaule;
    }

    public void StartLiao()
    {
        if (!OutLiao)
        {
            OutLiao = true;
            CollectLiao = false;
            move = 0;
            _renderer.material.SetFloat("_Fill", move);
            backmove = 0;
            _renderer.material.SetFloat("_FillBack", backmove);
        }
    }
    public void Stop()
    {
        if (OutLiao)
        {
            OutLiao = false;
            CollectLiao = true;
        }
    }

    public void Update()
    {
       
        if (OutLiao)
        {
            t += Time.deltaTime;
            move += Time.deltaTime * speed;
            if (t >3600f)
            {
                move = CollectLiaoControl;
                t = 0f;
            }
            _renderer.material.SetFloat("_Fill", move);
         

        }
        if (CollectLiao)
        {
            OutLiao = false;
            move += Time.deltaTime * speed;
            backmove += Time.deltaTime * speed;
            _renderer.material.SetFloat("_Fill", move);
            _renderer.material.SetFloat("_FillBack", backmove);
             
            if (backmove > CollectLiaoControl)
            {
                RestLiao();
                CollectLiao = false;
            }
        }
    }

    public void RestLiao()
    {
        GetComponent<Renderer>().material.SetFloat("_Fill", 0);
        GetComponent<Renderer>().material.SetFloat("_FillBack", backmoveVaule);

        _move = 0;
        backmove = backmoveVaule;
       // Debug.Log("sdfsdfs");
    }
}
