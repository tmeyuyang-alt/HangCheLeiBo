using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainControl : MonoBehaviour
{

    public Transform[] chains;

    //private Vector3[] offsets;
    private Vector3 offset;

    private void Awake()
    {
        offset = transform.position;
        //offsets = new Vector3[chains.Length];
        //for (int i = 0; i < chains.Length; i++)
        //{
        //    offsets[i] =  chains[i].position - transform.position;
        //}
    }
    void Update()
    {
        if (chains != null)
        {
            for (int i = 0; i < chains.Length; i++)
            {
                float height = (transform.position - offset).y;

                chains[i].localScale = new Vector3(((4.0f - height) / 4.0f),1, 1);
                //chains[i].position = offsets[i] + transform.position;
            }
        }
    }
}
