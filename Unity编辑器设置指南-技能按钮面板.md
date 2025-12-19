# Unity编辑器设置指南 - 技能按钮面板

## 准备工作

在开始之前，请确保：
1. Unity编辑器已经打开
2. 项目已经加载完成
3. 你已经找到了 `Assets/TcgEngine/Prefabs/GameUI.prefab` 文件

---

## 步骤1：打开GameUI Prefab并创建AbilityPanel

### 1.1 找到GameUI Prefab
1. 在Unity编辑器的 **Project窗口**（通常在左下角）中，找到以下路径：
   ```
   Assets/TcgEngine/Prefabs/GameUI.prefab
   ```
2. **双击** `GameUI.prefab` 文件，这会打开Prefab编辑模式（你会看到顶部显示 "Prefab: GameUI"）

### 1.2 找到PanelCanvas
1. 在 **Hierarchy窗口**（通常在左侧）中，展开GameUI对象
2. 找到名为 **"PanelCanvas"** 的对象（这是用于显示面板的Canvas）
3. **选中** PanelCanvas（点击它）

### 1.3 创建AbilityPanel对象
1. 在PanelCanvas被选中的情况下，**右键点击** PanelCanvas
2. 选择 **Create Empty**（创建空对象）
3. 新创建的对象会出现在PanelCanvas下，**重命名**它为 `AbilityPanel`
   - 方法：选中新对象，在Inspector窗口顶部直接输入新名称

---

## 步骤2：添加必要的组件

### 2.1 添加CanvasGroup组件
1. **选中** AbilityPanel对象（在Hierarchy中点击它）
2. 在 **Inspector窗口**（通常在右侧）底部，点击 **"Add Component"** 按钮
3. 在搜索框中输入 `Canvas Group`
4. 点击 **Canvas Group** 组件添加它
5. 在Canvas Group组件中，确保以下设置：
   - **Alpha**: 0（初始透明）
   - **Interactable**: ✓（勾选）
   - **Blocks Raycasts**: ✓（勾选）

### 2.2 添加AbilityPanel脚本
1. 在AbilityPanel对象被选中的情况下，点击 **"Add Component"** 按钮
2. 在搜索框中输入 `Ability Panel` 或 `AbilityPanel`
3. 点击 **Ability Panel** 脚本添加它
   - 如果找不到，可能需要先编译代码（Unity会自动编译）

---

## 步骤3：设置AbilityPanel的RectTransform（定位到右下角）

### 3.1 理解RectTransform
- RectTransform是UI元素的定位组件
- 我们需要将它定位到屏幕右下角

### 3.2 设置Anchor（锚点）
1. **选中** AbilityPanel对象
2. 在Inspector窗口中找到 **Rect Transform** 组件
3. 点击Rect Transform左上角的 **Anchor Presets** 图标（看起来像四个小方框）
4. 按住 **Alt + Shift** 键，然后点击右下角的预设（右下角对齐）
   - 这会同时设置锚点和位置
   - 如果没有自动定位，继续下一步

### 3.3 手动设置位置（如果需要）
如果Anchor设置后位置不对，手动设置：
1. 在Rect Transform组件中：
   - **Anchor Min**: X = 1, Y = 0
   - **Anchor Max**: X = 1, Y = 0
   - **Pivot**: X = 1, Y = 0
2. 设置 **Pos X** 和 **Pos Y** 来调整位置：
   - **Pos X**: -100（距离右边缘100像素，可根据需要调整）
   - **Pos Y**: 100（距离下边缘100像素，可根据需要调整）
3. 设置大小：
   - **Width**: 300（可根据需要调整）
   - **Height**: 400（可根据需要调整）

---

## 步骤4：创建按钮容器（Buttons Container）

### 4.1 创建容器对象
1. **选中** AbilityPanel对象
2. **右键点击** AbilityPanel，选择 **UI > Panel**
   - 这会创建一个Panel作为容器
3. **重命名**这个Panel为 `ButtonsContainer`

### 4.2 设置容器的RectTransform
1. **选中** ButtonsContainer对象
2. 在Rect Transform中：
   - 点击Anchor Presets，按住 **Alt** 键，点击 **Stretch-Stretch**（全屏拉伸）
   - 或者手动设置：
     - **Anchor Min**: X = 0, Y = 0
     - **Anchor Max**: X = 1, Y = 1
     - **Left, Top, Right, Bottom**: 都设为 10（留10像素边距）

