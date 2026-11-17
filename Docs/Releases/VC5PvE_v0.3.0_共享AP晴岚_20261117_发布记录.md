# VC5PvE v0.3.0 共享 AP 晴岚 · 双平台发布记录

状态：本地双平台包完成并验包通过，源码已本地提交；远端push与GitHub发布等待用户确认具体仓库／分支。2026-11-17 用户明确要求 Windows／Android 构建发布、提交代码并 push，记录发布过程和提交信息。

## 发布范围与计划（配置前记录）

- 工程 `Prototypes/VC5PvE`，Unity 2021.3.33f1c1；当前分支 `codex/shared-ap-qinglan-20261029`，发布前 HEAD `039cd56b3878666e26f8ed1834a5b49f448d905f`。
- 版本 0.3.0，标签 `pve-v0.3.0-shared-ap-20261117`；GitHub 2653223230/ValkyriaChronicles5 新建试玩预发布，先 draft、附件校验后公开，保持非 latest。不覆盖个人 AP v0.2.0 标签或包。
- 包仅包含 `Assets/VC5PvE/SharedAP/Scenes/SharedAP_Qinglan.unity`，启动即共享 AP 晴岚单关。不要调用会重建旧场景的 PrototypeBuild.BuildWindows；原个人 AP 默认 Build Settings 场景表保留。
- Windows x64、完整目录 ZIP；Android APK、IL2CPP ARM64、minSdk24、targetSdk33、versionCode3，调试签名试玩包。包名 `com.vc5pve.sharedap` 便于与旧个人 AP 共存，产品名 `晴岚战术 Shared AP`。
- 本地目录 `Prototypes/VC5PvE/Builds/Release-0.3.0-SharedAP/`；沿用 E 盘 release-0.2.0 Gradle 下载缓存和已配置仓库，临时目录置于 E 盘。
- 先检查此前未完成的 D5 自由战斗重开／中途跳过定向用例，再构建和验包、Windows 启动检查、APK manifest／签名／ARM64校验。Android 真机无连接则如实标注。
- 显式提交共享 AP 源码资源、为复用表现修改的公共文件、当前设计总稿、相关开发记录与可审阅证据。排除临时 InitTestScene、截图废片、可执行测试产物、缓存、聊天记录与未知本地副本；不执行 git add . 或强推。
- 源码提交后固定标签和 build-info 的 commit，再构建该源码。发布结果另作补充文档提交，记录 SHA256、文件大小、构建日志、提交号、分支和下载地址。

## 执行记录

待追加实际验证、构建、Git 提交／推送与 GitHub 发布结果。历史测试不作为本次发布验收。

## 构建前核对

- 最新 D5 重开无教学／中途跳过保留战局定向 PlayMode **2/2** 通过：任务 `3c0473b9ced8494c97450cbd336913d4`，0失败。没有用历史回归冒充本次结果。
- 已配置产品名“晴岚战术 共享AP”、版本0.3.0；Android独立包名 `com.vc5pve.sharedap`、versionCode3，可与旧个人AP应用并存。显式单共享场景构建，旧 Build Settings 两场景表不动。
- 构建环境复用旧发布已准备的 E盘 `.cache/release-0.2.0/gradle` 缓存和镜像init，TEMP/TMP指向该缓存 tmp；依赖／SDK版本不变。旧包不覆盖。本次输出 `Builds/Release-0.3.0/`。
- 当前共享AP源码、复用UI适配、两份总稿、实施记录／计划与相关审核素材纳入提交。排除本地临时场景、Assets/Screenshots拍摄残留、RuleChecks.exe、聊天备份、灵感未采纳大段草稿与无关个人文件；它们保留在工作区。

## 当前源码与 Windows 结果

- 源码提交 `f6d7a0fb2bae1dbe7fc9da7b4c3f7de90c7bb895`，标题 `feat: release shared AP Qinglan prototype v0.3.0`，334个文件。源码／配置已固定，历史证据和草稿明确区分；少量原文件EOF空行保留，没有为发布重排旧文档。
- Windows任务 `build-362dd8c7ea` 成功：85.69秒，0错误、5警告，报告139.5MB；ZIP实际65,228,677字节，SHA256 `52edab9ee4e93273490dabe2f2285dee61b8f176003cdced978c3ffe40f102a0`，完整性通过。
- 实际启动 x64 Player，进程响应、Direct3D11／RTX2070Super初始化完成，无启动异常堆栈；日志存在 Unity JobTempAlloc 超过4帧分配警告（age11），记录为已知运行时警告，不能称零警告或已完成通关。
- Android任务 `build-328574063d` 已启动；无ADB设备，未真机验收。
- push 被自动审批拒绝，原因为目的地仓库／分支未明确确认；已向用户提交当前 origin/分支及新预发布目的地确认问题。等待确认时继续本地构建，未绕过拒绝执行外部写入。

## Android 完成与安装包验证

- Android任务 `build-328574063d` 成功：285.80秒、0错误、6警告；报告468.83MB是构建统计，下载包实际 **55,164,046字节**。
- APK ZIP完整性、仅arm64-v8a、`libil2cpp.so`、只有level0单启动场景通过。aapt核实com.vc5pve.sharedap／0.3.0／versionCode3／min24／target33；apksigner v2验证通过。APK SHA256 `68a59c894a8b487382799f5b163ff7f6e37488f91361b2bd600e314c1113def6`。
- 无安卓设备，本次不声称已安装、触屏操作或真人通关通过。Windows仅完成启动检查。
- 编辑器最终读回 SharedAP_Qinglan，isDirty:false、非Play／非构建；默认Build Settings仍为旧Title和ForestRuins，发布只通过显式场景参数选SharedAP。
- [发布证据目录](VC5PvE_v0.3.0_证据/)包含构建任务、manifest／签名、哈希、Windows启动日志、源提交与发布文案；辅助打包／验包／GitHub脚本归档在 `Prototypes/VC5PvE/Tools/Release-0.3.0/`。脚本从系统Git凭据管理器进程内取凭据，不打印、不保存密钥。

## 本地交付路径

- Windows压缩包：`E:/unity/git_newest/ValkyriaChronicles5/Prototypes/VC5PvE/Builds/Release-0.3.0/VC5PvE-SharedAP-0.3.0-Windows-x64.zip`。
- Windows解压运行目录：同目录 `Windows/VC5PvE-SharedAP.exe`（运行时需保留整个Windows目录）。
- Android：`E:/unity/git_newest/ValkyriaChronicles5/Prototypes/VC5PvE/Builds/Release-0.3.0/VC5PvE-SharedAP-0.3.0-Android-arm64.apk`。
- 校验文件和许可证：同目录 `SHA256SUMS.txt`、`THIRD-PARTY-LICENSES.txt`。

## 待远端确认

目标是现有origin `https://github.com/2653223230/ValkyriaChronicles5`，分支 `codex/shared-ap-qinglan-20261029`，新预发布tag `pve-v0.3.0-shared-ap-20261117`。push已被自动审批拒绝，未绕过执行。用户确认后，正常push源码和记录提交、创建draft、上传ZIP/APK/校验和/许可证、核验服务端size/digest后公开预发布，并将最终URL／远端SHA补入记录。
