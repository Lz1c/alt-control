# 策划案 · 目录

> **2026-05-01 v2 收紧后版本**。一个月交作业，单关登山治愈版。
> v1（驱魔师 + 多关 + 傩戏面具）已归档，详见 [`plan/decisions-log.md` 2026-05-01](plan/decisions-log.md) 与 [`plan/rules-revisions.md`](plan/rules-revisions.md)。

像 D&D 规则书一样组织：**规则卷** 定死数值和判定，**条目卷**（关卡 / 影）填内容。

## 分卷

| 卷 | 文件 | 性质 | 何时看 |
|----|------|------|--------|
| 00 | [overview.md](00-overview.md) | 世界观 + 核心循环（v2 治愈版） | 第一次接触项目 / 新人入门 |
| 10 | [camera-rules.md](10-camera-rules.md) | **规则书**：相机参数、EV、测光、拍照判定 | 做相机 / 调数值 / 设计影的拍照条件 |
| 20 | ❌ ~~office-hub.md~~ | **v1 已废止**（办公室 hub 砍） | 不再维护 |
| 30 | [level-design.md](30-level-design.md) | **关卡结构**（v2 单关线性登山） | 改关卡模板时 |
| 40 | [ghost-rules.md](40-ghost-rules.md) | **影规则**：Boss 影 + 扰乱影两种模板、通用字段 | 加新影之前 |
| 41 | [41-ghosts/](41-ghosts/) | 每只影一个 md（v2 = 6 张；v1 旧 8 张归档） | 查 / 写具体影 |
| 50 | ❌ ~~mask-rules.md~~ | **v1 已废止**（傩戏面具体系砍） | 不再维护 |
| 51 | ❌ ~~51-masks/~~ | **v1 已废止**（v2 无面具系统） | 不再维护 |
| 60 | [60-levels/](60-levels/) | 关卡条目（v2 = 唯一关 [`01-shan.md`](60-levels/01-shan.md)） | 改具体关卡时 |
| 70 | [narrative.md](70-narrative.md) | 主角 / 朋友 / 山的叙事骨架 | 写故事文案时 |
| 90 | [90-reference/](90-reference/) | 调研与外部参考（傩戏调研已废止保留作历史） | 加新调研时 |
| plan | [plan/](plan/) | **规划系统**：roadmap / 知识锁链 / 决议日志 / 待决问题 / 规则修订 | **每次开新对话先看 [`plan/README.md`](plan/README.md)** |

## 写作约定

- **规则卷**（10、30、40）用表格和数值，定模板，**不放具体条目**。
- **条目卷**（41 影、60 关卡）每个条目一个 md 文件。文件名 = ID，按对应规则卷的模板填。
- 新增 ≠ 手动 `Write`：影走 `/new-ghost`、关卡走 `/new-level`。斜杠命令会顺带更新索引 README。⚠️ **不要跑 `/new-mask`**（v2 废止）。
- 标注未定：用 `TBD` 或 `⚠️`，方便全局搜索。
- 数值引用代码里的默认值时，**把代码字段名用行内代码标出**，例如 `CAMCOLCameraSettings.isoLimits`，方便之后改代码时反查。
- **术语统一（v2）**：影 / 余像 / 旧时人 / 山的记忆（不再用"鬼" / "驱魔师" / "面具" / "办公室" / "委托"）。但**技术 ID 仍叫 `ghost-*`**，避免破坏数百处引用——这个例外见 [`40-ghost-rules.md § v2 术语`](40-ghost-rules.md)。

## v1 废止内容（保留作历史，不再维护）

下列文件顶部都已加 ❌ 废止 banner。文件本体保留为历史参考。

- `doc/20-office-hub.md`
- `doc/50-mask-rules.md`
- `doc/51-masks/` 目录
- `doc/90-reference/nuo-masks.md`
- `doc/41-ghosts/ghost-yundong-yuan.md` / `ghost-guniang.md` / `ghost-yingzi.md` / `ghost-xianying.md` / `ghost-beimian.md` / `ghost-fenshen.md` / `ghost-xiaomian.md` / `ghost-qintong.md`

## 版本

这是游戏策划案，不是玩家手册。允许粗糙，鼓励写 `TBD` 和占位。每次大改在对应卷顶部加日期 + 变更摘要；项目级方向变动走 [`plan/rules-revisions.md`](plan/rules-revisions.md) 流程。
