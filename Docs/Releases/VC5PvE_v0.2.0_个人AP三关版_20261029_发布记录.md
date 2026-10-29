# VC5PvE v0.2.0｜个人 AP 三关版发布记录

状态：发布执行中。只有下面填写了实际结果的步骤才能视为完成。

## 授权与版本范围

- 2026-10-29 用户明确要求：先上传当前“分别使用 AP＋三关卡”版本，记录发布过程，打包发布 Windows 和安卓；再次确认“代码记得提交”。本次允许提交、推送、创建新标签和上传发行包。
- 工程：`Prototypes/VC5PvE`；Unity `2021.3.33f1c1`；远端 `https://github.com/2653223230/ValkyriaChronicles5`。
- 沿用分支 `codex/card-actor-interaction-20261004`，不合入 main、不强推、不覆盖旧标签。
- 发布前 HEAD：`519ea2a4cd36e8cf60623b0667e081c556aa3a0d`。当前三关及选人后出牌代码是既有工作区增量，本次归档发布。
- 实现范围：三名角色分别持有个人 AP（3／2／2）；三关战斗、两次营地、成长与回收；选己方棋子，再选卡／基础行动，最后点目标，支持拖卡到最终目标。
- 共享 AP、卡牌拖给执行棋子、免费短步、中立治疗新卡、“晴岚炮台”单关均属于下一版设计。本次不实现，也不声称发行包已包含它们。
- 保留未提交文件和待审核设计；不提交临时测试场景、缓存、聊天记录、未知本地副本、RuleChecks.exe。

## 构建计划（先记录，后配置）

1. 在已连接的 VC5PvE 编辑器重跑 EditMode 和 PlayMode。历史测试仅作参考。
2. 固定发行版本 `0.2.0`。仅修改必要的 PlayerSettings，游戏规则及 UI 不在本轮改动。
3. 场景仅使用现有 `Assets/VC5PvE/Scenes/Title.unity` 和 `Assets/VC5PvE/Scenes/ForestRuins.unity`。三关由数据切换；CampaignReview 只用于审阅，不进入包。
4. 使用 Unity MCP `manage_build` 直接构建。**不要调用旧 `PrototypeBuild.BuildWindows`，它会重新生成场景并把版本写回 0.1.0。**
5. Windows x64 普通发行构建，输出到版本专属目录；打包整个目录为 ZIP，保留 UnityPlayer.dll 和 Data 等依赖。
6. Android 输出可直接安装的 APK，应用标识 `com.vc5pve.senlingtactics`、versionCode 2。优先 IL2CPP＋ARM64，若工具链不支持，记录实际阻碍再决定。使用 Unity 随附 SDK／NDK／JDK，临时及 Gradle 缓存在 E 盘。本次为试玩预发布，未提供正式签名密钥，使用调试签名并明确标注；不发布到应用商店。
7. 源码及发布文档显式选取后提交、推送；新标签 `pve-v0.2.0-personal-ap-20261029` 固定实际构建的源码提交。随后记录包大小、SHA-256、构建报告、测试结果和发布地址；补充文档单独提交。
8. 发行页标为预发布，说明 Android 实机验证状态。禁止把构建成功当作实机通关证明。

## 实际过程与结果

- 编辑器实例：`VC5PvE@c0708fa7e3f45dfe`，已读回工程 Assets 路径确认。
- EditMode：38/38，0失败／0跳过；任务 `7604d35081064707b7a9d6974a183a55`。
- PlayMode：36/36，0失败／0跳过；任务 `cb262975cb944214a7b871832057f54b`。以最终 summary 为准，中间 progress.total 的38是工具进度字段，不作为测试数。
- 测试后控制台错误0条。测试包含人工缩减条件的流程 fixture，不代表真人自然通关或手机性能验收。
- 配置已保存：版本0.2.0；Android IL2CPP、ARM64、minSdk24、targetSdk33、versionCode2、固定左横屏；无自定义签名。
- Android 模块、SDK／NDK／OpenJDK均已安装，Unity读回支持 Windows64 和 Android。
- 工具链根目录：`D:/unity/安装位置/2021.3.33f1c1/Editor/Data/PlaybackEngines/AndroidPlayer/`；SDK已有API29、30、33。
- 编辑器进程的 TEMP／TMP、GRADLE_USER_HOME、ANDROID_USER_HOME／ANDROID_SDK_HOME已指向工程 `.cache/release-0.2.0/`；ANDROID_NDK_ROOT指向随附NDK。注意已启动编辑器的 Mono `Path.GetTempPath()`仍缓存旧C盘路径，不能据环境变量声称所有编辑器临时文件已迁移。主要构建输出和Gradle缓存使用E盘。
- 配置脚本第一次编译因枚举命名空间写错未执行任何赋值；反射确认后用 `UnityEditor.UIOrientation` 成功保存。游戏脚本未改动。
- 源码快照显式纳入既有 VC5PvE 实现、测试、审阅场景、10月22日实施计划和UI证据。两份混合规则文档仅暂存已实现部分，10月29日候选入口保留在工作区；新版设计文档、灵感草稿和无关文件不随本次提交。

待执行后填写：源码提交／远端核对、双平台构建、包结构／运行验证、SHA-256、上传与发行页核对。

## 复现与回看

- 取得本次标签，在另一工作目录打开其 `Prototypes/VC5PvE`，不要用 reset --hard 覆盖当前开发目录。
- 安装同版本 Unity 和 Windows／Android 模块，使用上述两个场景、记录中的 PlayerSettings 和 MCP 构建参数。
- 本文后续记录实际参数、工具链与结果；构建日志和中间产物位于工程内被忽略的 Logs／Builds／.cache 目录，发行成品附在发行页。
