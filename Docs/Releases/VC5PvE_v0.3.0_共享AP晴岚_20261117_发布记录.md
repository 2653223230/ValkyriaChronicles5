# VC5PvE v0.3.0 共享 AP 晴岚 · 双平台发布记录

状态：准备中。2026-11-17 用户明确要求 Windows／Android 构建发布、提交代码并 push，记录发布过程和提交信息。

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
