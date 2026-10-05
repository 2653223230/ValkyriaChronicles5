# VC5PvE 版本与回滚记录

## 2026-10-04 卡牌执行者交互改造

- 工程：`Prototypes/VC5PvE`；Unity `2021.3.33f1c1`。
- 试玩入口：`Assets/VC5PvE/Scenes/Title.unity`。本轮不打包。
- 修改前版本名：`pve-20261004-before-card-actor`。
- 修改前远程仓库：`origin`，`https://github.com/2653223230/ValkyriaChronicles5`。
- 工作分支：`codex/card-actor-interaction-20261004`。
- 修改前快照范围：独立原型源码、素材、场景、Packages、ProjectSettings、相关规则/设计/验证文档及现有玩家介绍成品。Unity缓存、构建目录、文档临时构建目录不提交。
- 基线行为：中立牌依赖当前选择；专属牌自动选角色；多数牌拖至最终目标松手执行；基础行动菜单AP采用数字文本。此行为是新交互需要对照的回滚点。
- 初始提交/标签/推送结果：创建快照后在本文件后续记录填写完整提交号和远程核对结果；在远程基线确认前不修改游戏代码。

### 安全回看/回滚

先保存当前未提交修改。需要试玩旧版时，优先在另一目录检出该标签：`git worktree add ../VC5PvE-before-card-actor pve-20261004-before-card-actor`，再打开其中的 `Prototypes/VC5PvE`。这样保留当前开发目录。

若未来要让开发分支正式撤销此次功能，使用本文件最终列出的功能提交号执行 `git revert <功能提交号>` 并正常推送。不要使用强制推送或 `reset --hard` 覆盖当前修改。Git标签保存的是源码/资源，不包含Library缓存与未打包的发行文件。

### 修改前快照已上传
- 完整提交：dc9a24385a92cd3b2b07f3ebe98367b8a81adf79。
- 2026-10-04：用户明确授权上述GitHub目标；分支与标签推送均成功。远端分支 codex/card-actor-interaction-20261004，回滚标签 pve-20261004-before-card-actor。
- 功能改造提交：待完成验证后填写。

### 2026-10-05 功能版验证完成
- 版本名：pve-20261005-card-actor-v1；功能提交与远端确认在下一条记录填写。
- Unity PlayMode 27/27通过，前阶段规则18/18通过；实际图像和执行结果见 Docs/ArtReviews/2026-10-04-ActorInteraction/README.md。
- 不打包；原修改前标签保留，不覆盖。
