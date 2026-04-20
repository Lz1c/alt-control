# 10 · 相机规则书

> 这一卷：相机系统的**硬规则**。所有数值都应该在这里能查到；具体实现在 `Assets/_Project/Scripts/Camera/`。
> 加新参数 / 改数值时，先改这里，再改代码。

## 参数总览

所有参数存在 `CAMCOLCameraSettings`，这是**全局唯一数据源**。

| 参数 | 字段 | 默认值 | 默认范围（可在 Inspector 改） | 单位 |
|---|---|---|---|---|
| ISO | `iso` | 100 | 100 – 6400 | 绝对值 |
| 快门速度 | `shutterSpeed` | 1/125 ≈ 0.008 s | 1/1000 – 1/15 | 秒 |
| 光圈 | `aperture` | f/5.6 | f/1 – f/32 | f-stop |
| 曝光补偿 | `exposureCompensation` | 0 | -3 – +3 | EV |

**硬边界**（代码里 `OnValidate` 强制）：ISO ≥ 1；快门 ≥ 0.0001 s；光圈 ≥ 0.1。

### 解锁策略（办公室推进）

玩家初期**不是**拿到全范围。随办公室推进扩张 `Vector2 limits`。见 `20-office-hub.md § 相机升级轨道`。默认建议：

| 阶段 | ISO 上限 | 快门最慢 | 快门最快 | 光圈范围 |
|---|---|---|---|---|
| 序章 | 800 | 1/30 | 1/500 | 2.8 – 16 |
| 第一章 | 1600 | 1/15 | 1/1000 | 1.8 – 22 |
| 第二章 | 3200 | 1s | 1/2000 | 1.4 – 32 |
| 终章 | 6400 | 8s | 1/4000 | 1.0 – 32 |

> **注**：代码默认值对应第一章中后段。序章要手动在 `CAMCOLCameraSettings` 收窄 `isoLimits` 等字段。

## 曝光（EV100）

代码：`CAMCOLExposureApplier`。

$$\text{EV}_{100} = \log_2\!\left(\frac{A^2}{T} \cdot \frac{100}{\text{ISO}}\right)$$

- **基准 EV**：光圈 5.6，快门 1/125，ISO 100。偏离基准的 EV 差值直接变成 `ColorAdjustments.postExposure`（URP Volume）。
- **夹紧范围**：`postExposure ∈ [-6, +6]`。极端数值不会无限拉高亮度。

### 自动测光辅助（不是"自动曝光"）

半按 `O` 时，`CAMMeteringBase` 读场景亮度，算出"为了让中心灰压到 0.18 所需的 EV 偏移" `MeteredExposureOffset`。

加到 `postExposure` 上，但**保守**：

| 项 | 值 | 含义 |
|---|---|---|
| `AutoMeteringStrength` | 0.4 | 只补偿 40% |
| `AutoMeteringDeadZone` | 0.2 EV | 小偏差不补 |
| 夹紧 | ±1.5 EV | 永远不会替玩家搞定太暗/太亮的场景 |

**意图**：测光只是"参考读数 + 一点点自动矫正"。玩家该自己看直方图和场景亮度。

### 测光模式

| 类 | 文件 | 算法 |
|---|---|---|
| `CAMMetering` | 全局 | 对数平均亮度（log-average） |
| `CAMEvaluativeMetering` | 分区评价 | 把画面切成 `zoneColumns × zoneRows` 格，按**中心权重 / 对比度 / 对焦区 / 高光保护**加权 |

`CAMEvaluativeMetering` 默认：8×5 分区，`centerWeight=1.35`，`contrastWeight=0.8`，`focusZoneBoost=2.2`。对焦区权重最高 —— **对焦在哪里，就测那里**。

## 对焦

代码：`CAMFocusController`。

- 半按 `O` 或调用 `FocusCenterOnce()` → 从屏幕中心发射 Ray，最远 `maxFocusDistance=500m`。
- 命中：`FocusPoint`、`FocusDistance`、`HasFocusLock=true`。
- 未命中：把焦点推到最远距离，`HasFocusLock=false`。
- **对焦 ≠ 景深**。景深来自物理相机的 `aperture`；但 `FocusDistance` 可以喂给后期景深 / 分区测光。

## 运动模糊

代码：`CAMCOLMotionBlurController` + `CAMMotionBlurSubject`。

### 原理

拍照时：
1. 记录相机位姿和所有 `CAMMotionBlurSubject` 快照。
2. **等待 `shutterSpeed` 秒**（`yield return new WaitForSeconds(ExposureDuration)`）—— 真实模拟快门打开时间。
3. 再次快照 → 算位移 → 喂给模糊 shader。

