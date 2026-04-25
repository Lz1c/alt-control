# 41 · 鬼谱条目

> 每只鬼一个 md 文件。规则与模板见 [`../40-ghost-rules.md`](../40-ghost-rules.md)。
> 加新鬼走 `/new-ghost` 斜杠命令，会自动建文件 + 更新本索引。

## 分层视图（设计成熟度）

按数值化阶段分层，便于一眼看清"哪只鬼可以直接进 Unity 实装、哪只还能调"。状态会随关卡推进往左移：占位 → 成熟 → 数值化（Locked）。规则层修改时 🟢 状态会反向回到 🟡（见 [`../plan/rules-revisions.md`](../plan/rules-revisions.md)）。

### 🟢 已数值化（绑定到某关，规则层修改后才能动）

| ID | 绑定关卡 |
|---|---|
| _暂无 — L1 还未 Locked_ | — |

### 🟡 概念已成熟（卡片填得很全，但未绑关，数值仍可调）

| ID | 核心机制 |
|---|---|
| [ghost-yundong-yuan](ghost-yundong-yuan.md) | 校园 L1 Boss · 快门主题综合考试（Phase 0 长曝消形 + Phase 1 多形态摇镜） |
| [ghost-yingzi](ghost-yingzi.md) | 余光/反射 |
| [ghost-xianying](ghost-xianying.md) | 只在 PNG 里显形 |
| [ghost-beimian](ghost-beimian.md) | 只能从反射面拍 |
| [ghost-fenshen](ghost-fenshen.md) | 拍一次多一只 |
| [ghost-xiaomian](ghost-xiaomian.md) | 对焦触发浮现 |
| [ghost-qintong](ghost-qintong.md) | 丑角 · 打破恐怖基调 |

### ⚠️ 概念占位（只有名字 / 半成品）

| ID | 备注 |
|---|---|
| ghost-changyi-gui | L1 扰乱 #1 长椅鬼 · 教基础操作；待 `/new-ghost minion 长椅鬼` 落卡 |
| ghost-fenbi-zi | L1 扰乱 #2 粉笔字鬼 · 教长曝积累；待 `/new-ghost minion 粉笔字鬼` 落卡 |
| ghost-bai-yi-nu | L1 扰乱 #3 白衣女 · 教摇镜法（**强制横向飘动**）；待 `/new-ghost minion 白衣女` 落卡 |
| ghost-chong-pao | L1 扰乱 #4 冲跑小孩 · 教冻结快速运动；待 `/new-ghost minion 冲跑小孩` 落卡 |
| ghost-yexun-canying | L1 扰乱 #5 夜训社团残影群 · 教长曝消形 + Boss Phase 0 启动；待 `/new-ghost minion 夜训残影` 落卡 |

### ❌ 已废弃（保留文件作历史参考）

| ID | 备注 |
|---|---|
| [ghost-guniang](ghost-guniang.md) | 原 L1 Boss 占位，2026-04-25 废弃，被 ghost-yundong-yuan 替代（见 `plan/decisions-log.md` 同日条目） |

## 命名

- 文件名 = 鬼 ID，例如 `ghost-xianying.md`
- ID 规则：`ghost-<拼音 slug>`，小写、中划线

## 当前条目

### Boss 鬼

| ID | 显示名 | 首次出现关卡 | 面具 | 状态 |
|---|---|---|---|---|
| [ghost-yundong-yuan](ghost-yundong-yuan.md) | 运动员鬼 | 01 · 校园之夜 | [mask-dixi-01](../51-masks/mask-dixi-01.md) | 🟡 In-Progress |
| [ghost-guniang](ghost-guniang.md) | 姑娘 | ~~01 · 古巷夜啼~~ | ~~mask-tbd~~ | ❌ 已废弃 |

### 扰乱鬼

#### L1 校园系（5 只，待落卡）

| ID | 显示名 | 出现关卡 | 面具 | 教学功能 |
|---|---|---|---|---|
| ghost-changyi-gui | 长椅鬼 | 01 | 不掉 | 基础操作 / 破冰 |
| ghost-fenbi-zi | 粉笔字鬼 | 01 | 不掉 | 长曝积累（黑板字轨迹） |
| ghost-bai-yi-nu | 白衣女 | 01 | 不掉 | 摇镜法预热（强制横向） |
| ghost-chong-pao | 冲跑小孩 | 01 | 不掉 | 冻结快速运动 |
| ghost-yexun-canying | 夜训社团残影群 | 01 | 不掉 | 长曝消形（Boss Phase 0 启动） |

#### 民俗章节系（待分配章节，归档不删）

| ID | 显示名 | 出现关卡 | 面具 | 核心机制 |
|---|---|---|---|---|
| [ghost-yingzi](ghost-yingzi.md) | 影子 | 待定 | 无 | ⚠️ 占位 · 余光/反射 |
| [ghost-xianying](ghost-xianying.md) | 显影 | 待定 | mask-dejiang-02 | 只在 PNG 里显形 |
| [ghost-beimian](ghost-beimian.md) | 背面 | 待定 | mask-han-02 | 只能从反射面拍 |
| [ghost-fenshen](ghost-fenshen.md) | 分身 | 待定 | mask-gelao-02 | 拍一次多一只 |
| [ghost-xiaomian](ghost-xiaomian.md) | 笑面娃娃 | 待定 | mask-maonan-03 | 对焦触发浮现 |
| [ghost-qintong](ghost-qintong.md) | 秦童 | 待定 | mask-dejiang-05 | 丑角 · 打破恐怖基调 |

> ⚠️ 占位 = 数值/设定还没定，只挂了壳，可被更高优先级的设计覆盖。
