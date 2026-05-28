---
name: vc5-friend-branch-merge
description: >-
  Guides VC5 (ValkyriaChronicles5) Git merge workflow: backup tag, user-created PR
  from friend branch 数值修改和机制设计 into main, local --no-commit merge test,
  Unity compile/smoke/logic tests, merge or abort/revert. Use when merging friend
  branches, pre-merge Unity validation, PR before merge, merge --abort, or rollback
  after bad merge.
---

# VC5 朋友分支合并 + PR + Unity 验证

## 角色分工

| 步骤 | 谁做 |
|------|------|
| 打备份标签、本地临时合并、跑测试、写验证报告 | Agent / 用户本地命令 |
| **在 GitHub 创建 PR** | **用户本人**（Agent 不代提 PR） |
| **在 GitHub Merge / Revert PR** | **用户本人**（Agent 可给命令与检查项） |
| `git push` 到 remote | 仅用户明确要求时 |

## 仓库常量

- 远程：`origin` → `https://github.com/2653223230/ValkyriaChronicles5`
- 集成分支：`main`（用户主线，含黏黏 0525 等）
- 朋友功能分支：`origin/数值修改和机制设计`
- 工程根目录：`E:/unity/git_newest/ValkyriaChronicles5`

## 核心原则

1. **正式合并只在 GitHub PR 页面完成**；本地只用 `merge --no-commit --no-ff` 预览。
2. **本地测试禁止 `git commit` 合并结果**。
3. **合并前必须打备份标签**，便于回退到「合并前时刻」。
4. PR **由用户自己在 GitHub 网页创建**；Agent 只协助检查 diff、跑本地验证、汇总 blocker。

---

## 阶段 0：合并前安全点

```powershell
cd E:/unity/git_newest/ValkyriaChronicles5
git checkout main
git pull origin main
git fetch origin

# 标签名带日期，避免覆盖旧备份
git tag backup/pre-merge-YYYYMMDD -m "合并 数值修改和机制设计 前的 main 安全点"
git push origin backup/pre-merge-YYYYMMDD
git log -1 --oneline main
git log -1 --oneline origin/数值修改和机制设计
```

确认工作区干净：`git status -sb` 应无未提交改动（有则先 stash 或 commit，并告知用户）。

---

## 阶段 1：用户创建 PR（GitHub 网页）

Agent **不**调用 `gh pr create`，提示用户：

1. 打开 https://github.com/2653223230/ValkyriaChronicles5/compare
2. **base** = `main`，**compare** = `数值修改和机制设计`
3. 填写标题/说明，**Create pull request**
4. **保持 Open，不要 Merge**

若已有同分支 PR，确认 compare 已包含朋友最新 push。

Agent 可协助：`git log main..origin/数值修改和机制设计 --oneline`、预览冲突：

```powershell
git merge-tree $(git merge-base main origin/数值修改和机制设计) main origin/数值修改和机制设计 2>&1 | Select-String "CONFLICT|changed in both"
```

---

## 阶段 2：本地临时合并（不进 Git 历史）

```powershell
cd E:/unity/git_newest/ValkyriaChronicles5
git fetch origin
git checkout main
git pull origin main

git checkout -B pr-test-merge main
git merge --no-commit --no-ff origin/数值修改和机制设计
```

| 结果 | 动作 |
|------|------|
| `Automatic merge went well` | 进入阶段 3 |
| `CONFLICT` | `git merge --abort` → 列出冲突文件 → **停止**，让用户/朋友解决后再测 |
| 已在 `MERGE_IN_PROGRESS` | 先 `git status`，再决定 `merge --abort` 重试或继续测 |

**合并前关 Unity**；合并完成后再打开，避免 `.meta` 混乱。

---

## 阶段 3：自动化验证（Agent 应主动执行）

按顺序执行，结果写入验证摘要。

### 3.1 GUID 扫描（有脚本时）

```powershell
python Tools/validate_unity_meta_guids.py Assets/TcgEngine/Resources/Cards/vc5
```

非法 GUID > 0 → **Blocker**，不可 Merge PR。

### 3.2 命名空间 / 编译风险（合并态下静态检查）

检查觉醒扩展是否与 `TcgEngine.Gameplay.GameLogic` 冲突：

