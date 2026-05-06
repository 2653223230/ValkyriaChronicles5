# VC5 Excel导入模板（黏黏卡组）

建议用一个 Excel 文件包含以下 Sheet（这里用 CSV 表示）：

- `heroes.csv`：英雄基础面板与技能绑定
- `cards.csv`：卡牌基础信息与主能力绑定
- `abilities.csv`：能力（触发、目标、费用、次数限制）
- `effects.csv`：效果参数（用于组合基础效果）

## 关键规则映射

- `fast_action=1` 表示快速行动，不占主要行动。
- `uses_per_turn` 为技能每回合使用次数（0=无限制）。
- `mana_cost/hp_cost/discard_cost` 同时支持法力、生命值、弃牌费用。
- `fields` 为字段标签（用 `|` 分隔），例如 `黏黏|快速行动`。

## 导入建议

1. 先导入 `effects.csv`
2. 再导入 `abilities.csv`
3. 最后导入 `heroes.csv` / `cards.csv`

## 当前工程对接

- 运行时已支持：快速行动、主行动限制、技能次数限制、生命/弃牌费用、黏黏核心机制。
- 本模板可直接映射到后续自动导入器（`CardDataImporter` 扩展版）。

## 用 Excel 打开不乱码

本目录 CSV 使用 **UTF-8带 BOM**（Excel 中文版双击打开可识别）。若你本地保存后再次出现乱码，可在仓库根目录执行：

`python Tools/csv_utf8_bom_docs.py`

会重新把 `vc5_excel_seed_v1` 与 `vc5_excel_template` 下全部 `*.csv` 写回 UTF-8 BOM。
