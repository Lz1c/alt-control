---
description: 扫描所有设计文档的完成度，输出 TBD/Locked/Filled 表，并核对 doc/10-camera-rules.md 与代码的数值一致性。
argument-hint: "[--since-rev]  (可选：规则修订模式，列出受影响的下游)"
allowed-tools: "Read Grep Glob Bash"
---

用户输入：`$ARGUMENTS`

这是一个**只读**审计命令，**不**写任何文件。

## 你要做的事

### 1. 扫文档完成度

对以下三个目录里的每个 md（除了 `README.md`）：

- `doc/41-ghosts/`
- `doc/51-masks/`
- `doc/60-levels/`

按以下规则判定状态：

- 文件首行块 / frontmatter 含 `🔒 Locked` 或 `status: locked` → **🔒 Locked**
- 文件含 `verified` 标记（在 `doc/plan/decisions-log.md` 里能搜到对应的 verified 条目）→ **✅ Shipped**
- 模板必填字段都有非占位内容（无 `TBD` / `⚠️`）→ **🟡 In-Progress**
- 否则 → **⚠️ TBD**（列出还在 TBD 的字段名）

模板必填字段定义：
- 鬼卡：见 `doc/40-ghost-rules.md` 的 Boss 模板 / 扰乱模板
- 面具卡：见 `doc/50-mask-rules.md § 面具卡模板`
- 关卡卡：见 `doc/30-level-design.md § 关卡条目模板`

输出表（按目录分三段）：

```
### 鬼卡 (doc/41-ghosts/)
| 文件 | 状态 | 阻塞项 / 备注 |
|---|---|---|
| ghost-guniang.md | ⚠️ TBD | 民族 / 外观 / AI / 失败反应 仍空 |
...
```

### 2. 核对规则书 vs 代码

读：

- `doc/10-camera-rules.md`
- `Assets/_Project/Scripts/Camera/CAMCOLCameraSettings.cs`
- `Assets/_Project/Scripts/Camera/CAMCOLIsoController.cs`
- `Assets/_Project/Scripts/Camera/CAMCOLMotionBlurController.cs`

核对项（这些是文档应该和代码一致的关键数值）：

- `isoLimits` Vector2（ISO 范围）
- `shutterSpeedLimits` Vector2（快门范围）
- `apertureLimits` Vector2（光圈范围）
- 曝光补偿范围
- `isoNoiseThreshold`（ISO 噪点临界）
- 动模糊触发阈值（前后位移、左右位移、roll 角度）

输出"漂移报告"：

```
### 规则书 vs 代码
| 字段 | 文档值 | 代码值 | 状态 |
|---|---|---|---|
| ISO 上限 | 6400 | 6400 | ✅ 一致 |
| 动模糊前后阈值 | 1.25m | 1.5m | ⚠️ 漂移 |
```

### 3. 整体完成度统计

输出三行：

```
鬼卡：M 个 Locked / N 个 In-Progress / K 个 TBD（共 X 个）
面具卡：M / N / K
关卡：M / N / K
```

### 4. 当前阻塞清单

列出 3-5 条"如果想往下推必须先解决的"：

- "L1 想启动，但 ghost-guniang 还是 TBD"
- "ghost-xianying 引用了 mask-dejiang-02，但 doc/51-masks/ 下没有此文件"
- 等等

### 5. （可选）`--since-rev` 模式

如果用户传了 `--since-rev`，额外做：

1. 用 `git log --oneline -- doc/10-camera-rules.md doc/40-ghost-rules.md doc/50-mask-rules.md doc/30-level-design.md` 找出最近的规则层 commit
2. 读 `doc/plan/decisions-log.md` 查每个已 Locked 关 lock 时记录的规则层 SHA
3. 列出"哪些 Locked 关基于的规则层 SHA 比当前 HEAD 早" → 这些就是受影响关
4. 对每个受影响关，建议处理方式：
   - (a) 接受漂移（在该关 md 加注释"基于旧版规则，已停止维护"）
   - (b) 解锁→重测→重锁（默认推荐）

## 输出格式

简洁。表格优先，叙述不超过两段。最后一行写"建议下一步：<最优先要解决的事>"。

## 注意

- 不写任何文件
- 不动代码
- 找不到引用文件就说"未找到"，不要瞎编内容
- 解析 frontmatter 用宽容匹配：首行块 / yaml frontmatter / 文件首段都算
