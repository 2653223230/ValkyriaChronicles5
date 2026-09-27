# VC5 Demo v3.1 美术优化 Windows / Android 测试版

状态：源码与两个 v3.1 Tag 已推送至 origin；Windows / Android 均已构建成功并完成包信息检查，待真人/设备验收。

## 版本与范围

- 版本名称：v3.1 美术优化；应用版本 `3.1.0`，Android versionCode `4`。
- 应用名沿用 `ValkyriaChronicles5`；图标沿用 `Assets/TcgEngine/Images/VC5/AppIcon.png`。
- 从 Menu.unity 启动，经 VC5 Demo 对战窗口进入 Game.unity；仅包含这两个场景。
- 包含当前六名 2D 立牌、贴身状态图标、战术沙盘/轻量动态背景，以及 Menu 启动缓存修复。
- 2026-09-27 策划追加 Windows；同时构建 Windows x64 与 Android 非 Development 测试包，不修改规则、AI 或 P2P。
- 目标文件 `Builds/Direct/Android/VC5_Demo_v3.1_美术优化_20260927_Android.apk`。旧 v3 Tag/版本化发布包保留。

## 提交与回滚安排

工作分支 `main`，先前 HEAD `9f86db2`（v3 构建记录）。本轮提交当前美术实现、对应资源与 .meta、聚焦测试、设计/验证文档及版本设置；不提交无关的 AGENTS.md 修改、企划本地副本、未命名.base 或本地排错截图。源码封存 Tag `demo_v3_1_art_20260927` 保持原指向。构建记录提交信息 `release: 记录 v3.1 美术优化 Windows 与 Android 成功构建`，构建记录 Tag `demo_v3_1_art_20260927_build1`；该提交哈希以 `git rev-parse demo_v3_1_art_20260927_build1^{commit}` 解析。旧 Tag 不移动；包与源码对应关系见下方实际结果。

## 本轮构建实际结果

- Windows MCP 作业 `build-3ffff922e6`：Succeeded，72.2806923 秒，0 errors / 3 warnings，报告 239.26 MB。状态查询曾超时，未重复触发构建，随后取得最终成功状态。
- Windows x64 ZIP：`Builds/Direct/VC5_Demo_v3.1_美术优化_20260927_Windows_x64.zip`，`99,073,959` 字节；SHA-256 `DEAB51311DEB8CB963ADE641C038F8973AC6BDC32A7C36FA6E7AE1EF718618C9`。ZIP 回读 160 项，`DoNotShip` 调试目录 0 项。非 Development，仅 Menu / Game 两场景；EXE 文件版本字段是 Unity 引擎版本，不能将其当作应用 bundleVersion。
- Android MCP 作业 `build-810873c976`：Succeeded，164.2990654 秒，0 errors / 11 warnings，报告 318.9 MB（不是 APK 文件大小）。APK `Builds/Direct/Android/VC5_Demo_v3.1_美术优化_20260927_Android.apk`，`92,289,394` 字节；SHA-256 `9C52F5D611DB8CD49368422CEF7F2BB5AECF901A49A6205C878B53D8E15A9D09`。不覆盖旧版封存 APK。
- aapt 实际读取：包名 `com.IndieMarc.TcgEngine`，应用名 `ValkyriaChronicles5`，versionName `3.1.0` / versionCode `4`，minSdk `24` / targetSdk `33`，六档图标条目，UnityPlayerActivity，横屏 `userLandscape`。沿用 Mono 后端，实际 ABI **仅 `armeabi-v7a`**；不适用于仅支持 64 位应用的设备。未宣称 APK 内图标像素比对或平板运行通过。

### 续接构建安排

策划已启动 Unity/MCP。当前编辑器通过 MCP 确认 Windows64 与 Android target 均支持，版本 `3.1.0` / versionCode `4`；直接在当前编辑器异步构建，前次批处理许可证问题不再阻断本轮构建。不关闭用户编辑器，不增加测试套件。Windows 新目录 `Builds/Direct/Windows/VC5_Demo_v3.1_20260927/ValkyriaChronicles5.exe`，分发 ZIP 如上；APK 使用上述版本化路径。前次失败记录保留，当前成功构建日志 `Builds/Direct/VC5-v3.1-20260927-editor-build.log`。随后策划明确确认远端，推送结果见下。

