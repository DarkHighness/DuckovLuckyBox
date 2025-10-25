# 幸运"方块"

**中文** | [English](README.EN.md)

这是一个 "逃离鸭科夫" 的 Mod，允许玩家在商店中使用额外的功能。

## 截图

![截图](imgs/Screenshot.png)

## 功能

### 商店功能
1. ⌚ 一个看起来还行的抽奖动画（播放时鼠标左键点击可跳过）
2. ♻️ "刷新"：刷新当前商人的库存情况
3. 👌 "商人那拾一个"：从当前商人的库存当中随机抽一件，会扣库存
4. 😊 "路边拾一个"：从游戏所有可能的物品当中随机抽一件，不涉及库存

### 物品菜单功能
5. 🗑️ "销毁"：销毁选中的物品（右键物品打开菜单）
6. 🎰 "抽奖"：使用选中的物品进行抽奖
   - 根据物品的**品质(Quality)**和**堆叠数量(StackCount)**抽取相同数量的相同品质物品
   - 例如：用 5 个品质为 3 的物品抽奖，会得到 5 个随机的品质 3 物品

### 特殊物品
- 🎁 **战术刀具箱**、等特定物品：打开时触发抽奖动画，自动从该物品内抽取随机物品

### 设置
7. ⚙️ 按 F1 可打开设置面板
   - 开启或关闭抽奖动画
   - 开启或关闭高等级物品音效（自定义音效文件路径）
   - 自定义商店功能的花费（默认值：100）（账户和现金均可支付花费）
   - 开启或关闭物品菜单的销毁和抽奖按钮
   - 启用/关闭加权抽奖（按物品品质加权）
   - 开启或关闭游戏内物品抽奖动画

⚠️ 警告："路边拾一个" 和 "抽奖" 功能有可能抽到游戏内未开放的非法物品（尽管已经尽力过滤了 "绝大部分"），请仔细斟酌使用❗特别是对于 "看起来就很异常的" 配方❗可能会导致存档损坏，使用前请务必备份存档❗
⚠️ 注意：如果按键存在冲突，请前往配置文件中手动修改按键设置。支持形如 "Ctrl+F1" 的复合按键（注：不能与系统快捷键或游戏内快捷键冲突，否则会无法正常录入按键）。

## 灵感来源

Minecraft "幸运方块"

## 未来计划

1. 📦 添加"幸运方块"道具，当打开时，随机抽取一件物品或BUFF
2. ~~设置配置落盘~~ ✅ **已完成！**

## 配置

设置会自动保存到 `{Application.persistentDataPath}/Duckov.LuckyBox/config.json`。 典型值为：`C:/Users/$USER/AppData/LocalLow/TeamSoda/Duckov\Duckov.LuckyBox\config.json`

配置文件支持：
- **EnableAnimation**: 启用/关闭抽奖动画（默认：`true`）
- **SettingsHotkey**: 打开设置面板的快捷键（默认：`"F1"`）
  - 支持复合按键，例如：`"Ctrl+F1"`, `"Shift+F2"`, `"Alt+F3"`, `"Ctrl+Shift+F4"`
  - 可用修饰键：`Ctrl`, `Shift`, `Alt`
- **EnableHighQualitySound**: 启用/关闭高等级物品音效（默认：`true`）
- **HighQualitySoundFilePath**: 高等级物品音效的自定义文件路径（默认：空字符串，使用默认音效）
- **RefreshStockPrice**: 刷新商人库存的花费（默认：`100`，范围：0-5000，步长：100）
- **StorePickPrice**: 从商人库存抽奖的花费（默认：`100`，范围：0-5000，步长：100）
- **StreetPickPrice**: 从所有物品抽奖的花费（默认：`100`，范围：0-5000，步长：100）
- **EnableDestroyButton**: 启用/关闭物品菜单的销毁按钮（默认：`true`）
- **EnableLotteryButton**: 启用/关闭物品菜单的抽奖按钮（默认：`true`）
- **EnableWeightedLottery**: 启用/关闭加权抽奖（按物品品质加权，默认：`false`）

当配置文件被外部修改时，Mod 会自动重新加载设置。

配置文件示例：
- 基础配置: [config.example.json](config.example.json)
- 复合按键配置: [config.with-modifiers.example.json](config.with-modifiers.example.json)

## 相关链接

源代码仓库: https://github.com/DarkHighness/DuckovLuckyBox

Steam 创意工坊: https://steamcommunity.com/sharedfiles/filedetails/?id=3589741459
