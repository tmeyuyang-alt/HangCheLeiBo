# Unity 项目上下文

2026-09-05 检查，基于提交 `d2f0e39` 及工作区；本次重点为点云数据接入。

- 项目：`D:/GIT/LeiBoHangChe`，Unity 2021.3.45f2，Built-in 渲染管线，旧版 Input Manager。依据：`ProjectSettings/ProjectVersion.txt`、`GraphicsSettings.asset`、`ProjectSettings.asset`。
- 包：Cinemachine 2.10.3、TextMeshPro 3.0.6、UGUI、Unity Test Framework 1.1.33。依据：`Packages/manifest.json`。未发现已配置的 Unity MCP，本会话没有可调用的 Unity Editor 工具。
- 构建场景：`Assets/Scenes/1.Login.unity`、`Assets/Scenes/2.Main.unity`，前者为首场景。`Assets/Scripts/TitlePanel.cs` 使用 SceneManager 切换场景。
- 一方代码主要在 `Assets/Script`（包括 PLC）和 `Assets/Scripts`（UI、账号、设备配置、数据等）；点云脚本为 `Assets/PointCloudLoader.cs`，位于默认 `Assembly-CSharp`。未发现一方测试程序集；名为 Test 的业务脚本不代表自动化测试。
- 点云使用 MonoBehaviour、协程驱动 Task 请求和 DrawMeshInstanced 批次渲染。UnityWebRequest 和渲染对象只在主线程访问；点云 UTF-8 解码、JsonUtility 解析、矩阵批次计算使用 Task.Run。构建使用值类型设置快照，Update 统一切换数据和设置，并复用已退出绘制的矩阵缓冲。公开配置字段保留 Inspector 序列化兼容性；注释主要为中文。
- 真实数据流程：每轮先 GET `/api/grid/get-stock-info`，以 long 解析所有 stockId，再逐库 GET `/api/grid/get-grid-by-stockId?stockId=...`。合并所有点后统一切换；空库允许，失败保留旧画面，下一轮重试；对象销毁取消请求。刷新间隔从每轮完成后计算。
- 点云接口地址由 `Assets/StreamingAssets/PointCloudConfig.json` 配置：1号行车使用 `192.168.12.22`，2号行车使用 `192.168.12.23`。切换行车时取消旧请求、清除旧点云，并立即从新地址重新加载。
- 2026-09-05 实测局域网接口返回 7 个库区、35,442 个点（矿球库为空）；坐标自带库区位置，保持 `(x,z,y) + worldOffset` 映射。此为当时快照，点数可变。
- 两个引用点云组件的场景为 `Assets/Scenes/2.Main.unity` 和 `Assets/Scenes/LeiBo_hc_01 1.unity`，已配置局域网地址并关闭模拟数据。
- 验证手段：Unity 自带 Roslyn 编译、实际 GET 验证接口，以及隔离 Unity 工程中的性能/数据一致性/HTTP 回归。2026-09-05 性能优化结果见 `Docs/AI/PointCloudPerformance.md`。尚未验证业务场景的实际帧率或完整 Player 构建。
- 编辑场景时只定点修改对应组件的 API/模拟开关；工作区存在用户并行修改的场景、光照和配置资源，必须保留。
