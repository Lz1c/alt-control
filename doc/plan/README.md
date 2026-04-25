# 计划看板

> 这是 alt-control 项目的开发节奏总控。改这里之前先看 [`roadmap.md`](roadmap.md)。
> 每次 Claude 开新对话时，先在这里看一眼"当前在哪"，再决定下一步。

## 当前状态

- **Phase**：0 — 铺地基
- **当前关卡**：暂无（L1 待启动）
- **最后更新**：2026-04-25

## 下一步该做什么

Phase 0 的工具/文档铺设刚完成，接下来需要做一次 **dry-run 验收**（见下方），通过后才正式启动 L1。

随时可以跑 [`/next-step`](../../.claude/commands/next-step.md) 拿到最新建议。

### Phase 0 dry-run 验收清单

- [ ] 跑 `/audit-docs` —— 应输出当前所有 doc 的状态表，标出 `ghost-guniang` 是占位、所有 mask 缺失、所有 level 缺失
- [ ] 跑 `/next-step` —— 应说"Phase 0 完成，下一步启动 L1，建议先调 `level-planner`"
- [ ] 真的调一次 `level-planner` 子代理 —— 看输出的设计问题清单是否覆盖到 30-level-design.md 模板的所有必填项
- [ ] 试图跑 `/lock-level 01` —— 应失败并列出阻塞项（ghost-guniang TBD / 0 mask / level-01 不存在）。这证明锁机制有效

四步全过 → Phase 0 验收通过 → 进入 Phase 1（L1 设计）。

## 索引

- [roadmap.md](roadmap.md) — 整体路线
- [knowledge-lock-chain.md](knowledge-lock-chain.md) — 每关教什么参数（核心教学曲线）
- [decisions-log.md](decisions-log.md) — 已做的设计决定（不可回退）
- [open-questions.md](open-questions.md) — 待决策清单
- [rules-revisions.md](rules-revisions.md) — 规则层修订流水（改 10/30/40/50 之前必看）
- 关卡执行清单：开关时新建 `level-NN-checklist.md`，关结束后归档不删

## 状态图例

| 标记 | 含义 | 谁能改 |
|---|---|---|
| ⚠️ TBD | 还没开始 / 占位 | 任何人随便改 |
| 🟡 In-Progress | 正在做 | 当前作者改 |
| 🔒 Locked-for-build | 文档冻结，进入实装 | 改要走 [rules-revisions](rules-revisions.md) 流程 |
| ✅ Shipped | 实装通过验证 | 同上 |

每个 ghost / mask / level 的 md 顶部应该有这个状态标记（首行块或 frontmatter）。新建文件时默认 ⚠️ TBD。

## 当前阻塞

由 `/audit-docs` 自动填充。手动写也行：

- L1 启动前提：`ghost-guniang` 卡片 ~30% 完成，需补民族 / 外观 / AI / 失败反应
- L1 启动前提：`doc/51-masks/` 一张面具卡都还没建
- L1 启动前提：`doc/60-levels/` 一关都没写
- 通用：`PhotoEvaluator` 评分算法只有打分表，需要在 L1 设计阶段写 `doc/31-photo-judgment.md`
