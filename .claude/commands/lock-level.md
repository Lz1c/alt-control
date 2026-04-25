---
description: 把一关的设计文档冻结为 Locked-for-build 状态，进入 Unity 实装。校验所有引用、记录规则层 git SHA。
argument-hint: "[关卡编号，如 01]"
allowed-tools: "Read Grep Glob Edit Bash"
---

用户输入：`$ARGUMENTS`

## 你要做的事

### 1. 解析参数

参数 = 关卡两位数字编号（如 `01`、`12`）。如果没给，先问。

### 2. 校验关卡文档

`Glob` 找 `doc/60-levels/<NN>-*.md`：

- 找不到 → 报错"关 NN 还没建，先跑 `/new-level`"
- 找到 → 读取，对比 `doc/30-level-design.md § 关卡条目模板` 的必填字段
- 任何必填字段含 `TBD` / `⚠️` / 空 → 列出缺字段，**拒绝锁**

### 3. 校验所有引用

把关卡 md 里引用到的 ID 抽出来：

- Boss ghost ID（一般在"Boss"或"主鬼"字段）
- 扰乱鬼 ID 列表
- 面具 ID 列表

对每个 ID：

- 鬼 ID → 找 `doc/41-ghosts/<id>.md`
- 面具 ID → 找 `doc/51-masks/<id>.md`

判定：

- 文件不存在 → **阻塞**
- 文件首行块含 `🔒 Locked` → **通过**
- 文件含 `TBD` / `⚠️` 占位 → **阻塞**，列出来

### 4. 通过 → 落锁（执行四件事）

**a. 取规则层 git SHA**：

用 Bash 跑：

```bash
git log -1 --format=%h -- doc/10-camera-rules.md
git log -1 --format=%h -- doc/40-ghost-rules.md
git log -1 --format=%h -- doc/50-mask-rules.md
git log -1 --format=%h -- doc/30-level-design.md
```

**b. 关卡 md 顶部插入锁定块**（用 Edit，定位到文件首行 `# ` 标题之上）：

```
🔒 Locked: 2026-MM-DD（实际日期）
基于规则层版本：
- 10-camera-rules.md @ <sha>
- 30-level-design.md @ <sha>
- 40-ghost-rules.md @ <sha>
- 50-mask-rules.md @ <sha>

---
```

**c. 同样在引用到的 ghost/mask md 顶部加**（如果还没锁的话）：

```
🔒 Locked: 2026-MM-DD（绑定 L<NN>）

---
```

**d. 在 `doc/plan/decisions-log.md` 顶部"---"之后追加一条**：

```
## YYYY-MM-DD · L<NN> Locked

- 关卡：<显示名>
- Boss：<ghost-id>
- 扰乱：<id 列表，逗号分隔>
- 面具：<id 列表>
- 规则层 SHA：10@<sha> / 30@<sha> / 40@<sha> / 50@<sha>
- 下一步：进 Unity 实装

---
```

**e. 更新 `doc/plan/README.md` 的「当前关卡」字段**：把 "暂无（L1 待启动）" 改成 "L<NN>（实装中）"。

### 5. 不通过 → 列阻塞

按缩进列出每条阻塞，最后一行：

> 修完之后再跑 `/lock-level <NN>`。

## 注意

- 不动 Unity 代码 / scene（实装是后续手动做）
- Locked 不等于不可改。规则层后续若变动，会通过 `doc/plan/rules-revisions.md` 流程显式重新审视这关
- 一次只能锁一关
- 所有的日期用真实当前日期，不要用占位
- Edit 失败（找不到锚点）就回报，不要 `replace_all`
