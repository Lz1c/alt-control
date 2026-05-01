# 计划看板

> 这是 alt-control 项目的开发节奏总控。改这里之前先看 [`roadmap.md`](roadmap.md)。
> 每次 Claude 开新对话时，先在这里看一眼"当前在哪"，再决定下一步。

## 当前状态

- **版本**：v2（2026-05-01 收紧后）
- **Phase**：W1 — 设计冻结 + 资产入场
- **当前关卡**：01 · 山（唯一一关）
- **最后更新**：2026-05-01

## v2 一句话

第一人称登山摄影 + 治愈小故事 + Cairn 风。一关，30-45 分钟。详见 [`../00-overview.md`](../00-overview.md)。

## 下一步该做什么

W1 设计文档刚写完，接下来：

1. **答 4 个 🔥 阻塞问题**（见 [`open-questions.md`](open-questions.md)）：主角名 / 朋友名 / 山名 / 时长
2. **`/audit-docs`** 检查 6 张新影卡 + `01-shan.md` 完成度
3. **导入 Cairn 资产**到 Unity 项目（用户负责）
4. **`/lock-level 01`** 把 01 山关锁定，进入 W2 实装

随时可以跑 [`/next-step`](../../.claude/commands/next-step.md) 拿到最新建议。

## 索引

- [roadmap.md](roadmap.md) — 4 周里程碑
- [knowledge-lock-chain.md](knowledge-lock-chain.md) — 6 段山路 × 6 相机概念 × 6 笔记本页
- [decisions-log.md](decisions-log.md) — 已做的设计决定（最新在前）
- [open-questions.md](open-questions.md) — 待决策清单
- [rules-revisions.md](rules-revisions.md) — 规则层修订流水（v2 cutover 已归档）

## 状态图例

| 标记 | 含义 | 谁能改 |
|---|---|---|
| ⚠️ TBD | 还没开始 / 占位 | 任何人随便改 |
| 🟡 In-Progress | 正在做 | 当前作者改 |
| 🔒 Locked-for-build | 文档冻结，进入实装 | 改要走 [rules-revisions](rules-revisions.md) 流程 |
| ✅ Shipped | 实装通过验证 | 同上 |
| ❌ 废止 | v1 内容已归档 | 不再维护 |

## 当前阻塞

- W1 启动前提：4 个 🔥 阻塞问题（[open-questions.md](open-questions.md)）
- W1 启动前提：6 张新影卡未建（`doc/41-ghosts/`）
- W1 启动前提：`60-levels/01-shan.md` 未建
- W1 启动前提：Cairn 场景资产未导入（用户侧）

## v1 已废止内容（不再维护，留作历史）

- `doc/20-office-hub.md`（办公室 hub）
- `doc/50-mask-rules.md`（傩戏面具体系）
- `doc/90-reference/nuo-masks.md`（傩戏调研）
- `doc/41-ghosts/` 8 张旧鬼卡（运动员鬼 + 6 民俗鬼 + 姑娘）
- `doc/51-masks/`（空目录，不再扩张）
- v1 Phase 0-N 多关纵切计划

详见 [`rules-revisions.md` 2026-05-01 v2 cutover](rules-revisions.md)。
