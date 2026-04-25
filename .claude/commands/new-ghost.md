---
description: 在 doc/41-ghosts/ 新建一个鬼的条目 md，按 40-ghost-rules.md 的模板填。
argument-hint: "[boss|minion] [中文名]"
allowed-tools: "Read Grep Glob Edit Write"
---

用户输入：`$ARGUMENTS`

解析参数：第一个词 = `boss` 或 `minion`，其余 = 鬼的显示名（中文）。如果用户没给，先问一次。

## 你要做的事

1. **读 `doc/40-ghost-rules.md`**，找到对应的模板段落（"Boss 鬼卡模板" 或 "扰乱鬼卡模板"），把整段复制作为起点。
2. **读 `doc/41-ghosts/README.md`**，确认要生成的 `ghost-<slug>` ID 在索引表里不存在。
3. **读 `doc/90-reference/nuo-masks.md` 的§三（造型志）索引**，了解哪些活态体系可用。
4. **问用户最少的必要信息**（不要一次问一大堆，一条一条问或一次 3 条以内）：
   - 体系参考（从 `doc/50-mask-rules.md § 地域 / 体系参考清单` 的 11 个里挑一个，或写"原创混合：A + B"）
   - 核心玩法意图（一句话：比如"需要快门 ≥ 1/500 才能拍清"、"只在余光里出现"、"站着不动凝视 2 秒后消失"）
   - 外观一句话
5. **读 `doc/10-camera-rules.md` 和 `doc/50-mask-rules.md`**，根据玩法意图反算出合理的机械数值：
   - `blur_multiplier`（快速鬼偏大，慢鬼偏小）
   - 推荐快门 / 光圈 / ISO 范围
   - 最低对焦距离
   - 占屏比要求
   - `passingScore`（Boss 60 起步，终章可到 85+；扰乱鬼默认无此字段）
   - 亮度偏好
   - 面具主色（五色体系）
6. **生成鬼 ID**：`ghost-<拼音 slug>`，例如 `ghost-guniang`。
7. **生成面具 ID**：`mask-<体系 slug>-<NN>`。扫一下 `doc/51-masks/README.md` 取下一个序号。
8. **新建文件 `doc/41-ghosts/<ghost-id>.md`**（用 Write 工具；**不要**追加到别的大文件）。模板里的 `###` 调成 `#`（文件级 heading）、`####` 调成 `##`。
9. **在 `doc/41-ghosts/README.md` 的对应表**（Boss 鬼 / 扰乱鬼）插入一行新条目（Edit 工具）。
10. **在 `doc/51-masks/README.md` 的条目表**插入一行占位面具（"未设计"），链回新鬼。
11. **最后回报**：新鬼 ID、面具 ID、核心数值三行摘要，以及"你下一步应该做什么"的提示（比如"面具还没落卡，跑 `/new-mask`；或去 `41-ghosts/<id>.md` 补外观描述"）。

## 注意

- 用户一时说不清的字段可以先填"TBD"，不要瞎编故事。
- `blur_multiplier` 的经验值：站定凝视型 0.5–0.8；正常行走 1.0；快速掠过 2.0–3.0；瞬移/残影 3.0+。
- 面具主色必须落在五色体系里（红/黑/白/青/金），不能出现"粉色""彩虹"这种。
- 如果用户给的体系是"藏传佛教羌姆"，**必须在确认前提醒一下文化敏感风险**（见 `doc/50-mask-rules.md § 三大风险` 第 3 条），让用户明确决定再继续。
