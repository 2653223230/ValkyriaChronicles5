# 共享AP交互V2 · Unity审核证据

2026-11-04；工程 Prototypes/VC5PvE，场景 Assets/VC5PvE/SharedAP/Scenes/SharedAP_Qinglan.unity。截图为1920×1080 GameView实机画面，非Figma图。

| 画面 | 核对内容 |
| --- | --- |
| [开局](01-opening.png) | 先锋技能前后图、左侧小队、AP常驻、炮击红框可见；此图在最后两行文案排版调整前采集，先锋内容不受影响 |
| [术师](02-role-mage.png) | 武器两行说明无重叠、移位图示、短步合法格；鼠标悬停治疗牌产生抬起效果 |
| [弃牌](03-discard.png) | 机动/治疗选中上浮；弃2留3，下轮补2；返回和弃牌确认按钮 |
| [突击](04-charge-preview.png) | 射手接牌→选C5落点→进入选敌阶段；未来射程分段线，2格AP待消耗标金；此时真实AP6/手牌5 |
| [下一回合](05-round2.png) | 敌人已推进，射手4/6HP，轮次2、AP6、手牌5、新炮击红框 |

## 验证方式与边界

截图状态由MCP调用实际控制器建立；突击完成攻击后实际位置C5、AP4、手牌4、巡兵2HP。弃牌确认先进入Enemy/Busy，演出结束后回到Player。无旧PrototypeFlow实例混入。
自动化另覆盖CardDragInput真实组件回调、EventSystem队员栏命中、UI Button弃牌选择/确认、上浮、保留未选牌、敌方行动与补牌。没有把这些调用视为真人原生鼠标或Android触屏全流程验收。

全量PlayMode66/66、EditMode80/80通过；最后两项排版调整后定向PlayMode3/3通过。测试任务ID见tests.json与统一实现记录。最后读取Unity错误日志0条。

## 用户试玩入口

在Unity打开SharedAP_Qinglan并Play。先点卡观察可执行队员，再直接拖攻击/突击给射手；突击选青色落点，再选敌人。选小队头像查看技能图；点击棋子后直接点击高亮短步格/普攻目标。结束回合后点选要弃的牌，确认；不选则全留。仍需用户评价实际手感和美术，Android尚未真机验收。
