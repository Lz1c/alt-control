# 规则层修订流水

> 改规则层文件 `doc/10-camera-rules.md` / `30-level-design.md` / `40-ghost-rules.md` / `45-uncanny.md`（**或 `Assets/_Project/Scripts/Camera/` 下的相机代码**）之前，**先**在这里起一条草稿条目；改完后归档。
>
> 这是为了让下游（已 Locked 的关、已数值化的鬼/面具）不被悄悄改飞。

## 流程

1. **想改之前**：在「草稿区」起一条，写"打算改什么、为什么改"。和 Claude 讨论确认 → 才动规则文件
2. **改完立刻**：跑 `/audit-docs --since-rev`（带规则修订模式），输出影响清单：
   - 哪些已 Locked 关引用了被改的字段（按 `/lock-level` 时记录的 git short SHA 比对）
   - 哪些已数值化的鬼/面具数值需要重新校验
   - 哪些 Unity prefab/scene 需要重测
3. **归档**：把草稿移到「已归档」，补"影响项 + 修复策略"
4. **下游处理**：每个被影响的 Locked 关——选 (a) 接受漂移（在该关 md 写"基于旧规则版本，已停止维护"）或 (b) 解锁→重测→重锁。**默认走 (b)**

效果：规则层永远是 living document，但下游不会偷偷过期。每次改规则都有一笔账。

---

## 草稿区（discussion-in-progress）

（暂无）

---

## 已归档

### 2026-05-11 · v2.1 再收紧 — 6 影骨架砍到 3 影

> 一个月交作业 deadline 临近，小组讨论后用户决定**进一步收紧鬼的数量**。"不要太多，不然做不完了。"

**触发**：用户告知"鬼的话就做几个就好了，把跑步鬼和极光鬼这种类型的加回去，其它的去掉"。

**核心决定**：

- **影从 6 砍到 3**：跑山客（`ghost-pao-shan-ke`）+ 极光使（`ghost-ji-guang-shi`）+ 山顶守者（`ghost-shan-ding-shou-zhe`，唯一 Boss）
- **段位从 6 段塌成 3 段 + trailhead**：trailhead → 段 1 山道（白天 · 跑山客）→ 段 2 夜脊（夜段 · 极光使）→ 段 3 顶峰（清晨 · Boss 山顶守者）
- **教学合并**：
  - trailhead 教半按对焦 + 中央构图（原 v2.0 林祠静坐者并入）
  - 段 1 跑山客 = 快门冻结 + ISO 基础 + 半按命中动目标
  - 段 2 极光使 = 长曝积累 + ISO 长曝控制 + 防抖（三合一，吸收原 v2.0 雾里残影 + 风口山君 ISO 噪点 + 星灵）
  - 段 3 山顶守者 = 景深 × 对焦 × 长曝综合（不变）
- **真扑面 jumpscare 一并撤销**：v2.0 给段 4 风口 Boss A 留的扑面例外随 Boss A 被砍而失去锚点；v2.1 回到"积累式不安，无刺响"
- **诡异调味事件清单重新分布**：trailhead 1 / 段 1 2 / 段 2 2 / 段 3 = 0（顶峰仍绝对干净）。删段 4 风口扑面 + 段 4 雪线第二剪影 + 段 2 雾带 3 事件 + 段 3 岩壁反向掠过（合并入段 1 跑山客的反向掠过）。
- **预估通关时长**：30-45 分钟 → **20-30 分钟**

**打算改什么 → 实际改了什么**：

