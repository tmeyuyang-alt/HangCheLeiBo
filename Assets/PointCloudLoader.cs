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
        public readonly List<Color> BatchColors = new List<Color>();
        public readonly float MinY;
        public readonly float MaxY;

        public RenderState(List<Matrix4x4[]> batches, float minY, float maxY, List<Color> batchColors = null)
        {
            Batches = batches;
            if (batchColors != null)
            {
                BatchColors = batchColors;
            }
            MinY = minY;
            MaxY = maxY;
        }

        public bool TryGetBatchColor(int index, out Color color)
        {
            if (BatchColors != null && index >= 0 && index < BatchColors.Count)
            {
                color = BatchColors[index];
                return true;
            }

            color = default(Color);
            return false;
        }
    }

    public class PointCloudLoader : MonoBehaviour
    {
        [Header("API")]
        public string apiBase = "http://127.0.0.1/EDGE_SCRAPER";
        public string stockId = "1";

        [Header("Simulation")]
        public bool useSimulatedData = false;
        public float simulatedLength = 97f;
        public float simulatedWidth = 20f;
        [Tooltip("模拟点云的点间距，单位：米。数值越小点越密。")]
        public float simulatedPointSpacing = 0.5f;
        public float simulatedBaseHeight = 0f;
        [Tooltip("连续坡面的细节起伏强度，不是逐点随机。")]
        public float simulatedHeightNoise = 0.15f;
        [Tooltip("随机生成的坡面数量。")]
        public int simulatedSlopeCount = 8;
        [Tooltip("单个坡面的最大高度差。")]
        public float simulatedMoundHeight = 1.2f;
        public int simulatedNoiseSeed = 0;

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
        public Color heightLowColor = new Color(0.1f, 0.35f, 1f, 1f);
        public Color heightHighColor = new Color(1f, 0.65f, 0.05f, 1f);
        [Range(2, 32)]
        public int heightColorBands = 12;

        [Tooltip("按高度着色时，锁定色标范围以避免每帧变色")]
        public bool lockHeightRange = true;
        public float lockedMinY = 0f;
        public float lockedMaxY = 10f;

        [Header("Refresh")]
        [Tooltip("刷新间隔(秒)")] public float refreshInterval = 0.5f;
        public bool autoStart = true;

        const int kBatchSize = 1023;

        private struct SimulatedSlopeFeature
        {
            public Vector2 Center;
            public Vector2 Radius;
            public float Height;
        }

        // —— 渲染双缓冲 —— //
        private RenderState _state;         // 当前用于绘制
        private RenderState _nextState;     // 后台准备好的下一份
        private bool _hasNext;
        private MaterialPropertyBlock _propertyBlock;

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
                var points = useSimulatedData
                    ? GenerateSimulatedGridPoints()
                    : await FetchGridPointsAsync(apiBase, stockId);
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

            ApplyMaterialProps(_state);
            var bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

            var batches = _state.Batches;
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                if (batch == null || batch.Length == 0) continue;

                MaterialPropertyBlock props = null;
                if (_state.TryGetBatchColor(i, out Color batchColor))
                {
                    _propertyBlock.Clear();
                    SetMaterialColor(_propertyBlock, batchColor);
                    props = _propertyBlock;
                }

                Graphics.DrawMeshInstanced(
                    instanceMesh, 0, instanceMaterial, batch, batch.Length, props,
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

        #region Simulation
        [ContextMenu("Generate Simulated Point Cloud")]
        public void GenerateSimulatedPointCloud()
        {
            var points = GenerateSimulatedGridPoints();
            _state = BuildBatches(points);
            _nextState = null;
            _hasNext = false;
            _ready = _state != null;
            ApplyMaterialProps(_state);
        }

        private List<GridPoint> GenerateSimulatedGridPoints()
        {
            float length = Mathf.Max(0.01f, simulatedLength);
            float width = Mathf.Max(0.01f, simulatedWidth);
            float spacing = Mathf.Max(0.01f, simulatedPointSpacing);

            int xCount = Mathf.Max(2, Mathf.FloorToInt(length / spacing) + 1);
            int yCount = Mathf.Max(2, Mathf.FloorToInt(width / spacing) + 1);
            var points = new List<GridPoint>(xCount * yCount);

            float xStep = length / (xCount - 1);
            float yStep = width / (yCount - 1);
            float seedOffset = simulatedNoiseSeed * 0.137f;
            List<SimulatedSlopeFeature> slopeFeatures = BuildSimulatedSlopeFeatures(length, width);

            for (int yIndex = 0; yIndex < yCount; yIndex++)
            {
                float y = yIndex * yStep;
                float normalizedY = width <= 0f ? 0f : y / width;

                for (int xIndex = 0; xIndex < xCount; xIndex++)
                {
                    float x = xIndex * xStep;
                    float normalizedX = length <= 0f ? 0f : x / length;
                    float z = simulatedBaseHeight
                        + GetSimulatedSlopeHeight(x, y, slopeFeatures)
                        + GetSimulatedNoiseHeight(normalizedX, normalizedY, seedOffset);

                    points.Add(new GridPoint
                    {
                        x = x,
                        y = y,
                        z = z
                    });
                }
            }

            return points;
        }

        private List<SimulatedSlopeFeature> BuildSimulatedSlopeFeatures(float length, float width)
        {
            int featureCount = Mathf.Max(1, simulatedSlopeCount);
            var features = new List<SimulatedSlopeFeature>(featureCount);
            var random = new System.Random(simulatedNoiseSeed);

            for (int i = 0; i < featureCount; i++)
            {
                float centerX = RandomRange(random, 0f, length);
                float centerY = RandomRange(random, 0f, width);
                float radiusX = RandomRange(random, length * 0.08f, length * 0.22f);
                float radiusY = RandomRange(random, width * 0.25f, width * 0.75f);
                float height = RandomRange(random, simulatedMoundHeight * 0.35f, simulatedMoundHeight);

                if (i % 3 == 2)
                {
                    height *= -0.45f;
                }

                features.Add(new SimulatedSlopeFeature
                {
                    Center = new Vector2(centerX, centerY),
                    Radius = new Vector2(Mathf.Max(0.01f, radiusX), Mathf.Max(0.01f, radiusY)),
                    Height = height
                });
            }

            return features;
        }

        private float GetSimulatedSlopeHeight(float x, float y, List<SimulatedSlopeFeature> slopeFeatures)
        {
            if (slopeFeatures == null || slopeFeatures.Count == 0 || Mathf.Approximately(simulatedMoundHeight, 0f))
            {
                return 0f;
            }

            float height = 0f;
            for (int i = 0; i < slopeFeatures.Count; i++)
            {
                SimulatedSlopeFeature feature = slopeFeatures[i];
                float dx = (x - feature.Center.x) / feature.Radius.x;
                float dy = (y - feature.Center.y) / feature.Radius.y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance >= 1f)
                {
                    continue;
                }

                float influence = 1f - SmoothStep01(distance);
                height += feature.Height * influence;
            }

            return height;
        }

        private float RandomRange(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private float SmoothStep01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private float GetSimulatedNoiseHeight(float normalizedX, float normalizedY, float seedOffset)
        {
            if (Mathf.Approximately(simulatedHeightNoise, 0f))
            {
                return 0f;
            }

            float noise = Mathf.PerlinNoise(normalizedX * 5f + seedOffset, normalizedY * 5f + seedOffset);
            return (noise - 0.5f) * simulatedHeightNoise;
        }
        #endregion

        #region Build Batches
        private RenderState BuildBatches(List<GridPoint> points)
        {
            var batches = new List<Matrix4x4[]>();
            var batchColors = new List<Color>();

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

            CalculateHeightRange(points, out float minY, out float maxY);

            if (ShouldUseHeightColors())
            {
                BuildHeightColorBatches(points, minY, maxY, batches, batchColors);
            }
            else
            {
                BuildPlainBatches(points, batches);
            }

            return new RenderState(batches, minY, maxY, batchColors);
        }

        private void CalculateHeightRange(List<GridPoint> points, out float minY, out float maxY)
        {
            minY = float.PositiveInfinity;
            maxY = float.NegativeInfinity;

            for (int i = 0; i < points.Count; i++)
            {
                float height = points[i].z;
                if (height < minY) minY = height;
                if (height > maxY) maxY = height;
            }

            if (float.IsInfinity(minY) || float.IsInfinity(maxY))
            {
                minY = 0f;
                maxY = 0f;
            }
        }

        private bool ShouldUseHeightColors()
        {
            return !useConstantColor && colorByHeight;
        }

        private void BuildPlainBatches(List<GridPoint> points, List<Matrix4x4[]> batches)
        {
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
                    mats[i] = Matrix4x4.TRS(GetRenderPosition(p), Quaternion.identity, Vector3.one * pointScale);
                }

                batches.Add(mats);
                idx += len;
            }
        }

        private void BuildHeightColorBatches(List<GridPoint> points, float stateMinY, float stateMaxY, List<Matrix4x4[]> batches, List<Color> batchColors)
        {
            ResolveHeightRange(stateMinY, stateMaxY, out float minY, out float maxY);
            int bandCount = Mathf.Clamp(heightColorBands, 2, 32);
            var bandMatrices = new List<Matrix4x4>[bandCount];

            for (int i = 0; i < bandCount; i++)
            {
                bandMatrices[i] = new List<Matrix4x4>();
            }

            for (int i = 0; i < points.Count; i++)
            {
                GridPoint p = points[i];
                float t = Mathf.InverseLerp(minY, maxY, p.z);
                int bandIndex = Mathf.Clamp(Mathf.FloorToInt(t * bandCount), 0, bandCount - 1);
                bandMatrices[bandIndex].Add(Matrix4x4.TRS(GetRenderPosition(p), Quaternion.identity, Vector3.one * pointScale));
            }

            for (int bandIndex = 0; bandIndex < bandMatrices.Length; bandIndex++)
            {
                List<Matrix4x4> matrices = bandMatrices[bandIndex];
                if (matrices.Count == 0) continue;

                float colorT = bandCount <= 1 ? 0f : bandIndex / (float)(bandCount - 1);
                Color color = Color.Lerp(heightLowColor, heightHighColor, colorT);

                for (int idx = 0; idx < matrices.Count; idx += kBatchSize)
                {
                    int len = Mathf.Min(kBatchSize, matrices.Count - idx);
                    var mats = new Matrix4x4[len];
                    matrices.CopyTo(idx, mats, 0, len);
                    batches.Add(mats);
                    batchColors.Add(color);
                }
            }
        }

        private Vector3 GetRenderPosition(GridPoint p)
        {
            return new Vector3(p.x, p.z, p.y) + worldOffset;
        }

        /// <summary>统一设置材质：固定颜色或稳定色标</summary>
        private void ApplyMaterialProps(RenderState s)
        {
            if (instanceMaterial == null || s == null) return;

            if (useConstantColor)
            {
                // 统一固定颜色，彻底消除颜色抖动
                instanceMaterial.DisableKeyword("_COLOR_BY_HEIGHT");
                SetMaterialColor(instanceMaterial, constantColor);
            }
            else
            {
                if (colorByHeight)
                {
                    instanceMaterial.DisableKeyword("_COLOR_BY_HEIGHT");
                    ResolveHeightRange(s.MinY, s.MaxY, out float minY, out float maxY);
                    instanceMaterial.SetFloat("_MinY", minY);
                    instanceMaterial.SetFloat("_MaxY", maxY);
                }
                else
                {
                    instanceMaterial.DisableKeyword("_COLOR_BY_HEIGHT");
                }
            }
        }

        private void ResolveHeightRange(float stateMinY, float stateMaxY, out float minY, out float maxY)
        {
            minY = stateMinY;
            maxY = stateMaxY;
            if (lockHeightRange)
            {
                minY = lockedMinY;
                maxY = lockedMaxY;
            }

            if (Mathf.Approximately(minY, maxY))
            {
                maxY = minY + 0.0001f;
            }

            if (maxY < minY)
            {
                (minY, maxY) = (maxY, minY);
            }
        }

        private void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
        }

        private void SetMaterialColor(MaterialPropertyBlock block, Color color)
        {
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            block.SetColor("_EmissionColor", Color.black);
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
