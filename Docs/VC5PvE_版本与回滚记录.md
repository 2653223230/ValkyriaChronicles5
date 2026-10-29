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

### 2026-10-22 三关成长工作区增量｜尚未提交的开发状态

- 本次沿用现有工程和分支，不新建工程、不打包；未自动提交、推送或新增版本标签。
- 本次只读核实的Git HEAD：`519ea2a4cd36e8cf60623b0667e081c556aa3a0d`（`docs: record verified card interaction release and rollback`）。上述旧功能版/回滚标签保持不变，不代表已包含三关增量。
- 当前三关代码、测试、场景和同步文档位于工作区。具体清单及恢复入口见 `Docs/Plans/2026-10-21-VC5PvE三关成长实施计划.md`。
- 用户已批准营地/回收UI。规则38/38、PlayMode30/30曾通过；之后的跨关障碍清理补丁尚未重新编译/验证，不能标为发布版本。停止Play与刷新请求因自动审批额度限制未执行，当前编辑器状态不能沿用上一节的历史结论。
- 后续应先完成最后补丁复验、较小窗口检查并恢复Title，再按用户授权进行版本提交。不要用reset/checkout覆盖当前用户修改。


### 2026-10-22 选人后目标出牌｜工作区版本

- 当前交互已改为选己方棋子→选牌/基础行动→直接点最终目标；可拖牌至最终目标。旧“拖给执行者”交互不再是现行玩法。
- 文档先行记录于 `Docs/Plans/2026-10-22-VC5PvE选人后目标出牌.md`，随后同步规则与棋子/卡牌设计第14节。保留原三关工作区改动；本轮未新增Git提交、远端推送或回滚标签，不能用旧功能标签声称包含本次实现。
- PlayMode36/36、EditMode38/38通过；最后视觉微调后2/2预览结算补测通过。跨关旧障碍补丁已复验，替代上节“尚未验证”状态。截图及测试任务号在本轮计划。
- 最终编辑器：现有VC5PvE的Title.unity，非Play、非暂停、GameView非最大化，控制台错误0条。MCP重连恢复；旧卡顿未复现，不擅自归因。

### 2026-10-23 收尾核验

最后一处角色名提示改为读取定义后，Unity自动重编译成功（Editor.log退出码0、Runtime程序集更新时间晚于源文件）；详见选人后目标出牌实施计划及其编译证据。没有新增提交/标签/推送。当前Unity已关闭，最后保存Title场景；之前36/38/2项测试结果是2026-10-22运行结果，本次未重复运行。

2026-10-23 MCP重新连接后已直接核对当前Title、非Play/非暂停/非最大化，最终角色名提示可从已加载程序集读回，项目编译未失败。控制台有一条重连前MCP连接失败历史日志，当前连接正常。保留Title用于试玩。

### 2026-10-29 v0.2.0个人AP三关版｜已提交、推送并完成双平台预发布

- 用户明确要求上传当前个人AP三关版本、打包Windows和Android，并再次确认需要提交代码。
- 源码提交：`5e4bd4552dd3e3764f107cd166ed6bc509c7fb96`；分支 `codex/card-actor-interaction-20261004`；标签 `pve-v0.2.0-personal-ap-20261029`。均已推送并通过远端SHA核对，未合入main。
- 本次重新运行EditMode38/38、PlayMode36/36，全部通过。Windows构建成功且启动日志无异常；Android IL2CPP／ARM64构建与APK结构、版本、签名验证通过。两个包已公开预发布，服务端SHA-256与本地一致。Android没有连接设备，未做真机验收。
- 共享AP新方案及单关设计仍未实现，不属于本标签的运行内容。个人AP、现有三关和先选人后目标出牌保持不变。
- 完整过程、参数、安装包校验与最终地址：[v0.2.0发布记录](Releases/VC5PvE_v0.2.0_个人AP三关版_20261029_发布记录.md)。未提交的下一版草稿和无关文件继续保留。
- 回看请在单独目录检出上述标签并打开 `Prototypes/VC5PvE`；不要用reset/强推覆盖当前开发内容。
