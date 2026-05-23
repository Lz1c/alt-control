# 41 · 影 / 鬼谱条目

> 每只影一个 md 文件。规则与模板见 [`../40-ghost-rules.md`](../40-ghost-rules.md)。
> 加新影走 `/new-ghost` 斜杠命令，会自动建文件 + 更新本索引。
>
> **2026-05-11 v2.1 再收紧**：v2.0 的 6 张影卡进一步砍到 **3 张**——保留 Boss B + 跑步鬼类型 + 极光鬼类型。其余教学概念分别合并进这 3 张。详见 [`../plan/rules-revisions.md` 2026-05-11](../plan/rules-revisions.md)。

## 命名约定

- **prose / UI 里**：写作"影 / 余像 / 旧时人 / 山的记忆"
- **技术 ID 仍叫 `ghost-*`**：保留 ID 前缀避免破坏所有引用

## v2.1 当前条目（3 只）

### Boss 影（1 只）

| ID | 显示名 | 段位 | 教学概念 | 状态 |
|---|---|---|---|---|
| `ghost-shan-ding-shou-zhe` | 山顶守者 | 段 3 顶峰 | 综合：景深 × 对焦 × 长曝 | 🟡 In-Progress |

### 扰乱影（2 只）

| ID | 显示名 | 段位 | 教学概念 | 状态 |
|---|---|---|---|---|
| `ghost-pao-shan-ke` | 跑山客 | 段 1 山道 | 快门冻结 + ISO 基础 + 半按命中动目标 | 🟡 In-Progress |
| `ghost-ji-guang-shi` | 极光使 | 段 2 夜脊 | 长曝积累 + ISO 长曝控制 + 防抖 | 🟡 In-Progress |

> ⚠️ 用 `/new-ghost` 时**口头明确告知 v2 治愈基调 + 山系**，否则 agent 可能按 v1 民俗调性写。

## v2.0 → v2.1 影卡变更（2026-05-11）

| v2.0 旧卡 | v2.1 处理 | 教学去向 |
|---|---|---|
| `ghost-lin-ci-jing-zuo`（林祠静坐者） | ❌ 删 | 半按对焦 + 中央构图并入 trailhead 教程 / 跑山客 |
| `ghost-wu-li-can-ying`（雾里残影） | ❌ 删 | 长曝消形并入极光使长曝积累 |
| `ghost-shan-ya-lue-ying`（山鸦掠影） | ❌ 删 | 快门冻结教学全部交给跑山客 |
| `ghost-feng-kou-shan-jun`（风口山君 Boss A） | ❌ 删 | Boss A 整体砍掉；ISO 噪点权衡并入极光使 |
| `ghost-xing-ling`（星灵） | ❌ 删 | 长曝积累教学全部交给极光使 |
| `ghost-shan-ding-shou-zhe`（山顶守者 Boss B） | ✅ 保留 | 不变 |

## v1 鬼卡（v2.0 cutover 时已物理删除）

v1 设计阶段的 8 张鬼卡（校园 + 民俗）已从仓库彻底删除——历史在 git log 里查。详见 [`../plan/rules-revisions.md` 2026-05-01 v2 cutover](../plan/rules-revisions.md)。
