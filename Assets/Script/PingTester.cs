// PingTester.cs —— 挂在任何场景对象上即可
// Unity 2021+ / .NET Standard 2.1

using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PingTester : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("要 Ping 的地址，可以是域名或 IP")]
    public string target = "192.168.20.1";

    [Tooltip("两次 Ping 之间的间隔 (秒)")]
    public float interval = 2f;

    [Tooltip("单次 Ping 的超时 (秒)")]
    public float timeout = 3f;

    // 最近一次结果
    private string _lastResult = "Waiting...";
    
    public GameObject OnLine,OffLine;
    
    public PLCConfigManager plcConfigManager;
    

    

    private Coroutine _routine;

    private void OnEnable()
    {
        target = DataUtil.Deserializer<string>(Application.streamingAssetsPath + "/PLCIP.config");
        _routine = StartCoroutine(PingLoop());
    }

    private void OnDisable()
    {
        if (_routine != null) StopCoroutine(_routine);
    }

    public bool isWaitForReconnect=false;
  

    public async void ReConnect()
    {
        SceneManager.LoadSceneAsync(0);
    }

    private IEnumerator PingLoop()
    {
        var waitInterval = new WaitForSeconds(interval);

        while (true)
        {
            // 1. 创建 Ping 对象
            Ping ping = new Ping(target);
            float startTime = Time.time;
           

            // 2. 轮询直到完成或超时
            while (!ping.isDone && Time.time - startTime < timeout)
                yield return null;

            bool isAllConnect=true;
            foreach (var plc in plcConfigManager.plcConnectDic)
            { 
                print(plc.Value.IsConnected().ToString());
                if (!plc.Value.IsConnected())
                {
                    isAllConnect = false;
                }
            }
            
            // 3. 记录结果
            if (ping.isDone)
            {
                _lastResult = $"Ping {target} = {ping.time} ms";
               
                if (isWaitForReconnect)
                {
                    isWaitForReconnect = false;
                    ReConnect();
                }
            }
            else
            {
                _lastResult = $"Ping {target} 失败 (timeout {timeout}s)";
                isWaitForReconnect = true;
                foreach (var plc in plcConfigManager.plcConnectDic)
                {
                    plc.Value.Close();
                }
            }
            if (isAllConnect)
            {
                OnLine.SetActive(true);
                OffLine.SetActive(true);
            }
            else
            {
                OnLine.SetActive(false);
                OffLine.SetActive(true);
            }
            //print(_lastResult);
            // 4. 主动释放原生资源
            ping.DestroyPing();
            
            // 6. 等待下一个周期
            yield return waitInterval;
        }
    }

    // Demo：直接在屏幕左上角打印
   
}