### 相机级模糊强度参考

| 运动类型 | 需要多少才达到"满模糊"强度 |
|---|---|
| 前后移动 | 1.25 m |
| 左右移动 | 0.50 m |
| 上下移动 | 0.50 m |
| 横滚（Roll） | 12° |

**补偿系数** `CompensationStrength=0.45`。即使玩家抖，也不会瞬间糊成一团。

### 主体模糊（关键玩法）

凡是挂 `CAMMotionBlurSubject` 的物体，会在拍照瞬间单独计算其屏幕空间位移，画出局部模糊。

- 每个 `CAMMotionBlurSubject` 有 `blurMultiplier`（默认 1）——**给不同鬼设不同值**：
  - 影子类快速鬼：3.0（稍微动一下就糊）
  - 徘徊的老太：0.5（慢，宽容）
- `minimumScreenMotionPixels=2`：低于 2 像素的运动不画模糊。
- **Boss 鬼必须挂 `CAMMotionBlurSubject`**，否则"需要快门多快"无法通过血管实现。

## ISO 噪点

代码：`CAMCOLIsoController`。

### 触发阈值

| 参数 | 默认 |
|---|---|
| `isoNoiseThreshold` | 6400（ISO 超过这个才开始有噪点） |
| `shutterNoiseThreshold` | 1 s（快门慢于 1s 才开始有热噪点） |
| `maxSupportedIsoNoise` | 25600（ISO 到这一步噪点饱和） |
| `maxSupportedShutterNoise` | 8 s（快门到这一步热噪点饱和） |

### 噪点类型

- **实时预览**：URP `FilmGrain` override，`intensity` 随 ISO 插值到 `GrainIntensityAtIso6400=0.8`。
- **拍后照片**：专用 shader `Hidden/Simulated Camera/Photo Processing`：
  - `LuminanceNoiseAtIso6400 = 0.1`（亮度噪点）
  - `ChromaNoiseAtIso6400 = 0.075`（色度噪点，块状 4 px）
  - `DarkAreaResponse = 0.85`（暗部更明显 —— 真实相机的脾气）
- **CPU fallback**：shader 丢失时走 `ApplyCpuFallbackIso()`。

### 意图

玩家可以"用高 ISO 拍到暗处的鬼"，但会被噪点惩罚 —— 鬼的细节被颗粒吃掉，可能过不了 Boss 照片的清晰度门槛。权衡由玩家做。

## 拍照判定（程序需要实现）

> **状态**：⚠️ 当前代码没有实现 Boss 判定，只有拍照出图。需要写 `PhotoEvaluator`。

**输入**（拍照瞬间采样）：
- Boss 鬼在屏幕空间的 bounding rect（从 `CAMMotionBlurSubject.CaptureSnapshot` 得到世界包围盒，投到屏幕）。
- `FocusDistance` vs Boss 实际距离。
- Boss 本帧 / 上一帧的屏幕位移（即它的模糊强度）。
- 照片在 Boss 区域内的亮度（是否过曝 / 欠曝）。
- 当前 ISO（换算后的噪点强度）。

**判定分数**（建议起点）：

| 维度 | 及格 | 满分 | 权重 |
|---|---|---|---|
| 位于画面中央 70% 区域 | 必要（不满足直接判失败） | - | - |
| 对焦距离与 Boss 距离误差 | ≤ 15% | ≤ 5% | 25 |
| Boss 主体模糊像素 | ≤ Boss 阈值 | ≤ 阈值 × 0.3 | 30 |
| 曝光偏差（EV） | ≤ 1.5 | ≤ 0.5 | 20 |
| ISO 噪点强度 | ≤ 0.5 | ≤ 0.2 | 15 |
| Boss 占屏比 | ≥ Boss 阈值 | ≥ 阈值 × 2 | 10 |

**Boss 过线门槛**：每只 Boss 在鬼谱里写自己的 `passingScore`（建议 60 起步，终章 Boss 可以 85+）。

**扰乱鬼判定**：只要 Boss 条件"位于画面中央" + "对焦误差 ≤ 30%" 满足即可进图鉴。

## 长曝光累积（暂未接入）

代码：`CAMExposureAccumulator` + `SimulatedExposureAccumulation.shader`。

另一条路线：不是"等一段时间再快照"，而是在快门窗口内真正**累积多帧**。会产生光轨效果（灯光、运动鬼的拖影）。**当前未接入 `CAMPhotoCapture`**。

**何时考虑接入**：如果后期需要"长曝光拍光轨"玩法（比如拍灯笼残影辨认鬼的路径），替换 `CAMPhotoCapture.CapturePhotoRoutine` 的等待+快照段落。