| 文件 / § | 旧 (v2.0) | 新 (v2.1) | 处理 |
|---|---|---|---|
| `41-ghosts/ghost-feng-kou-shan-jun.md` | Boss A 风口山君 | 砍 | **物理删除** |
| `41-ghosts/ghost-lin-ci-jing-zuo.md` | 林祠静坐者 | 砍（教学并入 trailhead）| **物理删除** |
| `41-ghosts/ghost-wu-li-can-ying.md` | 雾里残影 | 砍（长曝消形教学并入极光使）| **物理删除** |
| `41-ghosts/ghost-shan-ya-lue-ying.md` | 山鸦掠影 | 砍（快门冻结教学交给跑山客）| **物理删除** |
| `41-ghosts/ghost-xing-ling.md` | 星灵 | 砍（长曝积累教学交给极光使）| **物理删除** |
| `41-ghosts/ghost-shan-ding-shou-zhe.md` | 山顶守者 Boss B | 保留为唯一 Boss | 不动 |
| `41-ghosts/ghost-pao-shan-ke.md` | — | **新建** 跑山客 | 新文件 |
| `41-ghosts/ghost-ji-guang-shi.md` | — | **新建** 极光使 | 新文件 |
| `41-ghosts/README.md` | 6 影索引 | 3 影索引 + v2.0→v2.1 变更表 | 重写 |
| `60-levels/01-shan.md` | 6 段山路 + 6 影 + 7 调味事件 + 1 扑面 | 3 段 + 3 影 + 5 调味事件 + 0 扑面 | 重写 |
| `60-levels/README.md` | Boss = ghost-feng-kou-shan-jun + ghost-shan-ding-shou-zhe | Boss = ghost-shan-ding-shou-zhe（唯一）| 局部改 |
| `45-uncanny.md` | 8 调味事件预算 + 1 扑面 | 6 调味事件预算 + 0 扑面 + 撤销 JumpscareTrigger + RepositionedShadow | 局部改 |
| `40-ghost-rules.md` | 通用字段段位 1-6 + 教学概念枚举 + 影分布"风口段+顶峰段两只" + Boss A=65/Boss B=70 + 扑面例外条款 | 段位 1-3 + 教学概念枚举（3 项）+ 影分布"顶峰段一只" + Boss=70 + v2.1 全片 0 处扑面 | 局部改 |
| `10-camera-rules.md § 解锁策略` | 6 段解锁表 | 3 段解锁表 | 局部改 |
| `10-camera-rules.md § passingScore` | 50/60/65/70 四档 | 60/70 两档 | 局部改 |
| `30-level-design.md` | 6 段线性骨架 + 2 Boss 模板 | 3 段骨架 + 1 Boss 模板 | 重写 |
| `70-narrative.md` | 6 页笔记本 + 段 4 风口不安高潮 + 扑面 + Boss A 隐藏页 | 3 页笔记本 + 段 2 夜脊情绪暗流 + v2.1 0 扑面 + 删 Boss A 奖励 | 局部改 |
| `00-overview.md` | 6 段循环图 + 4 minion + 2 Boss + Boss A 隐藏 + 段 4 扑面边界 | 3 段循环图 + 2 扰乱 + 1 Boss + v2.1 0 扑面边界 | 局部改 |
| `plan/roadmap.md` | W2 林祠静坐者垂直切片 + W3 6 影 + 2 Boss + 30-45 分钟交付 | W2 跑山客垂直切片 + W3 3 影 + 1 Boss + 20-30 分钟 | 局部改 |
| `plan/knowledge-lock-chain.md` | 6 段 × 6 概念 | 3 段 × 4 教学目标 | 重写 |
| `plan/open-questions.md` | Boss A 隐藏奖励 + 扑面实装 + 雪线第二剪影 + 6 页笔记本文案 | 删上述 + 3 页笔记本文案 + 一次性事件比例改写 | 局部改 |
| `README.md`（doc 根） | 6 张影卡 | 3 张影卡 | 局部改 |

**保留不动**：

- `Assets/_Project/Scripts/Camera/` 全部相机模拟代码——数据驱动、场景无关
- `10-camera-rules.md` 除§解锁策略 + § passingScore 外全部规则（EV / 测光 / 对焦 / 模糊 / ISO 噪点 / 拍照判定 / 长曝累积）
- `40-ghost-rules.md` 治愈基调指引第 1-2 / 4-6 条
- `45-uncanny.md` 7 类诡异分类 + 设计原则 + 调性参考
- `ghost-shan-ding-shou-zhe.md` 整张卡（Boss B 仍是 v2.1 唯一 Boss，机制不变）

**实际影响项**：

- 已 Locked 关：无（项目从未走到 lock-level）
- 数值化影：无（所有影卡都是 🟡 In-Progress）
- Unity prefab/scene：无（W2 还未实装）
- 代码：无（相机管线和影数量完全解耦）

**修复策略**：v2.0 的 5 张要砍的影卡物理删除（历史在 git log，commit `780af5e` 之前是 v2.0 状态），v2.1 从 v2.0 的 1 张保留卡 + 2 张新卡起骨架。

**后续动作**：见 `decisions-log.md` 2026-05-11 条目。

---

