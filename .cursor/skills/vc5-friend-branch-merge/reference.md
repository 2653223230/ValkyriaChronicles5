# VC5 朋友分支合并 — 参考

## 标准命令速查

```powershell
# 完整本地测试一轮（不含 Unity 手点）
cd E:/unity/git_newest/ValkyriaChronicles5
git fetch origin
git checkout main && git pull origin main
git tag backup/pre-merge-$(Get-Date -Format yyyyMMdd) -m "pre-merge safety"
git checkout -B pr-test-merge main
git merge --no-commit --no-ff origin/数值修改和机制设计
python Tools/validate_unity_meta_guids.py Assets/TcgEngine/Resources/Cards/vc5
# → 用户开 Unity 验证 → 跑 Vc5SlimeLogicTests
git merge --abort && git checkout main && git branch -D pr-test-merge
```

---

## Known Blockers

### 1. Awakening 命名空间与 GameLogic 冲突

**现象**（合并后 Unity 编译失败）：

```
AIPlayer.cs: error CS1061: 'GameLogic' does not contain a definition for 'GetGameData'
AILogic.cs: error CS1729: 'GameLogic' does not contain a constructor that takes 1 arguments
```

**原因**：工程里同时存在两个 `GameLogic`：

| 类 | 文件 | 命名空间 |
|----|------|----------|
| 真正的游戏逻辑 | `GameLogic.cs` | `TcgEngine.Gameplay` |
| 觉醒扩展（错误） | `AwakeningGameLogic.cs` | `TcgEngine` |

`TcgEngine.AI` 下的代码会解析到错误的空壳类。

**朋友分支修复要点**：

1. `GameLogic.cs` → `public partial class GameLogic`（保持 `TcgEngine.Gameplay`）
2. `AwakeningGameLogic.cs` → `namespace TcgEngine.Gameplay`；`game` 改为 `game_data`
3. `AwakeningSystem.cs` 等加 `using TcgEngine.Gameplay`
4. 补交 `Awakening*.cs.meta`

**验证**：删除 `Library` 后完整重编译；或合并态下检查 `AwakeningGameLogic.cs` 第一行 namespace。

### 2. 非法 `.meta` GUID

Unity 要求 `guid:` 恰好 32 位 hex。朋友批量生成的 vc5 卡牌 meta 曾不合法，会导致资源被忽略。

```powershell
python Tools/validate_unity_meta_guids.py Assets/TcgEngine/Resources/Cards/vc5
python Tools/fix_invalid_unity_meta_guids.py Assets/TcgEngine/Resources/Cards/vc5  # 仅朋友在其分支修
```

### 3. Orphan `.meta`（合并测试遗留）

朋友只提交 `Awakening*.cs` 未提交 `.meta` 时，Unity 打开会生成 untracked meta；`merge --abort` 不删除。

```powershell
git status
git clean -n    # 预览
git clean -fd   # 确认后删除
```

### 4. 黏黏 hero 路径重复（合并后需统一）

- main：`Cards/vc5/heroes/vc5_hero_slime_*`（.asset）
- 朋友分支可能：`Cards/vc5/hero_vc5_slime_*`

合并后需团队统一路径，避免重复英雄资源。

---

## Unity 冒烟清单（详细）

1. Console 0 错误、0 严重警告（meta GUID 除外需为零）
2. MainMenu → 卡组下拉有「黏黏试玩卡组1」
3. 选 `deck_slime_trial_1` 开 Solo/LAN 一局
4. 出牌、上状态、英雄技能无异常弹窗
5. 若合并含龙族/风灵/三大家族：Collection 或 registry 不报错

---

## 规则逻辑自动测试

| 项 | 说明 |
|----|------|
| 入口 | Test Runner → EditMode → `Vc5SlimeLogicTests` |
| 脚本 | `Tools/run_vc5_logic_tests.ps1` |
| 报告 | `TestResults/vc5_logic_latest.json` / `.md` |
|  harness | `Assets/TcgEngine/Scripts/Testing/Vc5LogicTestHarness.cs` |

失败时把 JSON 发给 AI 修 bug，无需复述整局手测过程。

---

## 回退场景

### 场景 A：本地测试失败（PR 未 Merge）

```powershell
git merge --abort
git checkout main
git branch -D pr-test-merge
```

main 与合并前一致；备份标签仍在。

### 场景 B：PR 已 Merge，线上 main 有问题

1. GitHub PR → **Revert**（推荐）
2. 或本地：`git pull && git revert -m 1 <merge-sha> && git push`（需用户授权 push）

### 场景 C：仅想对照合并前代码

```powershell
git checkout backup/pre-merge-YYYYMMDD
# 或
git checkout -b inspect-backup backup/pre-merge-YYYYMMDD
```

---

## 发给朋友的 Blocker 模板（可复制）

```
合并 main（0525代码同步）后 Unity/测试失败：

1. [编译] AwakeningGameLogic 命名空间问题 → 见 reference
2. [GUID] N 个非法 meta → validate_unity_meta_guids.py 输出
3. [逻辑] Vc5SlimeLogicTests X/Y failed → 附 vc5_logic_latest.json

请在 数值修改和机制设计 分支修复并 push，我再重新跑本地 merge --no-commit 测试。
PR 保持 Open，暂不 Merge。
```
