# VC5 开发经验与踩坑记录

更新时间：2026-09-02
首次沉淀版本：`demo_v2_20260830`

## 文档定位

本文记录跨版本仍有价值的开发方法、验证策略和已踩过的坑。具体规则以主规则文档为准，具体卡牌以已实现卡牌文档为准，当前版本状态以发布计划和 `Docs/Releases` 交接记录为准。历史聊天不能代替这些文件。

## 文档与代码的事实来源

| 内容 | 权威文档 |
|---|---|
| 游戏规则、阶段、得分、胜负 | `战场女武神5最新规则与卡牌-20260621.md` |
| 实际可玩卡牌、数值、属性和资源 ID | `Docs/VC5_已实现可玩卡牌与卡组.md` |
| 卡组设计历史与候选方案 | `Docs/VC5_初版Demo简易卡组方案_20260701.md` |
| AI 决策、选择链和表现边界 | `Docs/VC5_AI行动逻辑.md` |
| Menu 入口、Solo AI、教学和人工测试 | `Docs/VC5_Demo启动与AI对战流程.md` |
| 发布门禁、进度和缺口 | `Docs/VC5_Demo发布计划与进度.md` |
| 单个版本的准确快照 | `Docs/Releases/` 下对应交接记录和 Git Tag |

项目规则要求先更新相关文档，再写测试和代码。不要在多个文档复制互相矛盾的“当前状态”；历史段落保留当时结论，文件顶部或版本封存节给出最新快照。

## 高效开发流程

1. 每一轮先固定目标、非目标、风险和验收标准，避免在一个版本同时扩展卡组、重构联网、改 UI 和做发布优化。
2. 小问题先写能复现原症状的定向测试，再做最小修改；通过后运行 `VC5DemoGate`，不要一开始反复跑完整门禁。
3. 自动门禁、真实场景运行、截图检查、人工手感和玩家反馈分开记录，不能互相替代。
4. 每次试玩版完成后提交、创建 Tag、写交接记录并结束会话；下一会话从 Tag 和文档恢复，不搬运整段聊天。
5. 构建产物、测试 XML/PNG、`Library`、`Temp` 和本地编辑器状态不提交 Git；源码、资源、`.meta`、项目设置和可重建文档必须提交。
6. 修改已有大文件前先搜索既有实现入口和测试，沿用当前架构；不要为了单个 Demo 功能引入第二套规则计算。

## Unity 与测试经验

- 固定 Unity `2021.3.33f1c1`。同一项目被图形 Editor 打开时，另一个 batchmode Unity 会受项目锁影响；需要命令行完整门禁时，先关闭主 Editor，或使用经过确认的独立 clone/worktree。
- Unity MCP 能提高场景、Console、Play Mode 和截图检查效率，但新会话需要重新确认 Bridge。MCP 不可用时可以做静态检查，不能把静态检查描述为运行验证。
- MCP 跨 Play Mode/domain reload 可能遗留“运行中”状态。先查看 Unity 原生 XML、Editor 是否空闲和 Console，再决定是否重跑，避免同时启动第二轮测试。
- 批处理用 `Start-Process -PassThru` 获取 Unity 主进程并等待该进程退出；不要简单使用可能把常驻许可子进程一起等待的方式。
- 测试等待动画时循环 `yield return null` 并检查条件；EditMode 测试不要直接依赖 `WaitForSeconds`。跨 domain reload 不要在外层协程保留 lambda 捕获的临时 Unity 对象。
- UI 自定义 `MaskableGraphic` 必须有 `CanvasRenderer`。只断言生成了顶点不能证明屏幕真正显示，至少要加渲染检查或截图像素检查。
- 真实运行入口从 `Assets/TcgEngine/Scenes/Menu/Menu.unity` 开始。直接打开 Game 场景可能跳过卡组选择、临时用户、固定先攻、教学和 AI 初始化，不能替代发布链路测试。

## 规则与棋盘高风险区

- 双方逻辑坐标是玩家相对坐标，但占格、距离、贴身方向和预览必须统一到画面实际格。曾经因此出现方向错误、落到不存在格子和敌我重叠。
- 任何移动、召唤、瞬移和贴身效果都只能使用 `Game.unity` 实际存在的 34 格，并排除双方单位占用的画面格。矩形坐标合法不代表棋盘上真的有该格。
- 条件、AI、预览和最终效果应共用同一移动行动者与合法格判断。主动技能使用 caster；卡牌后续效果可能使用 ability triggerer，不能读到上一行动残留角色。
- Demo 攻击牌不要直接复用带自动反击的模板 `EffectAttack`，除非卡面明确写有反击。普通攻击、射击和冲锋攻击已经改为单向伤害。
- 预览必须使用对局副本或纯计算，不能扣费、移动、伤害或写状态；确认后结算应与预览共用规则，避免“看见一种结果、执行另一种结果”。

## C3 与教学的关键边界