### 2026-05-01 · v2 收紧 — 单关登山治愈作业版（批量回滚）

> 这是一次**项目级方向变更**，不是普通规则改动。一个月交作业 deadline，原先的多关 + 办公室 hub + 傩戏面具收集体系全部砍掉。所有 v1 规则层文档同步标废止或局部改写。

**触发**：用户告知"只有一个月把这个游戏做出来"，且"题材改一下，人物上山进行拍照，游戏风格变成 Cairn (Steam app/1588550)"。详见 [`decisions-log.md` 2026-05-01 v2 收紧](decisions-log.md)。

**打算改什么 → 实际改了什么**：

| 文件 / § | 旧 | 新 | 处理 |
|---|---|---|---|
| `00-overview.md` | 驱魔师 + 办公室 + 多关 + 傩戏面具收集 | 登山摄影师 + 单关 + 朋友相机 + 笔记本叙事 + 治愈 | 全新覆盖 |
| `10-camera-rules.md § 解锁策略` | 序章/第一章/第二章/终章 4 阶段 ISO/快门/光圈分阶解锁 | 单关全功能；解锁节奏改"叙事驱动"（笔记本翻页） | 删该 § 重写 |
| `20-office-hub.md` | 办公室 hub 整章 | 砍 | **物理删除** |
| `30-level-design.md` | 生化危机式封闭关卡模板 + 多种 Boss 触发条件 | 线性登山结构模板 + 笔记本叙事教学 | 模板重写 |
| `40-ghost-rules.md` | "鬼"概念 + 傩戏面具锚定 + 一鬼一面具 | "影 / 余像"治愈基调 + 删面具锚定；技术 ID 仍 `ghost-*` | 局部改 |
| `50-mask-rules.md` | 傩戏五色 + 民族体系 + 一对一收集 | 砍 | **物理删除** |
| `90-reference/nuo-masks.md` | 300 行傩戏调研 | 砍 | **物理删除** |
| `41-ghosts/*` 8 张旧卡 | 校园 + 民俗 ghosts | 全废，新建 6 张影卡（2 Boss + 4 minion） | 旧卡**物理删除**；新卡新建 |
| `51-masks/` 目录 | 空目录 | 砍 | **物理删除** |
| `60-levels/` | 空目录 | 新建 `01-shan.md` | — |
| `plan/roadmap.md` | Phase 0..N 多关纵切 | v2 单关 4 周 W1-W4 | 全新 |
| `plan/knowledge-lock-chain.md` | L1-L5+ 候选锁链节 | 单关版 6 段山路 × 6 概念 | 全新 |

**保留不动**：

- `Assets/_Project/Scripts/Camera/` 全部相机模拟代码——数据驱动、场景无关
- `10-camera-rules.md` 除§解锁策略外全部规则（EV / 测光 / 对焦 / 模糊 / ISO 噪点 / 拍照判定 / 长曝累积）
- 实体相机外设 + 双屏 + 5 路硬件输入抽象（2026-04-25 决议保留）

**实际影响项**：

- 已 Locked 关：无（项目从未走到 lock-level）
- 数值化鬼：无（旧 8 张卡全是 🟡/⚠️ 状态）
- Unity prefab/scene：无（L1 还未实装）
- 代码：无（相机管线和题材完全解耦）

**修复策略**：v1 全部物理删除（历史在 git log 里，commit `738d070` 之前是 v1 状态），v2 从零起骨架。

**后续动作**：见 `decisions-log.md` 2026-05-01 条目下的 14 个 task。

---

## 修改条目模板

复制到草稿区填写：

```
### YYYY-MM-DD · <一句话标题>

- **打算改什么**：<文件名 § 章节> 从 X → Y
- **为什么**：<理由 / 触发场景>
- **预计影响项**（改之前估算）：
  - 已 Locked 关：<编号列表 / 无>
  - 数值化鬼：<ID 列表 / 无>
  - Unity prefab/scene：<路径 / 无>
  - 代码：<文件 / 无>
- **决定**：✅ 改 / ❌ 不改
```

归档时补一段：

```
- **实际影响项**（改之后由 /audit-docs --since-rev 给出）：
  - ...
- **修复策略**：
  - 关 NN：(a) 接受漂移 / (b) 解锁重测
  - 鬼 XX：调数值（具体数值）/ 重测
- **后续动作**：<新建任务编号 / 无>
```
