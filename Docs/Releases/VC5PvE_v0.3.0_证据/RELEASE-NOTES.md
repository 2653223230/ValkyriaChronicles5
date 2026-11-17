# 晴岚战术 v0.3.0｜共享 AP 单关试玩版

源码：`f6d7a0fb2bae1dbe7fc9da7b4c3f7de90c7bb895`，分支 `codex/shared-ap-qinglan-20261029`。

- 全队共用 6 AP，卡牌拖给执行棋子；先锋近战、射手单体远程、术师范围攻击。
- 晴岚炮台单关，逐步聚焦教学涵盖角色信息、免费短步、打牌、普通攻击和留牌。
- C1 点击射手棋子或左侧头像均可选中；D5“开始自由战斗”会重置初始战局并关闭教学，中途跳过则保留本局。
- 击毁炮座或击败三名敌兵获胜。固定起手，支持结束后随机手牌再挑战。

## 下载与运行

Windows x64：解压整个 ZIP，再运行 `Windows/VC5PvE-SharedAP.exe`，不要只复制 exe。
Android：安装 ARM64 APK，最低 Android 7.0（API24），应用包名 `com.vc5pve.sharedap`，可与旧个人 AP 版并存。试玩包使用 Android 调试签名，不是应用商店发行包。

## 验证与限制

本次补验 D5 重开／中途跳过两项 PlayMode 通过。Windows 完整构建与启动检查、APK 结构／版本／签名校验见仓库发布记录。Windows 启动日志出现 Unity JobTempAlloc 临时分配提示，未见托管异常；该检查不等于真人完整通关。Android 没有连接设备，尚未做真机安装、触屏及性能验收。

旧个人 AP 三关版仍可从 v0.2.0 发行页下载。本版为独立共享 AP 晴岚单关试玩预发布，不取代旧标签，不设为 latest。