```powershell
git show HEAD:Assets/TcgEngine/Scripts/GameLogic/AwakeningGameLogic.cs 2>$null | Select-Object -First 12
git show HEAD:Assets/TcgEngine/Scripts/GameLogic/GameLogic.cs 2>$null | Select-Object -First 15
```

**已知 Blocker**：`AwakeningGameLogic.cs` 在 `namespace TcgEngine` 声明 `partial class GameLogic`，会导致 `TcgEngine.AI` 下 AI 脚本报 `GetGameData`/`IsResolving` 等 CS1061。正确做法见 [reference.md](reference.md#known-blockers)。

### 3.3 Unity 编译与冒烟（用户操作，Agent 给清单）

用户打开 Unity 后检查：

- [ ] Console **0 编译错误**
- [ ] 主菜单可进
- [ ] 卡组列表正常（含 `deck_slime_trial_1`）
- [ ] 能开一局单机/局域网
- [ ] 黏黏试玩无明显崩溃

合并测试可能留下 orphan `.meta`（如 `Awakening*.cs.meta`），`merge --abort` 不会删 untracked；测后 `git status`，不需要则 `git clean -n` / `git clean -fd`。

### 3.4 规则逻辑自动测试（推荐）

```powershell
# Unity Test Runner → EditMode → Vc5SlimeLogicTests → Run All
# 或命令行：
$env:UNITY_PATH = "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe"
.\Tools\run_vc5_logic_tests.ps1
```

查看 `TestResults/vc5_logic_latest.json`：failed > 0 则 **Blocker** 或需用户确认。

---

## 阶段 4：决策

### 4A 验证通过 → 用户 Merge PR

Agent 执行本地清理：

```powershell
git merge --abort
git checkout main
git branch -D pr-test-merge
git status -sb
# 可选：git clean -fd
```

提示用户：去 **GitHub PR 页 → Merge pull request → Confirm**。

用户 Merge 后本地同步：

```powershell
git pull origin main
```

### 4B 验证失败 → 保持 Open，撤销本地

```powershell
git merge --abort
git checkout main
git branch -D pr-test-merge
```

- PR **保持 Open，不要 Merge**
- Agent 输出 **Blocker 报告**（见下方模板），用户转发给朋友
- 朋友修分支 push 后，PR 自动更新 → 从阶段 2 重测

---

## 阶段 5：合并后出问题 → 回退

| 方式 | 适用 | 操作 |
|------|------|------|
| **GitHub Revert PR**（推荐） | 已 Merge 到 main | PR 页 **Revert** |
| **git revert** | 本地/远程 main | `git revert -m 1 <merge-commit>` |
| **备份标签** | 本地对照/紧急恢复 | `git checkout backup/pre-merge-YYYYMMDD` |

不要在未获用户同意时对 `main` 做 `reset --hard` + force push。

---

## Agent 验证报告模板

每次本地合并测试结束后，输出：

```markdown
## VC5 合并验证报告

- 日期：
- main @：
- 朋友分支 @：
- 本地状态：pr-test-merge / MERGE_IN_PROGRESS / 已 abort

### Git 合并
- [ ] 无文本冲突
- [ ] merge --abort 可回到干净 main

### 静态检查
- [ ] vc5 卡牌 meta GUID：非法 N 个
- [ ] Awakening 命名空间：通过 / 失败

### Unity（用户确认）
- [ ] 编译 0 错误
- [ ] 冒烟测试通过

### 自动规则测试
- [ ] vc5_logic_latest.json：X/Y passed

### 结论
- **可 Merge PR** / **不可 Merge（Blocker 如下）**

### Blockers
1. ...

### 建议下一步
- ...
```

失败时附上 Unity Console 关键错误、`TestResults/vc5_logic_latest.md` 路径。

---

## Agent 行为约束

- **不要**在本地 `git commit` 合并结果
- **不要**未经用户要求 `git push`（备份标签 push 需用户同意）
- **不要**代用户创建 GitHub PR
- **要**在合并测试前确认 `main` 干净、已 fetch
- **要**提醒用户：朋友分支只提交 `.cs` 未提交 `.meta` 时，Unity 会生成 orphan meta，abort 后需清理
- **要**合并前建议跑 `Vc5SlimeLogicTests`，比纯手点效率高

## 附加资源

- 已知问题与朋友分支修复说明：[reference.md](reference.md)
- 规则测试说明：`TestResults/README.md`
- GUID 文档：`Docs/Unity_meta_GUID无效问题修复与预防.md`（若存在）
