using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UMP;
using UnityEngine;

public class CameraItem : MonoBehaviour
{
    public UniversalMediaPlayer mediaPlayer;

    private void Start()
    {
        mediaPlayer.Path = LiveCameraConfig.instance.GetPath(gameObject.name);
        
    }

    private void OnEnable()
    {
        DelayPlay();
    }

    private void OnDisable()
    {
        mediaPlayer.Stop();
    }

    public async void DelayPlay()
    {
        await Task.Delay(500);
        mediaPlayer.Play();
    }
    
}
