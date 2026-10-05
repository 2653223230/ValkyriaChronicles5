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
- 功能改造提交：`472bf83e7c1d4bba7ddfbefe7a8a91d55a9288b9`。

### 2026-10-05 功能版验证完成
- 版本名：`pve-20261005-card-actor-v1`；功能提交：`472bf83e7c1d4bba7ddfbefe7a8a91d55a9288b9`。
- 提交信息：`feat: unify executor-first card interactions and AP guidance`。
- Unity PlayMode 27/27通过，前阶段规则18/18通过；实际图像和执行结果见 Docs/ArtReviews/2026-10-04-ActorInteraction/README.md。
- 不打包；原修改前标签保留，不覆盖。

### 指定版本操作

- 试玩修改前版本：`git worktree add ../VC5PvE-before-card-actor pve-20261004-before-card-actor`。
- 另开功能版：`git worktree add ../VC5PvE-card-actor-v1 pve-20261005-card-actor-v1`。
- 每个检出的目录都打开其 `Prototypes/VC5PvE`，场景 `Assets/VC5PvE/Scenes/Title.unity`。
- 如需撤销此次交互功能，先保存未提交工作，再在目标开发分支执行 `git revert 472bf83e7c1d4bba7ddfbefe7a8a91d55a9288b9`。发生文档冲突时保留本版本记录；不使用强推覆盖其他修改。
- 本轮只推送开发分支，未合入 main。临时构建缓存和原有无关未跟踪文件未纳入功能提交。

### 远端核对完成（2026-10-05）
功能提交与版本标签已推送到 origin。git ls-remote 核对：分支指向 472bf83e7c1d4bba7ddfbefe7a8a91d55a9288b9，标签 pve-20261005-card-actor-v1 解引用后指向同一提交。随后独立提交本版本记录，不改变功能标签指向。

最终编辑器状态已读回：Title.unity，isPlaying=false，isPaused=false，GameView.maximized=false；控制台错误0条。

