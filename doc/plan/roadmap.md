# Roadmap

> 一关一关纵切。规则层修改走 [`rules-revisions.md`](rules-revisions.md) 单独流。
> 这份路线随项目演进会被改。每次改记一条到 [`decisions-log.md`](decisions-log.md)。

## Phase 0 · 铺地基（当前）

**目标**：把"一关一关推"的轨道搭好。不接触任何关卡内容。

工作清单：
- [x] `doc/plan/` 六个 md 创建
- [x] `CLAUDE.md` 加 3 节（Design Workflow / Slash Commands / Doc Status Legend）
- [x] `/audit-docs` `/lock-level` `/next-step` 三个斜杠命令
- [x] `level-planner` 子代理
- [x] `doc/41-ghosts/README.md` 加分层视图
- [ ] 跑一遍 dry-run 验收（见 [README.md](README.md)）

**退出条件**：dry-run 四步验证全过。

---

## Phase 1 · L1 纵切片

**目标**：跑通"L1 教快门"最小完整循环（设计 → Unity 实装 → 拍样片验证）。

**候选设定**：
- 知识锁：快门优先
- Boss：`ghost-guniang`（需补 ~70% 内容）
- 扰乱鬼：`ghost-yingzi`（影子，主教学）+ `ghost-qintong`（秦童，调剂）

**子阶段**：

1. **设计阶段**（文档侧）
   - 调 `level-planner` 输入 "L1 / 序章 / 锁=快门"
   - 用 `open-questions.md` 记本关需决定的 5-8 个问题
   - 完成 `ghost-guniang`
   - 抽出本关用到的面具卡（`/new-mask`）
   - 写 `doc/60-levels/01-*.md`（`/new-level`）
   - 补 `doc/31-photo-judgment.md`（PhotoEvaluator 算法规格）
   - 跑 `/audit-docs` 全绿 → `/lock-level 01`
2. **实装阶段**（Unity 侧）
   - 新建 `Assets/_Project/Scenes/level-01.unity`
   - 占位 prefab + Boss 触发器 + PhotoEvaluator 实装
3. **验证阶段**
   - Play Mode 走流程，拍 3 张样片，校准评分曲线
   - 调通 → `decisions-log.md` 记 verified

**退出条件**：从主菜单进 L1 → 拍出 ≥ passingScore 的照片 → 看到通关反馈。

---

## Phase 2..N · 后续关卡

每关都是一次 Phase 1 拷贝。区别只在知识锁链节不同。

**候选锁链节顺序**（不承诺，每关结束后回看修正）：

| 关 | 候选知识锁 | 备注 |
|---|---|---|
| L2 | ISO + 暗光噪点 | 引入 `ghost-xianying` 或 `ghost-xiaomian` |
| L3 | 对焦距离 / 景深 | 引入 `ghost-beimian` |
| L4 | 构图 + 分身 | 引入 `ghost-fenshen`，PhotoEvaluator 加"唯一性"项 |
| L5+ | 闪光 / 长曝积分 | 远期，可能动 `CAMExposureAccumulator` 接入 |

具体关卡数最终由叙事章节决定，不一定是 5 关。

---

## 横切轨道（与关卡平行）

不属于任何单关，但需在某个时机插入：

| 轨道 | 何时启动 | 一句话目标 |
|---|---|---|
| `70-narrative.md` 主线 | L1 实装后、L2 启动前 | 把序章 + 第一章主角动机和章节主题填掉 |
| `20-office-hub.md` 完整化 | L2 结束后 | 反推办公室解锁节奏 + 房间数量 |
| UI/HUD 设计 | L1 实装时同步 | 曝光表 / 对焦距离读数最低可用版 |
| 音频清单 | L2 启动前 | 童谣 / 脚步 / 显形音定一份基础清单 |

何时启动由 `/next-step` 根据本文件触发提示。

---

## 收尾阶段（远期）

- localization（中→英覆盖范围 TBD）
- accessibility 模式（PhotoEvaluator 阈值放宽档）
- 平台移植（PC 是默认，主机是远期）
- 美术抛光（替换 LowPoly 占位资产）