### 4.3 添加Vertical Layout Group（垂直布局）
1. **选中** ButtonsContainer对象
2. 点击 **"Add Component"** 按钮
3. 搜索并添加 **Vertical Layout Group** 组件
4. 在Vertical Layout Group中设置：
   - **Spacing**: 10（按钮之间的间距）
   - **Child Alignment**: Upper Center（顶部居中对齐）
   - **Child Control Size**: 
     - **Width**: ✓（勾选）
     - **Height**: ✓（勾选）
   - **Child Force Expand**:
     - **Width**: ✗（不勾选）
     - **Height**: ✗（不勾选）

### 4.4 添加Content Size Fitter（可选，但推荐）
1. **选中** ButtonsContainer对象
2. 点击 **"Add Component"** 按钮
3. 搜索并添加 **Content Size Fitter** 组件
4. 设置：
   - **Horizontal Fit**: Unconstrained
   - **Vertical Fit**: Preferred Size（这样容器会根据内容自动调整高度）

---

## 步骤5：设置AbilityPanel脚本的引用

### 5.1 找到AbilityPanel脚本组件
1. **选中** AbilityPanel对象（不是ButtonsContainer）
2. 在Inspector窗口中找到 **Ability Panel** 脚本组件

### 5.2 设置Buttons Container引用
1. 在Ability Panel组件中，找到 **Buttons Container** 字段
2. 从Hierarchy窗口**拖拽** ButtonsContainer对象到这个字段
   - 或者点击字段右侧的圆圈图标，在弹出的窗口中选择ButtonsContainer

### 5.3 设置Ability Button Prefab引用
1. 在Ability Panel组件中，找到 **Ability Button Prefab** 字段
2. 在Project窗口中，找到以下路径：
   ```
   Assets/TcgEngine/Prefabs/UI/AbilityButton.prefab
   ```
3. **拖拽** AbilityButton.prefab 到 **Ability Button Prefab** 字段
   - 或者点击字段右侧的圆圈图标，搜索"AbilityButton"并选择

---

## 步骤6：验证设置

### 6.1 检查所有设置
确保以下内容都已正确设置：
- ✅ AbilityPanel对象在PanelCanvas下
- ✅ AbilityPanel有CanvasGroup组件
- ✅ AbilityPanel有AbilityPanel脚本
- ✅ AbilityPanel定位在右下角
- ✅ ButtonsContainer在AbilityPanel下
- ✅ ButtonsContainer有Vertical Layout Group
- ✅ AbilityPanel脚本的Buttons Container字段已赋值
- ✅ AbilityPanel脚本的Ability Button Prefab字段已赋值

### 6.2 保存Prefab
1. 点击Unity编辑器顶部的 **"Overrides"** 按钮（如果有的话）
2. 选择 **"Apply All"** 来保存所有更改
3. 或者直接关闭Prefab编辑模式（点击顶部的箭头返回场景）

---

## 步骤7：测试（可选）

### 7.1 进入游戏场景
1. 打开游戏场景（通常是 `Assets/TcgEngine/Scenes/Game/Game.unity`）
2. 运行游戏（点击顶部播放按钮）

### 7.2 测试功能
1. 在游戏中点击一个棋子
2. 应该会在右下角看到技能按钮面板出现
3. 点击技能按钮应该能释放技能

---

## 常见问题

### Q: 找不到AbilityPanel脚本？
**A:** 确保代码已经编译。在Unity中，点击菜单 **Assets > Refresh** 或按 **Ctrl+R** 刷新。

### Q: 面板没有显示在右下角？
**A:** 检查Rect Transform的Anchor设置，确保Anchor Min和Anchor Max都是 (1, 0)。

### Q: 按钮没有显示？
**A:** 检查：
- Ability Button Prefab是否正确赋值
- Buttons Container是否正确赋值
- 棋子是否有技能（AbilityTrigger.Activate类型的技能）

### Q: 按钮点击没有反应？
**A:** 检查AbilityButton prefab是否有Button组件，并且Button组件的OnClick事件已正确设置。

---

## 完成！

如果所有步骤都完成了，你的技能按钮面板应该已经设置好了。现在点击棋子时，技能按钮会出现在右下角，而不是棋子周围。









