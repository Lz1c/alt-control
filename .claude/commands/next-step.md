---
description: 读项目当前进度，告诉用户现在该做什么。3-5 行回答，不改任何文件。
argument-hint: "(无参数)"
allowed-tools: "Read Grep Glob"
---

这是一个**只读**导航命令。**不改任何文件**。

## 你要做的事

### 1. 读上下文

按顺序读：

1. `doc/plan/README.md` —— 当前 Phase / 当前关卡 / 阻塞清单
2. `doc/plan/roadmap.md` —— 路线
3. `doc/plan/open-questions.md` —— 看有没有 🔥 标记的问题
4. `doc/plan/decisions-log.md` 顶部 —— 看上次做了什么
5. （如果当前关有）`doc/plan/level-NN-checklist.md`

### 2. 判断现状

按以下优先级决定下一步：

1. **Phase 0 未完成** → 检查 dry-run 四步是否全过，指向第一个未过的
2. **open-questions 有 🔥 标记** → 先去答这个
3. **当前关 checklist 在某一项** → 指向下一项
4. **当前关 checklist 全部完成 + 还没 Locked** → 提示跑 `/audit-docs` → `/lock-level NN`
5. **当前关已 Locked + 未 verified** → 提示进 Unity 实装阶段
6. **当前关已 verified** → 提示启动 L<NN+1>，先调 `level-planner`

### 3. 输出

3-5 行，告诉用户：

- **现状**：当前在哪
- **下一步**：具体动作
- **读**：要看的文件
- **建议**：哪个 agent / slash command 适合下一步

## 输出格式

```
现状：<一句话>
下一步：<一句话>
读：<文件列表>
建议跑：<工具>（如适用）
```

## 注意

- 不写文件
- 不长篇大论 —— 用户跑这个命令是想要"5 秒决定下一步做啥"
- 不重复 README 里已经有的内容 —— 解读它，不复述它
