# 先行测试文档 -> Excel 首版数据（v1）

这是一份从 `战场女武神5先行测试英雄与卡牌.docx` 整理出的首版结构化数据。

当前采用 CSV 多表形式（可直接用 Excel 打开，每个 CSV 视为一个 Sheet）：

- `heroes.csv`：英雄（棋子）基础面板与技能文本；**`player_read_text`** 列为给玩家看的英雄说明（【被动】/【技能】/【觉醒】等），与 Unity 英雄 `CardData.text` / `GetDisplayText()` 一致。
- `cards.csv`：卡牌基础信息、费用、字段、基础效果与特殊效果；**`player_read_text`** 列为给玩家看的卡面说明（对应规则文档【效果】+【特殊效果】），与 Unity `CardData.text` / `GetDisplayText()` 一致。CSV 为 UTF-8 BOM，便于 Excel 直接打开。
- `abilities.csv`：能力步骤编排（触发、目标、条件、链路、关联 effect_set）。
- `effects.csv`：效果积木参数（effect_set_id + effect_key + 参数）。
- `decks_sample.csv`：文档中的示例卡组

## 说明

- 本版目标是“先把文档内容结构化落表”，方便你人工校对与后续自动导入。
- `effect_key` 字段是后续对接程序的关键桥梁（对应基础效果模板）。
- 复杂效果暂时保留了 `*_text` 原文描述，下一版会继续拆分成参数化字段。

## 下一步建议

1. 先人工校对 `cards.csv` 中的费用与特殊效果触发条件。
2. 确认无误后，我可以继续把 `effect_key + params` 拆到独立效果表，直接对接导入器。

## 与 Unity 同步文案

- 菜单 **TcgEngine / 卡牌数据导入工具** → **从CSV导入**，选择本目录下的 `cards.csv`。若表头含 `card_id` 与 `player_read_text`，将把 `player_read_text` 写入工程中**已存在**的 `CardData` 资源（`card_id` 会通过映射表对应到运行时黏黏牌的 `id`，例如 `card_slime_heavy` → `vc5_slime_heavy_strike`）。
- **黏黏示例牌**若在工程里没有对应 `.asset`（由 `Vc5SlimeBootstrap` 运行时注册），则导入器无法写入，需同时修改 `Assets/TcgEngine/Scripts/Data/Vc5SlimeBootstrap.cs` 中对应 `CardData.text`，与 `player_read_text` 保持一致。
- 同样可选择 **`heroes.csv`** 导入：表头含 `hero_id` 与 `player_read_text` 时，写入已有英雄 `CardData.text`（`hero_slime_*` 会映射到 `vc5_hero_slime_*`）。运行时注册的英雄需同步改 `Vc5SlimeBootstrap`。
- 对局内悬停/点击**卡牌**或**英雄技能区**时，右侧 `CardDetailPreview` 与卡面文案均使用 `CardData.GetDisplayText()`（优先 `text`）。

## Excel 乱码

若双击 CSV 仍乱码，可在仓库根目录执行 `python Tools/csv_utf8_bom_docs.py`，将本目录与 `vc5_excel_template` 下 CSV 统一写回 **UTF-8 BOM**。
