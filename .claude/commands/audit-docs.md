---
description: 扫描所有设计文档的完成度，输出 TBD/Locked/Filled 表，并核对 doc/10-camera-rules.md 与代码的数值一致性（v2 单关版）。
argument-hint: "[--since-rev]  (可选：规则修订模式，列出受影响的下游)"
allowed-tools: "Read Grep Glob Bash"
---

用户输入：`$ARGUMENTS`

这是一个**只读**审计命令，**不**写任何文件。

## v2 上下文

v2 项目是单关登山治愈版（详见 [`doc/00-overview.md`](doc/00-overview.md)）。本命令只扫 v2 生效的目录，**不扫 v1 已废止的目录**。

## 你要做的事

### 1. 扫文档完成度

对以下两个目录里的每个 md（除了 `README.md`）：

- [`doc/41-ghosts/`](doc/41-ghosts/)
- [`doc/60-levels/`](doc/60-levels/)

⚠️ **不扫 `doc/51-masks/`** — 目录已物理删除（v2 没有面具系统）。

按以下规则判定状态：

- 文件首行块含 `❌` / "v1 已废止" → **❌ 废止**（直接归档段，不计入完成度）
- 文件首行块 / frontmatter 含 `🔒 Locked` 或 `status: locked` → **🔒 Locked**
- 文件含 `verified` 标记（在 `doc/plan/decisions-log.md` 里能搜到对应的 verified 条目）→ **✅ Shipped**
- 模板必填字段都有非占位内容（无 `TBD` / `⚠️`）→ **🟡 In-Progress**
- 否则 → **⚠️ TBD**（列出还在 TBD 的字段名）

模板必填字段定义：
- 影卡：见 [`doc/40-ghost-rules.md`](doc/40-ghost-rules.md) 的 Boss 影 / 扰乱影模板
- 关卡卡：见 [`doc/30-level-design.md § 关卡条目模板`](doc/30-level-design.md)

输出表（按目录分两段）：

```
### 影卡 (doc/41-ghosts/)
| 文件 | 状态 | 阻塞项 / 备注 |
|---|---|---|
| ghost-lin-ci-jing-zuo.md | 🟡 In-Progress | 笔记本完整版文案待填 |
| ghost-feng-kou-shan-jun.md | 🟡 In-Progress | 笔记本完整版文案待填 |
...

### 关卡 (doc/60-levels/)
| 文件 | 状态 | 阻塞项 / 备注 |
|---|---|---|
| 01-shan.md | 🟡 In-Progress | 主角名 / 朋友名 / 山名 待填 |
```

### 2. 核对规则书 vs 代码

读：

- [`doc/10-camera-rules.md`](doc/10-camera-rules.md)
- [`Assets/_Project/Scripts/Camera/CAMCOLCameraSettings.cs`](Assets/_Project/Scripts/Camera/CAMCOLCameraSettings.cs)
- [`Assets/_Project/Scripts/Camera/CAMCOLIsoController.cs`](Assets/_Project/Scripts/Camera/CAMCOLIsoController.cs)
- [`Assets/_Project/Scripts/Camera/CAMCOLMotionBlurController.cs`](Assets/_Project/Scripts/Camera/CAMCOLMotionBlurController.cs)

核对项：

- `isoLimits` Vector2（ISO 范围）
- `shutterSpeedLimits` Vector2（快门范围）
- `apertureLimits` Vector2（光圈范围）
- 曝光补偿范围
- `isoNoiseThreshold`（ISO 噪点临界）
- `shutterNoiseThreshold`（快门热噪点临界）
- 动模糊触发阈值（前后位移、左右位移、roll 角度）

⚠️ 注意 v2 的解锁策略是**单关内动态收紧**（见 [`doc/10-camera-rules.md § 解锁策略 v2 单关叙事驱动`](doc/10-camera-rules.md)）。代码默认值对应段 6 全开状态；trailhead 和段 1-5 通过 `CameraParamGate`（W2 实装项）动态收紧。这不是漂移。

输出"漂移报告"：

```
### 规则书 vs 代码
| 字段 | 文档值 | 代码值 | 状态 |
|---|---|---|---|
| ISO 上限（代码默认）| 6400 | 6400 | ✅ 一致 |
| 快门上限（代码默认）| 1/15 | 1/15 | ✅ 一致 |
| 动模糊前后阈值 | 1.25m | 1.25m | ✅ 一致 |
```

### 3. 整体完成度统计

输出两行：

```
影卡：M 个 Locked / N 个 In-Progress / K 个 TBD（共 X 个 v2 卡，Y 个 v1 已废止不计入）
关卡：M / N / K（v2 应该是 0 / 1 / 0 = 1 个 In-Progress）
```

### 4. 当前阻塞清单

列出 3-5 条 v2 推进必解的：

- "01-shan.md In-Progress，但主角名 / 朋友名 / 山名仍 TBD"
- "ghost-lin-ci-jing-zuo.md 笔记本完整版文案缺"
- "Cairn 资产未导入 Unity"
- 等等

### 5. （可选）`--since-rev` 模式

如果用户传了 `--since-rev`，额外做：

1. 用 `git log --oneline -- doc/10-camera-rules.md doc/40-ghost-rules.md doc/30-level-design.md doc/45-uncanny.md` 找出最近的规则层 commit（v2 规则层是这 4 个文件）
2. 读 [`doc/plan/decisions-log.md`](doc/plan/decisions-log.md) 查 `01-shan.md` lock 时记录的规则层 SHA（如果已 Locked）
3. 列出"哪些 Locked 关基于的规则层 SHA 比当前 HEAD 早" → 这些就是受影响关
4. 对每个受影响关，建议处理方式：
   - (a) 接受漂移
   - (b) 解锁 → 重测 → 重锁（默认推荐）

## 输出格式

简洁。表格优先，叙述不超过两段。最后一行写"建议下一步：<最优先要解决的事>"。

## 注意

- 不写任何文件 / 不动代码
- 找不到引用文件就说"未找到"，不要瞎编内容
- 解析 frontmatter 用宽容匹配：首行块 / yaml frontmatter / 文件首段都算
- 已废止文件 (含 ❌) 单独列归档段，不计入完成度统计
