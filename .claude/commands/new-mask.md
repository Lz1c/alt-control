---
description: ❌ v1 已废止 — v2 没有面具系统。运行此命令会立刻退出。
argument-hint: "(不要运行)"
allowed-tools: "Read"
---

用户输入：`$ARGUMENTS`

## ❌ 此命令已废止（2026-05-01 v2 收紧）

v2 项目改为单关登山治愈版，砍掉了傩戏面具收集机制 / 一鬼一面具系统 / 五色色谱体系。`doc/50-mask-rules.md` 与 `doc/51-masks/` 目录已标 v1 废止。

详见 [`doc/plan/rules-revisions.md` 2026-05-01 v2 cutover](doc/plan/rules-revisions.md)。

## 执行流程

**直接拒绝运行**，并向用户回报：

```
❌ /new-mask 已在 v2（2026-05-01 收紧）废止。

v2 没有面具系统：
- 傩戏面具体系 / 五色色谱 / 一鬼一面具收集 全部砍掉
- 新收集物 = 玩家相册（拍的所有 PNG 自动存盘 + 山顶相册回放）

如果你想：
- 给一只新影建卡 → 跑 /new-ghost（v2 治愈基调登山版）
- 重启面具系统 → 在 doc/plan/rules-revisions.md 起一条草稿先讨论

详见 doc/plan/rules-revisions.md 2026-05-01 v2 cutover 条目。
```

**不要执行任何 Write / Edit 操作**。不要新建任何文件。
