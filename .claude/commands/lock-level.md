---
description: 把关卡的设计文档冻结为 Locked-for-build 状态，进入 Unity 实装。校验所有引用、记录规则层 git SHA。（v2 单关版）
argument-hint: "[关卡编号，如 01]"
allowed-tools: "Read Grep Glob Edit Bash"
---

用户输入：`$ARGUMENTS`

## v2 上下文

v2 项目是单关登山治愈版，唯一关 `01-shan.md`。本命令在 v2 下主要用来 lock `01`。`/lock-level 01` 之后进入 W2 单点拉通阶段（详见 [`doc/plan/roadmap.md`](doc/plan/roadmap.md)）。

⚠️ v2 规则层文件 = `10-camera-rules.md` / `30-level-design.md` / `40-ghost-rules.md` / `45-uncanny.md`。`50-mask-rules.md` 已物理删除。

## 你要做的事

### 1. 解析参数

参数 = 关卡两位数字编号（如 `01`）。如果没给，先问。

### 2. 校验关卡文档

`Glob` 找 `doc/60-levels/<NN>-*.md`：

- 找不到 → 报错"关 NN 还没建，先跑 `/new-level`（⚠️ v2 单关下慎用）"
- 找到 → 读取，对比 [`doc/30-level-design.md § 关卡条目模板`](doc/30-level-design.md) 的必填字段
- 任何必填字段含 `TBD` / `⚠️` / 空 → 列出缺字段，**拒绝锁**

### 3. 校验所有引用

把关卡 md 里引用到的影 ID 抽出来：

- Boss 影 ID（一般在 § 影 / Boss 影 字段）
- 扰乱影 ID 列表

⚠️ **不再校验 mask ID**（v2 没有面具系统）。

对每个影 ID：

- 影 ID → 找 `doc/41-ghosts/<id>.md`

判定：

- 文件不存在 → **阻塞**
- 文件首行块含 `🔒 Locked` → **通过**
- 文件含 `TBD` / `⚠️` 占位 → **阻塞**，列出来
- 文件含 `❌ 废止` → **阻塞**（不应该引用 v1 已废止的影）

### 4. 通过 → 落锁（执行四件事）

**a. 取规则层 git SHA**：

用 Bash 跑：

```bash
git -C D:/Unity/Project/alt-control log -1 --format=%h -- doc/10-camera-rules.md
git -C D:/Unity/Project/alt-control log -1 --format=%h -- doc/30-level-design.md
git -C D:/Unity/Project/alt-control log -1 --format=%h -- doc/40-ghost-rules.md
git -C D:/Unity/Project/alt-control log -1 --format=%h -- doc/45-uncanny.md
```

**b. 关卡 md 顶部插入锁定块**（用 Edit，定位到文件首行 `# ` 标题之上）：

```
🔒 Locked: 2026-MM-DD（实际日期）
基于规则层版本：
- 10-camera-rules.md @ <sha>
- 30-level-design.md @ <sha>
- 40-ghost-rules.md @ <sha>
- 45-uncanny.md @ <sha>

---
```

**c. 同样在引用到的影 md 顶部加**（如果还没锁的话）：

```
🔒 Locked: 2026-MM-DD（绑定 L<NN>）

---
```

**d. 在 [`doc/plan/decisions-log.md`](doc/plan/decisions-log.md) 顶部"---"之后追加一条**：

```
## YYYY-MM-DD · L<NN> Locked

- 关卡：<显示名>
- Boss 影：<ghost-id 列表>
- 扰乱影：<id 列表，逗号分隔>
- 规则层 SHA：10@<sha> / 30@<sha> / 40@<sha> / 45@<sha>
- 下一步：进 Unity 实装（W2 单点拉通）

---
```

**e. 更新 [`doc/plan/README.md`](doc/plan/README.md) 的「Phase」字段**：把 "W1 — 设计冻结 + 资产入场" 改为 "W2 — 单点拉通"。

### 5. 不通过 → 列阻塞

按缩进列出每条阻塞，最后一行：

> 修完之后再跑 `/lock-level <NN>`。

## 注意

- 不动 Unity 代码 / scene（实装是后续手动做）
- Locked 不等于不可改。规则层后续若变动，会通过 [`doc/plan/rules-revisions.md`](doc/plan/rules-revisions.md) 流程显式重新审视这关
- 一次只能锁一关
- 所有的日期用真实当前日期，不要用占位
- Edit 失败（找不到锚点）就回报，不要 `replace_all`
- ⚠️ **不要**校验 `mask-*` ID（v2 没有面具系统） / `doc/51-masks/` 与 `50-mask-rules.md` 已物理删除
