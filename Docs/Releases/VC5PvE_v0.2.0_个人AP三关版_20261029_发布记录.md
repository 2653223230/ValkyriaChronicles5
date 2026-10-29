# VC5PvE v0.2.0｜个人 AP 三关版发布记录

状态：Windows／Android 双平台试玩预发布已公开。游戏源码与版本标签已推送；本文件及验证证据作为独立补充提交归档，源码标签不移动。

- [发行页与下载](https://github.com/2653223230/ValkyriaChronicles5/releases/tag/pve-v0.2.0-personal-ap-20261029)
- [源代码提交](https://github.com/2653223230/ValkyriaChronicles5/commit/5e4bd4552dd3e3764f107cd166ed6bc509c7fb96)
- Windows ZIP约62.2MiB，Android APK约52.4MiB。Android为调试签名ARM64试玩包，未做真机测试。

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

### 源码上传

- 代码提交：`5e4bd4552dd3e3764f107cd166ed6bc509c7fb96`，`feat: release personal AP three-stage PvE prototype v0.2.0`，64个文件。
- 已执行 `git push origin codex/card-actor-interaction-20261004`，成功。
- 新建带说明标签 `pve-v0.2.0-personal-ap-20261029` 指向上述提交并推送成功。
- `git ls-remote`核对分支SHA、标签解引用SHA均为上述代码提交。没有合并main或覆盖旧标签。
- `git diff --cached --check`提示新Unity序列化场景／meta的空值行自带尾随空格；保留Unity原始格式。单独检查C#和Markdown无空白问题。代码提交后运行代码及ProjectSettings与HEAD一致。

### Windows构建与启动

- MCP任务 `build-9a20caf65e`：Succeeded，76.06秒，139.48MiB，0错误、6警告。可见编译警告为 `CampaignCampView.tileW/tileH`、`BattleHud.livePreview` 未使用（日志有重复输出）。
- 构建过程中MCP曾断连，随后重连，通过任务状态和Editor.log双重核对成功，没有因失联重复构建。
- 输出 `Prototypes/VC5PvE/Builds/Release-0.2.0/Windows/森钟战术.exe`。使用D3D11、RTX2070Super实际启动，进程保持响应，日志完成引擎／输入初始化、未见Exception/Error。该检查仅证明启动，未自动完成真人操作通关；测试进程已关闭。
- ZIP包含完整运行目录、build-info、试玩说明、第三方许可；排除 `_BurstDebugInformation_DoNotShip` 调试符号目录。ZIP完整性检查通过。
- Windows ZIP：65,247,792字节；SHA-256 `e76842a8b587a9709838b9a5762b00f29fd78f964f1c50cae9cf887e37cc42ed`。
- Android设备查询 `adb devices -l`为空，不声称已真机验收。

### Android首次失败诊断

- 任务 `build-beec911ce2`：失败，198.32秒。两个IL2CPP合并编译单元报 `fatal error: 'string' file not found`，并非C#玩法编译错误。
- 随附NDK `21.3.6528147` 的标准头文件实际存在；最小 `#include <string>` 编译检查通过。随后读取Bee生成的原始命令，使用相同NDK和参数单独编译失败单元，退出码0。
- 上述证据不能归因于代码错误或断言是中文路径问题。保留失败报告与探针日志，先用原配置复建一次，确认是否可复现；不降级到Mono／32位，也不改玩法。
- 首次报告还包含未使用变量、SDK repositories.cfg不存在、UnityEngine.TestRunner.dll未作为Player程序集发现的警告。Player构建未启用IncludeTestAssemblies。

### Android依赖仓库修复（构建环境）

- 第二次任务 `build-406413d308` 已通过原生编译（927个节点评估），首次两个失败单元也经Bee重新编译通过，没有修改C#或降低架构。随后Gradle依赖解析失败：Unity默认 `jcenter()` 重定向至repo1.maven.org，下载返回403。
- 单独请求Maven Central官方端点也返回403，响应明确为当前出口被识别为开放代理；阿里云公共Maven镜像对相同版本POM返回200。本机未找到可直接复用的用户Gradle缓存。
- 处理范围仅为本次E盘Gradle缓存：在 `.cache/release-0.2.0/gradle/init.d/release-repositories.gradle` 添加下列仓库。保留原Unity／AGP／依赖版本、不修改玩法源码或项目模板，不改全局Gradle设置。

```groovy
allprojects {
    buildscript.repositories { maven { url 'https://maven.aliyun.com/repository/public' } }
    repositories { maven { url 'https://maven.aliyun.com/repository/public' } }
}
```

- 后续复现若同样无法访问Maven Central，需在本次指定的GRADLE_USER_HOME下放置此init脚本；网络正常时可直接使用相同版本官方依赖。这个脚本属于工具链下载配置，不是游戏功能。
- 期间自动审批额度不足，一次只读Unity窗口检查未执行；用户随后明确要求继续，恢复后核实Unity没有阻塞对话框、`isBuildingPlayer=false`，第二次构建最终失败。其14975.18秒记录包含长时间等待，不能视作正常打包耗时。此前超时的第三次构建请求没有产生新任务，不声称它已执行。
- 继续后实际启动新任务 `build-1658ece1fc`。已读回GRADLE_USER_HOME仍指向本次E盘缓存，版本仍为0.2.0，玩法源码和已提交PlayerSettings没有变化。

### Android最终构建与验包

- 最终任务 `build-1658ece1fc`：Succeeded，149.50秒，0错误、1警告。UTC 2026-10-29 11:41:57开始、11:44:27完成（北京时间19:41:57至19:44:27）。本次重新编译了先前失败的原生单元，保留IL2CPP／ARM64。
- 唯一报告警告：`Unable to find player assembly: Library/PlayerScriptAssemblies/UnityEngine.TestRunner.dll`。本次非测试Player，不包含测试程序集；未调整玩法或测试代码来隐藏该警告。
- BuildReport总计481,733,260字节包含构建统计，**不是APK下载大小**。实际APK为54,910,154字节。
- APK完整性检查通过：仅有 `arm64-v8a` 原生库，包含 `libil2cpp.so` 和 `level0`／`level1` 两个场景数据；三关仍由运行数据切换。
- aapt读回：包名 `com.vc5pve.senlingtactics`，versionName `0.2.0`，versionCode `2`，minSdk `24`，targetSdk `33`，ARM64。
- apksigner验证通过：APK Signature Scheme v2；调试签名 `C=US, O=Android, CN=Android Debug`。证书SHA-256 `2cbf1b75188e2c67dc67ddffadb1775a007117e31349c899ed1234360cbd7df4`。没有生成或公开正式签名私钥。
- APK SHA-256：`d6182e522d2763d3b621cd6409c1aa6aa5f27ada272a61858edb0a7b9caac467`。
- 详细验包记录：`Builds/Release-0.2.0/Android-manifest-verification.txt`、`Android-signature-verification.txt`、`Android-final-build-report.txt`、`artifact-verification.json`。原失败报告保留，没有覆盖。
- Android构建自动生成默认 `ProjectSettings/BurstAotSettings_Android.json`，源码标签初始不包含该文件；实际生成值将在发布记录补充提交中归档，便于复现。它是构建工具生成的默认配置，本轮没有手动改变Burst行为。游戏源码和PlayerSettings仍与源提交一致。

### 正式公开预发布与最终核对

- GitHub Release ID：`427539178`；北京时间2026-10-29 19:49:29公开（UTC 11:49:29），`draft=false`、`prerelease=true`，未设为正式latest。
- [Windows x64 ZIP](https://github.com/2653223230/ValkyriaChronicles5/releases/download/pve-v0.2.0-personal-ap-20261029/VC5PvE-0.2.0-Windows-x64.zip)
- [Android ARM64 APK](https://github.com/2653223230/ValkyriaChronicles5/releases/download/pve-v0.2.0-personal-ap-20261029/VC5PvE-0.2.0-Android-arm64.apk)
- [SHA256SUMS.txt](https://github.com/2653223230/ValkyriaChronicles5/releases/download/pve-v0.2.0-personal-ap-20261029/SHA256SUMS.txt)
- [第三方许可](https://github.com/2653223230/ValkyriaChronicles5/releases/download/pve-v0.2.0-personal-ap-20261029/THIRD-PARTY-LICENSES.txt)
- 四个附件均为uploaded。Windows／Android／许可逐项核对服务端size与SHA-256；SHA256SUMS自身为286字节，SHA-256 `12c24d5d187038d26c6ca77f6e8101e6cd7c652ca1a9b329b17e9d6cb8636682`。
- 发行页已明确个人AP三关范围、操作方式、调试签名、无存档、未做Android实机验收；未宣传共享AP方案已实现。
- 已恢复编辑器到StandaloneWindows64、Title场景、非Play／非暂停、非编译状态。切平台期间MCP短暂重连，恢复后读回确认。
- 发布记录、README、版本回滚记录、验证证据及自动生成的Android Burst配置作为补充提交；不更改已发布标签指向。该补充提交的完整SHA见Git历史／最终交付消息，避免文件自引用提交号。
- 已保留全部下一版待审核设计、两份混合规则文档的候选入口、灵感改稿及其他原有未跟踪文件；没有reset、clean、强推或合并main。

验证证据归档目录：`Docs/Releases/VC5PvE_v0.2.0_证据/`。构建报告不等于真人平衡、完整自然通关或安卓实机测试；这些仍由试玩验收。

## 复现与回看

- 取得本次标签，在另一工作目录打开其 `Prototypes/VC5PvE`，不要用 reset --hard 覆盖当前开发目录。
- 安装同版本 Unity 和 Windows／Android 模块，使用上述两个场景、记录中的 PlayerSettings 和 MCP 构建参数。
- 本文后续记录实际参数、工具链与结果；构建日志和中间产物位于工程内被忽略的 Logs／Builds／.cache 目录，发行成品附在发行页。

### 本次构建调用

在VC5PvE实例中调用 `manage_build`；Android字段同下，仅替换target及输出路径。不要打开仓库根目录的旧Unity项目进行构建。

```json
{
  "action": "build",
  "target": "windows64",
  "output_path": "E:/unity/git_newest/ValkyriaChronicles5/Prototypes/VC5PvE/Builds/Release-0.2.0/Windows/森钟战术.exe",
  "scenes": "[\"Assets/VC5PvE/Scenes/Title.unity\",\"Assets/VC5PvE/Scenes/ForestRuins.unity\"]",
  "development": "false",
  "options": "[\"detailed_report\"]"
}
```

- Android：`target=android`，`output_path=E:/unity/git_newest/ValkyriaChronicles5/Prototypes/VC5PvE/Builds/Release-0.2.0/VC5PvE-0.2.0-Android-arm64.apk`。
- 用返回的job_id调用 `manage_build(action=status)`核对结果；编辑器重导入导致MCP短暂失联时，先查Editor.log和实例资源，重连后查询原任务，避免重复构建。
- 配置须与本记录一致：非Development、非AAB、现有Title优先；不使用 `include_tests` 或 `auto_run`。

### 上传方式

- GitHub连接器已确认仓库为 `2653223230/ValkyriaChronicles5` 且当前账户可push，但不提供Release附件上传。改用GitHub REST API；凭据来自本机Git凭据管理器，仅进程内使用，不打印、不写入文件。
- 先创建 `draft=true, prerelease=true` 的Release，固定本次tag和源提交；逐个上传ZIP／APK／SHA256SUMS／第三方许可；逐个比对服务端size、state及digest。
- 两个平台包全部验证后，再将该Release改为 `draft=false, prerelease=true, make_latest=false`，最后GET核对发行地址和附件。不更改其他版本，不把此试玩版替代正式latest。
- 本地任务脚本和详细临时日志保留在工程 `.work/release-0.2.0/` 与 `Builds/Release-0.2.0/`，不提交凭据或缓存。
