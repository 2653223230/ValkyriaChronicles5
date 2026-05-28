# 朋友分支合并与 PR 验证流程（手动操作）

> 适用：将朋友分支 `数值修改和机制设计` 合并进 `main`  
> 仓库：https://github.com/2653223230/ValkyriaChronicles5  
> 原则：**本地只预览合并，正式合并只在 GitHub PR 页面点 Merge**

---

## 流程总览

```
① 打备份标签（合并前安全点）
② 你在 GitHub 创建 PR（先不要 Merge）
③ 本地临时合并（--no-commit，不写进 Git 历史）
④ Unity + 自动测试验证
⑤ 通过 → 你在 GitHub Merge PR → 本地 pull
   失败 → 撤销本地合并，PR 保持 Open，让朋友修
⑥ 日后合并后有问题 → Revert PR 或回到备份标签
```

---

## 第 0 步：合并前准备

在工程根目录打开 PowerShell：

```powershell
cd E:\unity\git_newest\ValkyriaChronicles5

git checkout main
git pull origin main
git fetch origin

git status -sb
```

确认：

- 当前在 `main`，且与 `origin/main` 同步
- 工作区干净（无未提交修改；有的话先 commit 或 `git stash`）

查看朋友分支最新提交：

```powershell
git log -1 --oneline origin/数值修改和机制设计
git log main..origin/数值修改和机制设计 --oneline
```

---

## 第 1 步：打备份标签（强烈建议）

给「现在的 main」打一个永久备份点，以后随时可以回到合并前：

```powershell
# 标签名改成当天日期，例如 backup/pre-merge-20260527
git tag backup/pre-merge-20260527 -m "合并 数值修改和机制设计 前的 main 安全点"
git push origin backup/pre-merge-20260527
```

回退到这一刻：

```powershell
git checkout backup/pre-merge-20260527
# 或新建分支对照
git checkout -b restore-from-backup backup/pre-merge-20260527
```

---

## 第 2 步：你在 GitHub 创建 PR

**这一步必须你本人在网页上完成。**

1. 打开：https://github.com/2653223230/ValkyriaChronicles5/compare  
2. **base** 选 `main`  
3. **compare** 选 `数值修改和机制设计`  
4. 填写标题和说明  
5. 点 **Create pull request**  
6. **保持 Open，先不要点 Merge**

若已有同分支的旧 PR，确认 compare 分支已包含朋友最新 push 即可。

可选：本地预览是否会有文本冲突（不改动工作区）：

```powershell
git merge-tree $(git merge-base main origin/数值修改和机制设计) main origin/数值修改和机制设计 2>&1 | Select-String "CONFLICT"
```

无输出通常表示 Git 文本合并无冲突。

---

## 第 3 步：本地临时合并（不进 Git 历史）

**合并前先关闭 Unity。**

```powershell
cd E:\unity\git_newest\ValkyriaChronicles5

git fetch origin
git checkout main
git pull origin main

git checkout -b pr-test-merge
git merge --no-commit --no-ff origin/数值修改和机制设计
```

| 结果 | 下一步 |
|------|--------|
| `Automatic merge went well` | 继续第 4 步 |
| 出现 `CONFLICT` | 执行下方「冲突处理」，修完或让朋友修后再重来 |
| 已在 `MERGE_IN_PROGRESS` | 先 `git merge --abort`，再从本步重来 |

### 冲突处理

```powershell
git merge --abort
git checkout main
git branch -D pr-test-merge
```

把冲突文件列表发给朋友，在其分支解决后再从第 2 步重测。

---

## 第 4 步：验证（Unity + 可选脚本）

### 4.1 打开 Unity 前（可选静态检查）

**GUID 扫描**（若仓库里有脚本）：

```powershell
python Tools/validate_unity_meta_guids.py Assets/TcgEngine/Resources/Cards/vc5
```

非法 GUID 数量应为 **0**。不为 0 则不要 Merge PR，让朋友修 `.meta` 后再测。

**觉醒命名空间**（合并后常见编译 blocker）：

打开 `Assets/TcgEngine/Scripts/GameLogic/AwakeningGameLogic.cs`，确认：

- 应为 `namespace TcgEngine.Gameplay`（**不是** `namespace TcgEngine`）
- `GameLogic.cs` 应为 `public partial class GameLogic`

若仍是错误命名空间，Unity 里 AI 脚本会报 `GetGameData` / `IsResolving` 等错误，**不要 Merge PR**。

### 4.2 Unity 手动验证清单

1. 打开 Unity 工程，等编译完成  
2. **Console：0 编译错误**  
3. 主菜单能进  
4. 卡组列表正常（如「黏黏试玩卡组1」）  
5. 开一局单机或局域网，能正常出牌  
6. 朋友新增内容（觉醒、新卡包等）无明显崩溃  

### 4.3 规则逻辑自动测试（推荐，比纯手点快）

**方式 A：Unity Test Runner**

1. **Window → General → Test Runner**  
2. **EditMode** 标签  
3. 运行 `Vc5SlimeLogicTests`（Run All）

