# VC5 规则逻辑自动测试

本目录存放卡牌规则（伤害、状态、召唤、黏黏机制等）的**自动测试结果**。

## 如何运行

### 方式 A：Unity Test Runner（推荐日常开发）

1. 打开 Unity 工程
2. **Window → General → Test Runner**
3. 切到 **EditMode**
4. 搜索 `Vc5SlimeLogicTests`，点 **Run All**

### 方式 B：命令行（适合 CI / 批量回归）

```powershell
# 先设置 Unity 编辑器路径（按本机安装位置修改）
$env:UNITY_PATH = "C:\Program Files\Unity\Hub\Editor\2021.3.xxxf1\Editor\Unity.exe"

cd E:\unity\git_newest\ValkyriaChronicles5
.\Tools\run_vc5_logic_tests.ps1
```

## 输出文件

每次跑完测试会生成：

| 文件 | 用途 |
|------|------|
| `TestResults/vc5_logic_latest.json` | 机器可读，含每条用例 pass/fail、错误信息、**局面快照** |
| `TestResults/vc5_logic_latest.md` | 人类可读摘要，失败时附 JSON 快照 |
| `TestResults/unity_test_results.xml` | Unity 标准 NUnit 报告（命令行模式） |

## 给 AI 修 Bug 时怎么用

1. 跑测试，若有失败，把 **`vc5_logic_latest.json`** 或 **`.md`** 整份发给 AI
2. 重点看失败项的：
   - `message` / `expected` / `actual`
   - `snapshot.board`（场上单位 HP、状态层数）
3. AI 可据此定位是 `GameLogic`、Effect 还是卡牌配置问题

## 如何新增测试

1. 在 `Assets/TcgEngine/Scripts/Testing/Editor/` 新建测试类，继承 `Vc5LogicTestBase`
2. 用 `Vc5LogicTestHarness.CreateLogic` 建局
3. 用 `PlayCardWithSelects` / `DamageCard` / `AssertStatus` 等断言
4. 参考 `Vc5SlimeLogicTests.cs`

## 设计说明

- **不启动游戏场景**，直接驱动 `GameLogic(true)`，无 UI、无网络
- 使用 `deck_slime_vol01_test` 卡组（全手牌 + 高法力），减少 setup 代码
- 失败时自动保存局面快照到本目录
