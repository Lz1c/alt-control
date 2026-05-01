---
description: 读项目当前进度，告诉用户现在该做什么。3-5 行回答，不改任何文件。（v2 W1-W4 决策树）
argument-hint: "(无参数)"
allowed-tools: "Read Grep Glob"
---

这是一个**只读**导航命令。**不改任何文件**。

## v2 上下文

v2 项目是单关登山治愈版，4 周 W1-W4 里程碑（详见 [`doc/plan/roadmap.md`](doc/plan/roadmap.md)）。

## 你要做的事

### 1. 读上下文

按顺序读：

1. [`doc/plan/README.md`](doc/plan/README.md) — 当前 Phase / 阻塞清单
2. [`doc/plan/roadmap.md`](doc/plan/roadmap.md) — W1-W4 里程碑 + 退出条件
3. [`doc/plan/open-questions.md`](doc/plan/open-questions.md) — 看有没有 🔥 标记的问题
4. [`doc/plan/decisions-log.md`](doc/plan/decisions-log.md) 顶部 — 看上次做了什么

### 2. 判断现状

按以下优先级决定下一步：

1. **W1 设计冻结未完成** → 检查 4 个 🔥 阻塞问题（主角名 / 朋友名 / 山名 / 时长）是否都答了 → 指向第一个未答的
2. **4 个 🔥 都答了，但 `01-shan.md` / 6 张影卡仍 In-Progress** → 提示按答案补齐文档
3. **文档齐全但未 `🔒 Locked`** → 提示跑 `/audit-docs` → `/lock-level 01`
4. **Cairn 资产未导入** → 提示用户导入（这是用户侧任务）
5. **W1 完成 → W2 单点拉通**：段 1 林祠静坐者实装为垂直切片
6. **W2 完成 → W3 全 6 影 + 2 Boss 实装**
7. **W3 完成 → W4 抛光 + 验收**
8. **W4 完成 → 项目交付**

### 3. 输出

3-5 行，告诉用户：

- **现状**：当前在 W?，做到哪一步
- **下一步**：具体动作
- **读**：要看的文件
- **建议**：哪个 agent / slash command 适合下一步（如 `/audit-docs`、`/lock-level 01`、`unity-mcp-skill`）

## 输出格式

```
现状：<一句话>
下一步：<一句话>
读：<文件列表>
建议跑：<工具>（如适用）
```

## 注意

- 不写文件
- 不长篇大论 — 用户跑这个命令是想要"5 秒决定下一步做啥"
- 不重复 README 里已经有的内容 — 解读它，不复述它
- ⚠️ 不要引用 v1 概念（Phase 0 dry-run / level-NN-checklist / 多关 / 办公室 hub）
