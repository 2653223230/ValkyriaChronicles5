# 共享AP晴岚炮台：Unity实机审核图

2026-10-29，Unity2021.3.33f1c1，独立场景 `Assets/VC5PvE/SharedAP/Scenes/SharedAP_Qinglan.unity`。

图片使用实际 GameView 的 ScreenCapture，包含 Overlay UI；没有使用生成图替代实机证据。

| 文件 | 检查内容 |
| --- | --- |
| [01-opening-1920.png](01-opening-1920.png) | 1920×1080固定起手、卡牌名称/费用、共享AP、红框、敌人和坐标 |
| [02-retain-after-opening.png](02-retain-after-opening.png) | 合法执行设计首轮后，剩1AP并保留治疗，射手在炮击区有3盾 |
| [03-victory-round3.png](03-victory-round3.png) | 正常血量/牌序第3回合胜利；最后修正了世界名牌覆盖结果窗的问题 |
| [04-opening-1280.png](04-opening-1280.png) | 真正切换GameView到1280×720后的画面，不是缩图冒充分辨率测试 |
| [05-shift-opens-cannon.png](05-shift-opens-cannon.png) | 术师将仍存活的护卫移到D4，保护连线消失，先锋已在C2准备集中输出 |

目检结果：主要按钮、手牌、角色栏、共享AP、目标与地图均在画面内；卡名不再因Noto字体行高被截掉，脚底与棋盘格对齐。美术为复用旧资源加首版炮座几何模型，尚待用户审核。

`Diagnostics/camera-only-initial.png` 是早期排查截图，仅包含相机层、没有UI且包含已修复视觉问题，留作开发记录，不能作为最终审核画面。

动作由控制器与拖拽事件测试驱动；两条正常通关路线和适用范围见[实现与审核记录](../../VC5PvE_共享AP晴岚炮台_实现与审核记录_20261029.md)。尚未证明真人第一次游玩时长、随机起手整体胜率或Android真机体验。
