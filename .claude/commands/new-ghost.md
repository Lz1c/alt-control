---
description: 在 doc/41-ghosts/ 新建一个影的条目 md，按 40-ghost-rules.md 的模板填（v2 治愈基调登山版）。
argument-hint: "[boss|minion] [中文名]"
allowed-tools: "Read Grep Glob Edit Write"
---

用户输入：`$ARGUMENTS`

解析参数：第一个词 = `boss` 或 `minion`，其余 = 影的显示名（中文）。如果用户没给，先问一次。

## v2 上下文（必读）

- 项目是单关登山治愈版，详见 [`doc/00-overview.md`](doc/00-overview.md)
- 关卡 = 一座山，6 段路径，每段一只影。Boss 段固定（段 4 风口 / 段 6 顶峰），其他段都是扰乱
- "鬼" 在 v2 写作"影 / 余像 / 旧时人"。技术 ID 仍叫 `ghost-*`
- ⚠️ **不要读 `50-mask-rules.md` / `90-reference/nuo-masks.md` / `20-office-hub.md`**——已 v1 废止
- ⚠️ **不要更新 `51-masks/README.md`**——v2 没有面具系统

## 你要做的事

1. **读 [`doc/40-ghost-rules.md`](doc/40-ghost-rules.md)**，找到对应的模板段落（"Boss 影卡模板" 或 "扰乱影卡模板"），把整段复制作为起点。
2. **读 [`doc/41-ghosts/README.md`](doc/41-ghosts/README.md)**，确认要生成的 `ghost-<slug>` ID 在 v2 当前条目里不存在。占位 ID（"⚠️ 待建"）可以直接复用。
3. **读 [`doc/plan/knowledge-lock-chain.md`](doc/plan/knowledge-lock-chain.md)**，看 6 段山路对应的相机概念，决定新影补在哪一段。
4. **读 [`doc/60-levels/01-shan.md`](doc/60-levels/01-shan.md)**，确认该段位的环境光 / 已有影 / 教学节奏，避免冲突。
5. **问用户最少的必要信息**（一次问 1-3 条）：
   - 段位（1 林线 / 2 雾带 / 3 岩壁 / 4 风口 Boss A / 5 星脊 / 6 顶峰 Boss B）
   - 故事一句话（这个影是谁 / 何时走过 / 做过什么；**人形 + 身份感 + 时间线索**）
   - 外观一句话（穿什么 / 姿态 / 显形特征）
   - 教学钩子一句话（**必须紧扣段位的相机概念**，不能跑题）
6. **读 [`doc/10-camera-rules.md`](doc/10-camera-rules.md)**，根据段位 + 教学钩子反算合理的机械数值：
   - `blur_multiplier`（静止 0.3-0.5 / 慢移 0.8-1.0 / 快移 2.0+ / 高速横向 2.5+）
   - 推荐快门 / 光圈 / ISO 范围
   - 最低对焦距离
   - 占屏比要求
   - 亮度偏好（极暗 / 暗 / 中 / 亮）
   - `passingScore`：扰乱破冰段 1 = 50；扰乱中段 = 60；Boss A = 65；Boss B = 70
7. **设计朋友笔记本对应页**：
   - 半页（拍糊版）：朋友字迹 80-150 字 + 缺角样片描述。指明这段的相机概念 + 朋友当年怎么拍
   - 完整版（拍达标解锁）：补全朋友的话（温柔关心 / 日记）+ 完整样片描述
   - 调性：克制、实用、温柔（参考是枝裕和兄长视角）。**不戏剧化**
8. **生成影 ID**：`ghost-<拼音 slug>`，例如 `ghost-feng-kou-shan-jun`。中划线分词。
9. **新建文件 [`doc/41-ghosts/<ghost-id>.md`](doc/41-ghosts/)**（用 Write 工具；**不要**追加到别的大文件）。模板里的 `###` 调成 `#`（文件级 heading）、`####` 调成 `##`。
10. **更新 [`doc/41-ghosts/README.md`](doc/41-ghosts/README.md) 的 v2 当前条目表**（Boss 影 / 扰乱影对应段），把"⚠️ 待建"改为"🟡 In-Progress"，或追加新行。
11. **最后回报**：影 ID、段位、教学概念、核心数值三行摘要，以及"下一步"的提示（"把朋友笔记本文案写细一点" / "去 60-levels/01-shan.md 把这只影对应的段位 § 数值同步过去"）。

## 注意

- 用户一时说不清的字段可以先填 "TBD"，不要瞎编故事或民俗
- `blur_multiplier` 的经验值：站定凝视型 0.3–0.5；正常行走 1.0；快速掠过 2.0–3.0；瞬移/残影靠 alpha 衰减替代
- ⚠️ **不要写**民族 / 出处 / `mask_id` / 五色主色 / 傩戏 / 驱魔等 v1 词汇——v2 模板里不存在这些字段
- ⚠️ **不要触碰** `doc/51-masks/` 任何文件
- 调性自查：影是"被山记下来的人"，不是怪物；不主动靠近玩家；拍照后反应温柔（光飘起 / 点头 / 淡出），不凝固崩解