- C3 是独立卡组，不能覆盖旧射击压制。菜单、AI 列表、资源 ID、测试和文档都应按独立 ID 处理。
- C3 本地 Solo 先正常洗牌，再把一张`移动射击`调到第一抽；其余 19 张保持随机相对顺序。不要把整副牌改成固定顺序。
- 六状态教学只在玩家使用 C3 的 Demo Solo 中触发，不进入 Multiplayer/P2P，也不作用于 AI 或旧 B/C。
- 教学进度由真实操作和结算事件推进：开始拖动、指向正确棋子、成功出牌、移动和伤害完成。不要只按计时器或鼠标松开推进。
- 教学期间允许底层行动权因普通牌交给 AI，但 AI 执行被门禁暂停；最终页关闭、对象销毁或场景退出都必须释放门禁。
- 教学高亮使用空心边框。旧 UI 材质曾把低透明度填充渲染成实色色块并遮住游戏内容。

## Figma 本地协作

- 使用个人 Skill：`$local-figma-unity-ui-review`。它固定使用 `figmaLocalBridge`，不调用官方远程 Figma MCP，并在明确批准前不修改 Unity。
- 日常从 `D:\CodexData\tools\figma-mcp-bridge\desktop\Launch-Figma.cmd` 启动本地 Figma；保持目标 Design 文件和 `Figma MCP Bridge` 插件打开。
- 先 `list_files` 确认文件，再读取页面和节点；空列表只表示桥接服务可响应，不表示插件已连接。
- UI 审核稿应使用真实开局截图和原生可编辑图层。教程每个状态建立独立 Frame，并记录触发该状态的实际程序事件。
- 修改后回读节点并把截图存到 `D:\CodexData\tools\figma-mcp-bridge\workspace\artifacts`。`save_screenshots` 不能写到工作目录之外。
- `unsaved-` fileKey 只证明本地文件连接成功，不代表已经保存到 Figma Drafts/项目。版本交接前要保存文件并记录文件名、页面和最终 Frame。

## 美术与 UI 资源

- 运行时图片位于 `Assets/TcgEngine/Resources/VC5/DemoArt`；只改 `img` 或 `Docs/Art` 不会自动更新游戏。
- 替换图片时保留 Unity `.meta`，避免 GUID 变化导致绑定丢失。图片导入上限、mipmap、Read/Write 和压缩设置需要重新检查。
- 手牌、左侧详情和棋盘棋子是不同布局。棋盘方形图正确不代表手牌与详情图会填满；必须分别检查常用分辨率。
- 外部角色图片在扩大公开测试或商业发布前确认授权，不要把“能显示”误写成“可以正式发布”。

## 构建与磁盘经验

- Demo Build Settings 只启用 `Menu.unity` 和 `Game.unity`，`Menu.unity` 必须为索引 0。每次构建前由构建管线校验，不手工依赖 Editor 当前勾选状态。
- 产品名为 `ValkyriaChronicles5`，图标资源为 `Assets/TcgEngine/Images/VC5/AppIcon.png`。
- `Builds` 被 Git 忽略。Git Tag 固定可重建输入，不固定本地二进制；正式外发时另外记录 ZIP/APK 的大小和 SHA-256。
- Windows 目录中的 `*_BurstDebugInformation_DoNotShip` 不发送给玩家。Windows 分发必须包含 EXE、`*_Data`、`MonoBleedingEdge`、`UnityPlayer.dll` 等完整同目录内容。
- Android 首次构建耗时主要来自平台导入、着色器、Gradle 和压缩；日志暂时无输出不一定卡死，先检查 Unity/Java 进程再决定是否重跑。
- 在启动 Unity 前设置项目 E 盘 `TEMP`、`TMP`、`GRADLE_USER_HOME`。曾因启动后才设置导致 C 盘生成约 177 MiB `.gradle`；确认 E 盘缓存可用后才精确清理本轮 C 盘目录。
- 日常 Android 增量构建不要清空 Gradle 缓存；只有缓存损坏、平台升级或最终候选包才使用 clean 构建。

## Git 与版本封存

- 版本命名使用可搜索 Tag，例如 `demo_v2_20260830`。Tag 指向封存提交，不在文档里硬编码“当前提交”造成自引用哈希问题。
- `.obsidian/` 是本地编辑器状态，已经忽略；不要与游戏版本一起提交。
- 封存前检查 `git status --short --branch`、`git diff --check`、未跟踪大文件、测试结果和构建日志。不要直接 `git add -A` 后不看暂存区。
- 提交前用 `git diff --cached --stat` 和 `git status --short` 审核暂存内容；提交后确认 Tag 与 `HEAD` 一致并检查工作树只剩预期忽略项。
- 旧版本构建与新源码可能同时留在 `Builds`。不要因为文件名相似把旧 APK、旧 Windows 包当成当前 Tag 的发布物。

## 新一轮玩家反馈处理

1. 原样保存玩家反馈，注明版本 Tag、平台、设备和对局卡组。
2. 分成已复现 Bug、可用性问题、平衡意见和个人偏好。
3. 先修阻塞正常游玩和理解核心玩法的问题，再处理美术、平衡和新增内容。
4. 每个采用项写清目标、修改范围、风险和验收标准；未采用项保留原因，不静默丢失反馈。
5. 下一版本仍以“玩家只需打牌、少操作、预览可理解”为核心，不因单条反馈重新引入棋子自由移动或复杂多段选择。