## 本地提交与推送实际状态

- 源码提交：`e3576305ce62178b78f88cc076e6fdbe917e0025`；提交信息 `release: 封存 v3.1 美术优化源码（Android待构建）`，80 个相关文件。
- 本地 annotated Tag：`demo_v3_1_art_20260927`，指向上述源码提交；Tag 注释明确 Android APK 等待许可证激活。未移动 v3/v2 旧 Tag。
- 2026-09-27 请求 push `main` 与该 Tag 时被安全审核拒绝，原因是完整代码/资源 payload 的外部目的地尚未获得审核所需的明确确认。命令未执行，不能称为已 push。
- 2026-09-27 用户明确确认后，`git push --atomic origin main demo_v3_1_art_20260927 demo_v3_1_art_20260927_build1` 退出码 0：远端 main 从 `9f86db2` 更新至 `8b602bc58fd8e4f7196b6c43d8f7248959160895`，两个 Tag 均新建成功。目的地 `https://github.com/2653223230/ValkyriaChronicles5`；APK/ZIP 不上传 Git。
- 构建记录提交：`8b602bc58fd8e4f7196b6c43d8f7248959160895`，信息 `release: 记录 v3.1 美术优化 Windows 与 Android 成功构建`。随后以 `docs: 同步 v3.1 推送完成记录` 提交并推送本条状态回填，不移动封存 Tag。
- 本条状态回填形成后续文档提交，未改变源码或包内容。源码定位以源码提交/Tag 为准；构建产物以版本化路径和 SHA-256 为准。

## 历史构建失败与验证边界

- 图标源 SHA-256 与旧版一致：`25667CF6D8C077AAE0A878851ACF48E1FC2FEE15756882E3CE40B85022BF5FF5`。
- 沙箱首次启动进程未写出构建日志；已结束本任务创建的该进程，不涉及用户编辑器。
- 正常权限批处理首次调用原构建入口：退出码 1，报告 `Error building player because build target was unsupported`，日志 `Builds/Direct/AndroidBuild-v3.1-20260927.log`。
- 历史批处理显式 `-buildTarget Android` 后退出码 1，日志 `Builds/Direct/AndroidBuild-v3.1-20260927-android-target.log` 报告 `No ULF license found / No license activation found for this computer`。当时停止重试；用户启动编辑器后，本轮通过 MCP 构建成功，不删除历史失败证据。
- 新版版本化 APK 已生成，旧版本化 APK 保留，通用 `ValkyriaChronicles5.apk` 未替换，仍是旧包。
- 轻量只读审查未发现所查美术文件的明确 Android 编译阻断；这不是构建成功或设备验证。立牌外观按 Demo 英雄 ID 应用，非完整 P2P 外观验收；非核心资源静态缓存仍沿用原逻辑，本次不扩展修复。

此前验证仅引用 2026-09-26 的 8 项背景检查、6 项立牌资源映射和 1 项启动缓存回归，以及文档内实际场景截图。本轮新增两平台构建与包信息检查，未重跑玩法套件或设备测试；构建成功不代表真人/设备验收完成。警告数量如上，不宣称零警告或全部警告已逐项排除。

## 验证边界

沿用 2026-09-26 已记录的聚焦测试和实际场景证据，本轮不重复全量玩法测试。只检查构建结果、ZIP 条目、manifest 的名称/版本/SDK/图标条目、实际 ABI 与文件哈希。Windows 独立包启动/画面/对局及 Android 平板安装、触控、旋转、帧率、内存、连续对局均待策划拿包复测，未做的不能写通过。扩展运行时夹具 `Menu_CommanderSelection_AndTemporaryPreview` 未验证、仍不记通过。ZIP、APK 和日志在被忽略的 Builds 目录，不将包上传 Git；push 指源码和记录。
