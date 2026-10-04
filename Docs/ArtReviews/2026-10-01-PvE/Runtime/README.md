# Unity 实际运行与构建记录（2026-10-01）

独立工程：`Prototypes/VC5PvE`；Unity 2021.3.33f1c1，URP12.1.13，版本0.1.0。

## 检查结果

| 项目 | 实际证据 |
| --- | --- |
| EditMode | VC5PvE.Tests：18/18；job `1c131a26d0e049edb220bd2a5e779053` |
| PlayMode | VC5PvE.PlayTests：5/5；job `9075d08c161f4f49b670297af65a8174`；6.331秒 |
| 正常联动 | 战士C6、辅助C7、法使D6：鼓舞星火印2伤，守护减1后推进盾吸收，下一轮贯星术4伤击杀并消费印记 |
| 换牌 | 每轮首动作前实际UI处理：手牌仍6，换出牌离开手牌，不算已行动 |
| 新工程 Console | 最终场景重新生成、运行截图后读取错误0条 |
| Windows 包 | 构建成功，0错误/1警告；实际exe启动检查通过，详见下方记录 |

用例覆盖规则合法性、预览不消费资源、非法/陈旧操作不扣费、状态期限、抽换牌、AI，以及标题→部署→战斗→胜败→重试/标题、敌方锁定、AP全空自动交接、旧协程隔离、推位动画。使用同源纯规则与Unity流程检查；不是人工鼠标试玩。

## 截图

- [标题1920×1080](release-title-1920.png)、[部署1920×1080](release-deploy-1920.png)、[己方回合1920×1080](release-player-1920.png)：最终前景遮挡修正后截图，实际 ScreenCapture，包括 Overlay UI。
- [预览1280×720](pve-preview-final-1280.png)、[敌方回合1280×720](pve-enemy-final-1280.png)：实际运行UI，拍摄在最后前景树缩小及提示本地化修正之前，卡面/布局相同。
- [胜利1280×720](pve-victory-final-1280.png)、[失败1280×720](pve-defeat-final-1280.png)：缩减敌军/低血量测试 fixture 通过真实攻击触发，只证明结算流程；拍摄在最终提示本地化前。

## 尚未验收

自然血量完整通关、两条不同部署/推进路线的人工试玩、随机牌序策略与难度平衡、最终美术及外部设备。不能用上述 fixture、静态 Figma 图或自动测试宣称这些项目通过。

## 最终 Windows 交付

构建 job `build-cfefb985fe`：2026-10-01 12:27完成，StandaloneWindows64、非Development，96488717 bytes（约92.02MiB），38.6秒，0错误/1警告。警告为测试框架Runner程序集未找到；实际Windows程序已正常启动，没有运行异常。详见 [完整构建报告](build-report.txt)。首次全量Lit构建停止，优化后成功，不把停止的任务计成功。

- exe：`Prototypes/VC5PvE/Builds/Windows/森钟战术.exe`。
- 便携包：`Prototypes/VC5PvE/Builds/森钟战术-Windows-x64-v0.1.0.zip`，47198569 bytes，包含运行所需文件、README、第三方声明/许可，排除Burst调试信息。
- exe SHA256：`E0F5551437DF1F11F5407499A17211FA59EF3F4FCE560BFC1C07DC2BBA863D9F`。
- 在本机RTX2070Super上隐藏启动实际exe，D3D11/Mono/输入初始化完成、进程正常响应，检查运行日志无Exception/Error后关闭本任务启动的进程。这是发布程序启动检查，不是人工游戏流程或其他机器验收。