**方式 B：命令行**

```powershell
$env:UNITY_PATH = "C:\Program Files\Unity\Hub\Editor\<你的版本>\Editor\Unity.exe"
.\Tools\run_vc5_logic_tests.ps1
```

查看报告：

- `TestResults/vc5_logic_latest.json`
- `TestResults/vc5_logic_latest.md`

有失败项 → 附报告给朋友或 AI 修 bug，**不要 Merge PR**。

---

## 第 5 步：根据验证结果二选一

### 5A — 验证通过 → 正式合并

**1. 清理本地临时合并（重要）**

```powershell
cd E:\unity\git_newest\ValkyriaChronicles5

git merge --abort
git checkout main
git branch -D pr-test-merge

git status -sb
```

**2. 清理 Unity 测试遗留（若有）**

`merge --abort` 后可能留下未跟踪的 orphan `.meta`（例如 `Awakening*.cs.meta`）：

```powershell
git status
git clean -n    # 预览会删什么
git clean -fd   # 确认不需要后再执行
```

**3. 在 GitHub 上 Merge PR**

打开你创建的 PR → **Merge pull request** → **Confirm merge**

**4. 本地同步 main**

```powershell
git checkout main
git pull origin main
git log -1 --oneline
```

---

### 5B — 验证失败 → 撤销本地，暂不合 PR

```powershell
git merge --abort
git checkout main
git branch -D pr-test-merge

# 可选清理 orphan 文件
git clean -n
git clean -fd
```

- **PR 保持 Open，不要 Merge**  
- 把 Unity Console 报错、`TestResults/vc5_logic_latest.json` 发给朋友  
- 朋友在其分支修复并 push 后，PR 会自动更新  
- 从 **第 3 步** 重新做本地临时合并测试  

---

## 第 6 步：日后合并后 main 有问题 → 如何撤回

### 方式 1：GitHub Revert PR（推荐，适合已 push 的 main）

在 GitHub 已 Merge 的 PR 页面点 **Revert**，会生成一条撤销合并的 commit。

### 方式 2：本地 git revert

```powershell
git pull origin main
git log --oneline -10          # 找到 Merge PR 的那条 commit
git revert -m 1 <merge-commit-hash>
git push origin main           # 需团队同意后再 push
```

### 方式 3：回到备份标签（本地对照 / 紧急恢复）

```powershell
git checkout backup/pre-merge-20260527
```

一般**不要**对 shared 的 `main` 做 `reset --hard` + force push，除非团队明确同意。

---

## 各阶段对照表

| 阶段 | main 是否变化 | GitHub PR | 能否回到合并前 |
|------|---------------|-----------|----------------|
| 打备份标签 | 否 | 无/ Open | ✅ 标签永久保留 |
| 本地 `--no-commit` 测试 | 否 | Open | ✅ `merge --abort` |
| GitHub Merge PR | **是** | Merged | ✅ Revert 或 checkout 标签 |
| `git pull` 后本地 | 是 | Merged | ✅ 同上 |

---

## 常见问题

### Q：`merge --abort` 后为什么还有 Untracked 的 `.meta`？

朋友分支若只提交了 `.cs` 没提交 `.cs.meta`，Unity 打开时会自动生成 meta。`abort` 只撤销已跟踪文件的合并，**不会删 untracked 文件**。不需要就 `git clean -fd`。

### Q：朋友说他本地能编译，我合并后报错？

常见原因：

1. **Awakening 命名空间写错**（见 4.1）  
2. 朋友未在「合并后的完整工程」里测，或 Unity `Library` 缓存未清  
3. 可让朋友删除 `Library` 后完整重编译对比  

### Q：本地测试时能不能 `git commit` 合并结果？

**不要。** 正式合并只走 GitHub PR。本地 commit 会让历史变乱，且容易和 PR Merge 重复。

### Q：stash 里的东西怎么办？

合并前若有本地文档/脚本未提交：

```powershell
git stash list
git stash pop    # 需要时再恢复
```

---

## 一键命令速查（复制用）

```powershell
# --- 合并前 ---
cd E:\unity\git_newest\ValkyriaChronicles5
git checkout main && git pull origin main && git fetch origin
git tag backup/pre-merge-YYYYMMDD -m "pre-merge safety"
git push origin backup/pre-merge-YYYYMMDD

# --- 本地临时合并（Unity 先关） ---
git checkout -b pr-test-merge main
git merge --no-commit --no-ff origin/数值修改和机制设计

# --- 验证通过后清理 ---
git merge --abort && git checkout main && git branch -D pr-test-merge

# --- GitHub Merge PR 后 ---
git pull origin main
```

---

## 相关文档

- 合并 Skill（给 Agent 用）：`.cursor/skills/vc5-friend-branch-merge/SKILL.md`
- 规则自动测试：`TestResults/README.md`
- GUID 问题说明：`Docs/Unity_meta_GUID无效问题修复与预防.md`（若存在）
