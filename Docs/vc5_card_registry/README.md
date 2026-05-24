# VC5 卡牌 / 英雄注册表（CSV）

与游戏中 `CardData.id` **一一对应**的主数据清单。改 CSV 后进入 Play 或执行菜单应用，即可删减卡牌或改数值。

## 文件

| 文件 | 内容 |
|------|------|
| `heroes_registry.csv` | `CardType.Hero`，或 id 以 `vc5_hero_` 开头的出战英雄 |
| `cards_registry.csv` | 其余卡牌（含法术、随从、黏黏 runtime 卡等） |

## 常用操作

1. **首次或新增资源后**：Unity 菜单 **TcgEngine → 卡牌注册表 → 从游戏导出到 CSV**（以游戏内数据为准，保留已有行的 `enabled` / `notes`）。
2. **删卡（软删）**：将对应行的 `enabled` 改为 `0`，保存 CSV，再 **Play** 或 **从 CSV 应用到游戏**。
3. **改数值 / 名称 / 文案**：改 `mana`、`attack`、`hp`、`text` 等列，保存后 Play 或应用菜单；带 `asset_path` 的行会写回 `.asset`。
4. **打开文件夹**：**TcgEngine → 卡牌注册表 → 打开注册表文件夹**。

## 列说明

| 列 | 说明 |
|----|------|
| `enabled` | `1` 启用，`0` 从游戏中隐藏（不删磁盘资源） |
| `id` | 引擎 id，勿改（与 `CardData.id` 一致） |
| `title` / `series` / `type` | 显示名、系列、类型名（Hero/Character/Spell…） |
| `mana` … `attack_Range` | 费用与身材 |
| `fast_action` / `deckbuilding` | `0` 或 `1` |
| `text` | 卡面说明（`CardData.text`） |
| `source` | `asset` 或 `runtime_vc5`（导出时自动填，勿手改） |
| `asset_path` | 资源路径；`runtime_vc5` 为空 |
| `notes` | 备注，导出时保留 |

## 与策划表关系

- `Docs/vc5_excel_seed_v1/` 仍用于黏黏 **能力效果**（abilities/effects）；本注册表管 **是否在局内出现** 与 **基础面板**。
- 加载顺序：`CardData.Load` → `Vc5SlimeBootstrap`（含 seed CSV）→ **本注册表**（最后生效）。

## 命令行导出（无 Unity）

```bash
python Tools/export_card_registry.py
```

新增卡牌实现后请同步：**导出到 CSV** 或手动在对应表中加一行。
