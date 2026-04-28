using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PointCloudDemo
{
    [Serializable] public class RootResponse { public int code; public string msg; public DataPayload data; }
    [Serializable] public class DataPayload
    {
        public string stockId;
        public int xpointNum;
        public int ypointNum;
        public List<GridPointRaw> gridPointList;
    }
    [Serializable] public class GridPointRaw { public float x; public float y; public float z; public float distance; }
    [Serializable] public class GridPoint { public float x, y, z; }

    /// <summary>不可变渲染状态（双缓冲）</summary>
    public sealed class RenderState
    {
        public readonly List<Matrix4x4[]> Batches = new List<Matrix4x4[]>();
        public readonly float MinY;
        public readonly float MaxY;

        public RenderState(List<Matrix4x4[]> batches, float minY, float maxY)
        {
            Batches = batches;
            MinY = minY;
            MaxY = maxY;
        }
    }

    public class PointCloudLoader : MonoBehaviour
    {
        [Header("API")]
        public string apiBase = "http://127.0.0.1/EDGE_SCRAPER";
        public string stockId = "1";

        [Header("Render")]
        public Mesh instanceMesh;
        public Material instanceMaterial;
        public float pointScale = 0.05f;
        public Vector3 worldOffset = Vector3.zero;

        [Header("Color & Shading")]
        [Tooltip("使用固定颜色（推荐：稳定、无色带跳变）")]
        public bool useConstantColor = true;
        public Color constantColor = new Color(0.15f, 0.7f, 1f, 1f);

        [Tooltip("关闭固定颜色后，改为按高度着色")]
        public bool colorByHeight = false;

        [Tooltip("按高度着色时，锁定色标范围以避免每帧变色")]
        public bool lockHeightRange = true;
        public float lockedMinY = 0f;
        public float lockedMaxY = 10f;

        [Header("Refresh")]
        [Tooltip("刷新间隔(秒)")] public float refreshInterval = 0.5f;
        public bool autoStart = true;

        const int kBatchSize = 1023;

        // —— 渲染双缓冲 —— //
        private RenderState _state;         // 当前用于绘制
        private RenderState _nextState;     // 后台准备好的下一份
        private bool _hasNext;

        private bool _ready;
        private bool _loading;

        private void Start()
        {
            if (autoStart)
                StartCoroutine(RefreshLoop());
        }

        private System.Collections.IEnumerator RefreshLoop()
        {
            while (true)
            {
                yield return ReloadAsync().AsIEnumerator();
                yield return new WaitForSeconds(refreshInterval);
            }
        }

        /// <summary>后台拉取 + 构建新批次，不立即替换</summary>
        public async Task ReloadAsync()
        {
            if (_loading) return;
            _loading = true;
            try
            {
                var points = await FetchGridPointsAsync(apiBase, stockId);
                var newState = BuildBatches(points);
                _nextState = newState;
                _hasNext = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PointCloud] Load failed: {ex.Message}");
            }
            _loading = false;
        }

        private void Update()
        {
            // 主线程原子交换，避免当帧空绘制
            if (_hasNext && _nextState != null)
            {
                _state = _nextState;
                _nextState = null;
                _hasNext = false;
                _ready = _state != null;

                ApplyMaterialProps(_state); // 只在交换时统一设置一次材质
#if UNITY_EDITOR
                // Debug.Log($"[PointCloud] Swapped. Batches={_state?.Batches.Count}");
#endif
            }

            if (!_ready || instanceMaterial == null || instanceMesh == null || _state == null) return;

            var bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

            var batches = _state.Batches;
            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                if (batch == null || batch.Length == 0) continue;

                Graphics.DrawMeshInstanced(
                    instanceMesh, 0, instanceMaterial, batch, batch.Length, null,
                    UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer, null,
                    UnityEngine.Rendering.LightProbeUsage.Off
                );
            }
        }

        #region HTTP + Parse
        private static async Task<List<GridPoint>> FetchGridPointsAsync(string apiBase, string stockId)
        {
            string url = $"{apiBase}/api/grid/get-grid-by-stockId?stockId={UnityWebRequest.EscapeURL(stockId)}";
            using (var req = UnityWebRequest.Get(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
                    throw new Exception(req.error);
#else
                if (req.isNetworkError || req.isHttpError)
                    throw new Exception(req.error);
#endif

                var json = req.downloadHandler.text ?? "";
                var root = JsonUtility.FromJson<RootResponse>(json);
                if (root == null || root.data == null || root.data.gridPointList == null)
                    throw new Exception("JSON parse failed");

                var list = new List<GridPoint>(root.data.gridPointList.Count);
                foreach (var p in root.data.gridPointList)
                    list.Add(new GridPoint { x = p.x, y = p.y, z = p.z });
                return list;
            }
        }
        #endregion

        #region Build Batches
        private RenderState BuildBatches(List<GridPoint> points)
        {
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;
            var batches = new List<Matrix4x4[]>();

            if (instanceMesh == null)
            {
                var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instanceMesh = temp.GetComponent<MeshFilter>().sharedMesh;
#if UNITY_EDITOR
                DestroyImmediate(temp);
#else
                Destroy(temp);
#endif
            }

            if (points == null || points.Count == 0)
                return new RenderState(batches, 0f, 0f);

            int total = points.Count;
            int batchCount = Mathf.CeilToInt(total / (float)kBatchSize);
            int idx = 0;

            for (int b = 0; b < batchCount; b++)
            {
                int len = Mathf.Min(kBatchSize, total - idx);
                var mats = new Matrix4x4[len];

                for (int i = 0; i < len; i++)
                {
                    var p = points[idx + i];

                    // 计算高度统计（这里用 p.y；如果你的高度在 z，可切换）
                    if (p.y < minY) minY = p.y;
                    if (p.y > maxY) maxY = p.y;

                    // 坐标映射（如你的高度在 z，把下行改为 new Vector3(p.x, p.z, p.y)）
                    Vector3 pos = new Vector3(p.x, p.z, p.y) + worldOffset;
                    mats[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * pointScale);
                }

                batches.Add(mats);
                idx += len;
            }

            return new RenderState(batches, minY, maxY);
        }

        /// <summary>统一设置材质：固定颜色或稳定色标</summary>
        private void ApplyMaterialProps(RenderState s)
        {
            if (instanceMaterial == null || s == null) return;

            if (useConstantColor)
            {
                // 统一固定颜色，彻底消除颜色抖动
                instanceMaterial.DisableKeyword("_COLOR_BY_HEIGHT");
                if (instanceMaterial.HasProperty("_BaseColor")) instanceMaterial.SetColor("_BaseColor", constantColor); // URP/HDRP Lit
                if (instanceMaterial.HasProperty("_Color")) instanceMaterial.SetColor("_Color", constantColor);         // Built-in/Standard
                if (instanceMaterial.HasProperty("_EmissionColor")) instanceMaterial.SetColor("_EmissionColor", Color.black);
            }
            else
            {
                if (colorByHeight)
                {
                    instanceMaterial.EnableKeyword("_COLOR_BY_HEIGHT");
                    float minY = s.MinY, maxY = s.MaxY;
                    if (lockHeightRange)
                    {
                        minY = lockedMinY;
                        maxY = lockedMaxY;
                    }
                    // 避免除零或颠倒
                    if (Mathf.Approximately(minY, maxY)) maxY = minY + 0.0001f;
                    if (maxY < minY) (minY, maxY) = (maxY, minY);

                    instanceMaterial.SetFloat("_MinY", minY);
                    instanceMaterial.SetFloat("_MaxY", maxY);
                }
                else
                {
                    instanceMaterial.DisableKeyword("_COLOR_BY_HEIGHT");
                }
            }
        }
        #endregion
    }

    public static class TaskExt
    {
        public static System.Collections.IEnumerator AsIEnumerator(this Task task)
        {
            while (!task.IsCompleted) { yield return null; }
            if (task.IsFaulted) throw task.Exception;
        }
    }
}
