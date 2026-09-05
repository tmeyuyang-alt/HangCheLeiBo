using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PointCloudDemo
{
    [Serializable] public class StockInfoResponse { public int code; public string msg; public List<StockInfo> data; }
    // 接口返回数字形式的长 ID，不能经过 float/double 转换。
    [Serializable] public class StockInfo { public long stockId; public string name; }
    [Serializable] public class RootResponse { public int code; public string msg; public DataPayload data; }
    [Serializable] public class DataPayload
    {
        public string stockId;
        public int xpointNum;
        public int ypointNum;
        public List<GridPointRaw> gridPointList;
    }
    [Serializable] public struct GridPointRaw { public float x; public float y; public float z; public float distance; }
    [Serializable] public struct GridPoint { public float x, y, z; }

    /// <summary>绘制期间只读；退出绘制后可复用矩阵数组的双缓冲状态</summary>
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
        public string apiBase = "http://192.168.12.22/EDGE_SCRAPER";
        // 保留旧场景的序列化字段；真实数据模式现在加载接口返回的所有库区。
        [HideInInspector] public string stockId = "1";

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
        private RenderState _spareState;    // 已退出绘制的缓冲，只有构建任务可以写入
        private bool _hasNext;
        private MaterialPropertyBlock _propertyBlock;
        private List<GridPoint> _lastPoints;
        private List<GridPoint> _nextPoints;
        private BuildSettings _stateSettings;
        private BuildSettings _nextSettings;

        private bool _ready;
        private bool _loading;
        private readonly CancellationTokenSource _destroyCancellation = new CancellationTokenSource();

        private void Start()
        {
            if (autoStart)
                StartCoroutine(RefreshLoop());
        }

        private void OnDestroy()
        {
            _destroyCancellation.Cancel();
            _destroyCancellation.Dispose();
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
            if (_loading || _hasNext || this == null) return;
            _loading = true;
            try
            {
                var cancellationToken = _destroyCancellation.Token;
                var points = useSimulatedData
                    ? GenerateSimulatedGridPoints()
                    : await FetchAllStockGridPointsAsync(apiBase.TrimEnd('/'), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (this == null) return;
                await PrepareNextStateAsync(points, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 场景销毁后停止请求，不再构建渲染资源。
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PointCloud] Load failed: {ex.Message}");
            }
            finally
            {
                _loading = false;
            }
        }

        private void Update()
        {
            // 主线程原子交换，避免当帧空绘制
            if (_hasNext && _nextState != null)
            {
                _spareState = _state;
                _state = _nextState;
                _lastPoints = _nextPoints;
                _stateSettings = _nextSettings;
                _nextState = null;
                _nextPoints = null;
                _hasNext = false;
                _ready = _state != null;

                ApplyMaterialProps(_state); // 只在交换时统一设置一次材质
#if UNITY_EDITOR
                // Debug.Log($"[PointCloud] Swapped. Batches={_state?.Batches.Count}");
#endif
            }

            if (!_ready || instanceMaterial == null || instanceMesh == null || _state == null) return;

            if (!_loading && !_hasNext && !_stateSettings.Matches(CaptureBuildSettings()))
                _ = RebuildCurrentStateAsync();
            ApplyMaterialProps(_state);

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
        private static async Task<List<GridPoint>> FetchAllStockGridPointsAsync(string apiBase, CancellationToken cancellationToken)
        {
            byte[] bytes = await GetResponseBytesAsync($"{apiBase}/api/grid/get-stock-info", cancellationToken);
            string json = Encoding.UTF8.GetString(bytes);
            var root = JsonUtility.FromJson<StockInfoResponse>(json);
            if (root == null || root.code != 200 || root.data == null)
                throw new Exception($"获取库区列表失败: code={root?.code}, msg={root?.msg}");

            var points = new List<GridPoint>();
            var loadedStockIds = new HashSet<long>();
            foreach (var stock in root.data)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (stock == null || stock.stockId <= 0)
                    throw new Exception("库区列表包含无效的 stockId");
                if (!loadedStockIds.Add(stock.stockId)) continue;

                string id = stock.stockId.ToString(CultureInfo.InvariantCulture);
                try
                {
                    points.AddRange(await FetchGridPointsAsync(apiBase, id, cancellationToken));
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    throw new Exception($"库区 {stock.name} (stockId={id}) 加载失败: {ex.Message}", ex);
                }
            }

            return points;
        }

        private static async Task<List<GridPoint>> FetchGridPointsAsync(string apiBase, string stockId, CancellationToken cancellationToken)
        {
            string url = $"{apiBase}/api/grid/get-grid-by-stockId?stockId={UnityWebRequest.EscapeURL(stockId)}";
            byte[] bytes = await GetResponseBytesAsync(url, cancellationToken);
            // 网络对象仅在主线程访问，UTF-8 解码和 JSON 解析在工作线程完成。
            return await Task.Run(() => ParseGridPoints(bytes, cancellationToken), cancellationToken);
        }

        private static List<GridPoint> ParseGridPoints(byte[] bytes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string json = Encoding.UTF8.GetString(bytes);
            var root = JsonUtility.FromJson<RootResponse>(json);
            if (root == null || root.code != 200 || root.data == null || root.data.gridPointList == null)
                throw new Exception($"点云响应无效: code={root?.code}, msg={root?.msg}");

            var list = new List<GridPoint>(root.data.gridPointList.Count);
            for (int i = 0; i < root.data.gridPointList.Count; i++)
            {
                if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                var p = root.data.gridPointList[i];
                list.Add(new GridPoint { x = p.x, y = p.y, z = p.z });
            }
            return list;
        }

        private static async Task<byte[]> GetResponseBytesAsync(string url, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = 15;
                var op = req.SendWebRequest();
                try
                {
                    while (!op.isDone)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }
                    cancellationToken.ThrowIfCancellationRequested();

#if UNITY_2020_2_OR_NEWER
                    if (req.result != UnityWebRequest.Result.Success)
                        throw new Exception(req.error);
#else
                    if (req.isNetworkError || req.isHttpError)
                        throw new Exception(req.error);
#endif

                    return req.downloadHandler.data;
                }
                finally
                {
                    if (!op.isDone) req.Abort();
                }
            }
        }
        #endregion

        #region Simulation
        [ContextMenu("Generate Simulated Point Cloud")]
        public void GenerateSimulatedPointCloud()
        {
            var points = GenerateSimulatedGridPoints();
            _lastPoints = points;
            _state = BuildBatches(points);
            _nextState = null;
            _hasNext = false;
            _ready = _state != null;
            ApplyMaterialProps(_state);
            _stateSettings = CaptureBuildSettings();
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
        // 只包含值类型；工作线程不得读取 MonoBehaviour / Mesh / Material。
        private struct BuildSettings
        {
            public bool ColorByHeight, LockHeightRange;
            public Color LowColor, HighColor;
            public int BandCount;
            public float MinY, MaxY, Scale;
            public Vector3 Offset;

            public bool Matches(BuildSettings other)
            {
                return ColorByHeight == other.ColorByHeight && LockHeightRange == other.LockHeightRange &&
                    LowColor == other.LowColor && HighColor == other.HighColor && BandCount == other.BandCount &&
                    MinY == other.MinY && MaxY == other.MaxY && Scale == other.Scale && Offset == other.Offset;
            }
        }

        private BuildSettings CaptureBuildSettings()
        {
            return new BuildSettings
            {
                ColorByHeight = colorByHeight, LockHeightRange = lockHeightRange,
                LowColor = heightLowColor, HighColor = heightHighColor,
                BandCount = Mathf.Clamp(heightColorBands, 2, 32),
                MinY = lockedMinY, MaxY = lockedMaxY, Scale = pointScale, Offset = worldOffset
            };
        }

        private async Task PrepareNextStateAsync(List<GridPoint> points, CancellationToken cancellationToken)
        {
            EnsureInstanceMesh();
            var settings = CaptureBuildSettings();
            var spare = _spareState;
            _spareState = null;
            var state = await Task.Run(() => BuildBatchesWorker(points, settings, spare, cancellationToken), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (this == null) return;
            _nextState = state;
            _nextPoints = points;
            _nextSettings = settings;
            _hasNext = true;
        }

        private async Task RebuildCurrentStateAsync()
        {
            _loading = true;
            try
            {
                await PrepareNextStateAsync(_lastPoints, _destroyCancellation.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[PointCloud] Rebuild failed: {ex.Message}");
            }
            finally { _loading = false; }
        }

        private void EnsureInstanceMesh()
        {
            if (instanceMesh != null) return;
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instanceMesh = temp.GetComponent<MeshFilter>().sharedMesh;
#if UNITY_EDITOR
            DestroyImmediate(temp);
#else
            Destroy(temp);
#endif
        }

        // 保留编辑器菜单的同步生成入口；定时刷新和颜色调整走后台构建。
        private RenderState BuildBatches(List<GridPoint> points)
        {
            EnsureInstanceMesh();
            return BuildBatchesWorker(points, CaptureBuildSettings(), null, CancellationToken.None);
        }

        private static RenderState BuildBatchesWorker(List<GridPoint> points, BuildSettings settings,
            RenderState spare, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batches = new List<Matrix4x4[]>();
            var colors = new List<Color>();
            if (points == null || points.Count == 0) return new RenderState(batches, 0f, 0f);

            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                float height = points[i].z;
                if (height < minY) minY = height;
                if (height > maxY) maxY = height;
            }
            if (float.IsInfinity(minY) || float.IsInfinity(maxY)) minY = maxY = 0f;

            if (settings.ColorByHeight)
                BuildHeightColorBatches(points, settings, minY, maxY, batches, colors, spare, cancellationToken);
            else
                BuildPlainBatches(points, settings, batches, spare, cancellationToken);
            return new RenderState(batches, minY, maxY, colors);
        }

        private static Matrix4x4[] GetBatchBuffer(RenderState spare, int batchIndex, int length)
        {
            if (spare != null && batchIndex < spare.Batches.Count && spare.Batches[batchIndex].Length == length)
                return spare.Batches[batchIndex];
            return new Matrix4x4[length];
        }

        private static void BuildPlainBatches(List<GridPoint> points, BuildSettings settings,
            List<Matrix4x4[]> batches, RenderState spare, CancellationToken cancellationToken)
        {
            for (int start = 0; start < points.Count; start += kBatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int length = Math.Min(kBatchSize, points.Count - start);
                var matrices = GetBatchBuffer(spare, batches.Count, length);
                for (int i = 0; i < length; i++) matrices[i] = CreatePointMatrix(points[start + i], settings);
                batches.Add(matrices);
            }
        }

        private static void BuildHeightColorBatches(List<GridPoint> points, BuildSettings settings,
            float stateMinY, float stateMaxY, List<Matrix4x4[]> batches, List<Color> colors,
            RenderState spare, CancellationToken cancellationToken)
        {
            float minY = settings.LockHeightRange ? settings.MinY : stateMinY;
            float maxY = settings.LockHeightRange ? settings.MaxY : stateMaxY;
            NormalizeHeightRange(ref minY, ref maxY);
            int bandCount = settings.BandCount;
            var counts = new int[bandCount];
            var starts = new int[bandCount];
            var written = new int[bandCount];

            // 先统计，再直接填入最终数组，避免按色带扩容矩阵列表后再复制一遍。
            for (int i = 0; i < points.Count; i++)
            {
                if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                counts[GetHeightBand(points[i].z, minY, maxY, bandCount)]++;
            }
            for (int band = 0; band < bandCount; band++)
            {
                starts[band] = batches.Count;
                Color color = Color.Lerp(settings.LowColor, settings.HighColor, band / (float)(bandCount - 1));
                for (int start = 0; start < counts[band]; start += kBatchSize)
                {
                    int length = Math.Min(kBatchSize, counts[band] - start);
                    batches.Add(GetBatchBuffer(spare, batches.Count, length));
                    colors.Add(color);
                }
            }
            for (int i = 0; i < points.Count; i++)
            {
                if ((i & 1023) == 0) cancellationToken.ThrowIfCancellationRequested();
                GridPoint point = points[i];
                int band = GetHeightBand(point.z, minY, maxY, bandCount);
                int index = written[band]++;
                batches[starts[band] + index / kBatchSize][index % kBatchSize] = CreatePointMatrix(point, settings);
            }
        }

        private static int GetHeightBand(float height, float minY, float maxY, int count)
        {
            float t = Mathf.InverseLerp(minY, maxY, height);
            return Mathf.Clamp(Mathf.FloorToInt(t * count), 0, count - 1);
        }

        private static Matrix4x4 CreatePointMatrix(GridPoint point, BuildSettings settings)
        {
            // 点仅有统一缩放和平移，不需要逐点调用原生 TRS 进行旋转分解。
            var matrix = new Matrix4x4();
            matrix.m00 = matrix.m11 = matrix.m22 = settings.Scale;
            matrix.m33 = 1f;
            matrix.m03 = point.x + settings.Offset.x;
            matrix.m13 = point.z + settings.Offset.y;
            matrix.m23 = point.y + settings.Offset.z;
            return matrix;
        }

        private static void NormalizeHeightRange(ref float minY, ref float maxY)
        {
            if (Mathf.Approximately(minY, maxY)) maxY = minY + 0.0001f;
            if (maxY < minY) (minY, maxY) = (maxY, minY);
        }

        /// <summary>统一设置材质：固定颜色或稳定色标</summary>
        private void ApplyMaterialProps(RenderState s)
        {
            if (instanceMaterial == null || s == null) return;

            if (colorByHeight)
            {
                instanceMaterial.DisableKeyword("_COLOR_BY_HEIGHT");
                ResolveHeightRange(s.MinY, s.MaxY, out float minY, out float maxY);
                instanceMaterial.SetFloat("_MinY", minY);
                instanceMaterial.SetFloat("_MaxY", maxY);
            }
            else if (useConstantColor)
            {
                // 统一固定颜色，彻底消除颜色抖动
                instanceMaterial.DisableKeyword("_COLOR_BY_HEIGHT");
                SetMaterialColor(instanceMaterial, constantColor);
            }
            else
            {
                instanceMaterial.DisableKeyword("_COLOR_BY_HEIGHT");
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
