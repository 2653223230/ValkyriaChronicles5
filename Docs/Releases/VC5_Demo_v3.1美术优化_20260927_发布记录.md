# VC5 Demo v3.1 美术优化 Android 平板测试版

状态：源码准备封存；Android APK 构建受 Unity 许可证阻断，未生成新版包。推送结果待回填。

## 版本与范围

- 版本名称：v3.1 美术优化；应用版本 `3.1.0`，Android versionCode `4`。
- 应用名沿用 `ValkyriaChronicles5`；图标沿用 `Assets/TcgEngine/Images/VC5/AppIcon.png`。
- 从 Menu.unity 启动，经 VC5 Demo 对战窗口进入 Game.unity；仅包含这两个场景。
- 包含当前六名 2D 立牌、贴身状态图标、战术沙盘/轻量动态背景，以及 Menu 启动缓存修复。
- 只构建 Android 非 Development 测试 APK，不重新发布 Windows，不修改规则、AI 或 P2P。
- 目标文件 `Builds/Direct/Android/VC5_Demo_v3.1_美术优化_20260927_Android.apk`。旧 v3 Tag/版本化发布包保留。

## 提交与回滚安排

工作分支 `main`，先前 HEAD `9f86db2`（v3 构建记录）。本轮提交当前美术实现、对应资源与 .meta、聚焦测试、设计/验证文档及版本设置；不提交无关的 AGENTS.md 修改、企划本地副本、未命名.base 或本地排错截图。源码封存 Tag 拟定 `demo_v3_1_art_20260927`，提交信息 `release: 封存 v3.1 美术优化源码（Android待构建）`。实际提交哈希与 push 结果回填；APK 未生成，其哈希/ABI 待许可证恢复后再补，不移动旧 Tag。源码 Tag 不能当作已发布 APK。

## 本轮构建实际结果

- 图标源 SHA-256 与旧版一致：`25667CF6D8C077AAE0A878851ACF48E1FC2FEE15756882E3CE40B85022BF5FF5`。
- 沙箱首次启动进程未写出构建日志；已结束本任务创建的该进程，不涉及用户编辑器。
- 正常权限批处理首次调用原构建入口：退出码 1，报告 `Error building player because build target was unsupported`，日志 `Builds/Direct/AndroidBuild-v3.1-20260927.log`。
- 显式 `-buildTarget Android` 后退出码 1，日志 `Builds/Direct/AndroidBuild-v3.1-20260927-android-target.log` 明确报告 `No ULF license found / No license activation found for this computer`。Android 模块、SDK/NDK/OpenJDK 均存在；停止重试，需用户在 Unity Hub 恢复许可证并手动打开项目后再构建。
- 新版 APK 没有生成；不能提供新版大小/哈希/manifest/ABI 验证，也不能把现存 v3 APK 标成 v3.1。旧版本化 APK 保留，当前通用 `ValkyriaChronicles5.apk` 也仍是旧包。
- 轻量只读审查未发现所查美术文件的明确 Android 编译阻断；这不是构建成功或设备验证。立牌外观按 Demo 英雄 ID 应用，非完整 P2P 外观验收；非核心资源静态缓存仍沿用原逻辑，本次不扩展修复。

此前验证仅引用 2026-09-26 的 8 项背景检查、6 项立牌资源映射和 1 项启动缓存回归，以及文档内实际场景截图。本轮未重跑玩法套件或设备测试；源码封存不代表 APK 发布完成。

## 验证边界

沿用 2026-09-26 已记录的聚焦测试和实际场景证据，本轮不重复全量玩法测试。只检查构建结果、manifest 的名称/版本/SDK/图标、实际 ABI 与文件哈希。Android 平板安装、触控、旋转、帧率、内存、连续对局均由策划拿包复测，未做的不能写通过。扩展运行时夹具 `Menu_CommanderSelection_AndTemporaryPreview` 未验证、仍不记通过。APK 和日志在被忽略的 Builds 目录，不将包上传 Git；push 指源码和记录。
