# 操作引导实景与队伍栏审核

## Unity 实景（已实现）

- `card-drag-final.png`：实际 Play 中发送拖拽事件，卡面留槽淡出、箭头指向战士、三名合法执行者双圈。此时 6 张手牌、战士 3 AP。
- `legal-move-targets.png`：拖到执行者后显示合法移动格，没有消耗资源。
- `enemy-target-fixture.png`：星火印选中法使后，仅合法敌人有双圈。临时把法使设到 D6 以构造射程场景，未修改/保存首关初始部署。

## Figma 待审核（尚未接入 Unity）

- `party-context-final.png`：在当前游戏画面上替换左侧队伍栏的位置稿。
- `party-selected-final.png`：选中星纹师的队伍栏近景。
- `party-states-final.png`：未选中、选中、受伤/AP 消耗三状态对照。

本地文件 VC5 Demo 玩家引导 UI，fileKey `unsaved-murtma7i-4sino79g`；页 `299:2`；实景位置 frame `300:3`、状态说明 frame `302:86`。头像使用当前项目既有立绘，UI 为原生可编辑 Figma 图层。

规则、范围与验证细节见 `Docs/Plans/2026-10-03-VC5PvE操作引导与队伍栏审核.md`。本轮没有打包；运行独立工程的 `Assets/VC5PvE/Scenes/Title.unity` 即可试玩已经实现的部分。
