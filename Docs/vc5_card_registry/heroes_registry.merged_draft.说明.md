# heroes_registry.csv 合并草案说明

## 文件

| 文件 | 用途 |
|------|------|
| `heroes_registry.merged_draft.csv` | **合并后的完整草案**（复制用） |
| `heroes_registry.csv` | 当前 main 正式文件（PR 冲突中勿直接覆盖，先解决冲突） |

## 合并规则（本草案采用）

1. **保留朋友分支**新增英雄（齿轮 / 符文 / 荒野 / 龙族 / 风灵，共 25 行）
2. **保留 main** 的黏黏 5 人（`vc5_hero_slime_*`），含 asset 路径与数值/文案
3. **删除重复**的朋友旧黏黏行（`vc5_slime_blood` 等 + `hero_vc5_slime_*` 路径）
4. **格式**与 main 一致（无全字段引号；多行 `text` 用双引号）
5. 新家族行 `enabled=1`（朋友导出时为空，默认可用；此处显式写 1）

## 如何使用（解决 PR 冲突）

### 朋友在其分支上修（推荐）

```powershell
git checkout 数值修改和机制设计
git fetch origin
git merge origin/main
# 冲突文件：Docs/vc5_card_registry/heroes_registry.csv
# 用 merged_draft.csv 的内容整体替换 heroes_registry.csv
copy Docs\vc5_card_registry\heroes_registry.merged_draft.csv Docs\vc5_card_registry\heroes_registry.csv
git add Docs/vc5_card_registry/heroes_registry.csv
git commit -m "fix: 合并 heroes_registry（新家族 + main 黏黏 asset）"
git push origin 数值修改和机制设计
```

### 或你在本地 pr-test-merge 时修

```powershell
git checkout pr-test-merge
copy Docs\vc5_card_registry\heroes_registry.merged_draft.csv Docs\vc5_card_registry\heroes_registry.csv
git add Docs/vc5_card_registry/heroes_registry.csv
# 若只是本地测试且 --no-commit，不必 commit
```

## 合并后请核对

- [ ] GitHub PR 不再显示 `heroes_registry.csv` 冲突
- [ ] 黏黏卡组仍引用 `vc5_hero_slime_corrosive` / `vc5_hero_slime_hard`（与 `deck_slime_trial_1` 一致）
- [ ] 新家族 `.asset` 路径在合并后的工程里存在
- [ ] Unity：**TcgEngine → 卡牌注册表 → 从 CSV 应用到游戏**（可选验证）

## 未纳入 / 故意删除的行

| id | 原因 |
|----|------|
| `vc5_slime_blood` 等 5 行 | 与 `vc5_hero_slime_*` 重复，且路径规范不同 |
| 朋友分支 `runtime_vc5` 版黏黏 | 已被 main 的 asset 版替代 |
