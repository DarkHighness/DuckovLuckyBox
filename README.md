# 幸运"方块"

**中文** | [English](README.EN.md)

这是一个 "逃离鸭科夫" 的 Mod，允许玩家在商店中使用额外的功能。

## 截图

![截图](imgs/Screenshot.png)

## 功能

⚠️：本 Mod 物品价值参考 ”物品价值稀有度与搜索音效“ Mod, 如果您安装的稀有度和价值 Mod 版本与本 Mod 不一致，可能会产生中奖概率与游戏体感不符的问题！

### 商店功能
1. ⌚ 一个看起来还行的抽奖动画（播放时鼠标左键点击可跳过）
2. ♻️ "刷新"：刷新当前商人的库存情况
3. 👌 "商人那拾一个"：从当前商人的库存当中随机抽一件，会扣库存
4. 😊 "路边拾一个"：从游戏所有可能的物品当中随机抽一件，不涉及库存

### 物品菜单功能
5. 🗑️ "销毁"：销毁选中的物品（右键物品打开菜单）
6. 🎰 "熔炼"：消耗游戏货币，使用选中的物品进行熔炼
   - 根据物品的**品质(Quality)**和**堆叠数量(StackCount)** 熔炼出相应数量和品质的随机物品
   - 熔炼结果包括：品质升级，品质降低，品质不变或失败（无物品产出）

### 汰换合同功能
7. 🗂️ "汰换合同"：将物品放入垃圾箱，根据物品价值获得奖励
   - **支持的物品类型**：武器、近战武器、头盔、医疗用品、面罩、护甲、奢侈品、注射器、电器、图腾、工具
   - 高品质物品会触发特殊的奖励动画和音效
   - 对于子弹类型物品，要求至少堆叠数量为 30 发（弓箭为 20 发）才可放入汰换合同

### 特殊物品

- 🎁 **战术刀具箱**、等特定物品：打开时触发抽奖动画，自动从该物品内抽取随机物品

### 设置

- ⚙️ 设置面板已被集成到游戏设置内
  - 开启或关闭抽奖动画
  - 开启或关闭高等级物品音效（自定义音效文件路径）
  - 自定义商店功能的花费（账户和现金均可支付花费）
  - 开启或关闭物品菜单的销毁和抽奖按钮
  - 启用/关闭加权抽奖（按物品品质加权）
  - 开启或关闭游戏内物品抽奖动画

⚠️ 警告："路边拾一个" 和 "抽奖" 功能有可能抽到游戏内未开放的非法物品（尽管已经尽力过滤了 "绝大部分"），请仔细斟酌使用❗特别是对于 "看起来就很异常的" 配方❗可能会导致存档损坏，使用前请务必备份存档❗

## 灵感来源

Minecraft "幸运方块"

## 配置

设置会自动保存到 `{Application.persistentDataPath}/Duckov.LuckyBox/config.json`。 典型值为：`C:/Users/$USER/AppData/LocalLow/TeamSoda/Duckov\Duckov.LuckyBox\config.json`

配置文件支持：

- **EnableAnimation**: 启用/关闭抽奖动画（默认：`true`）
- **EnableHighQualitySound**: 启用/关闭高等级物品音效（默认：`true`）
- **HighQualitySoundFilePath**: 高等级物品音效的自定义文件路径（默认：空字符串，使用默认音效）
- **RefreshStockPrice**: 刷新商人库存的花费（默认：`100`，范围：0-10000，步长：100）
- **StorePickPrice**: 从商人库存抽奖的花费（默认：`3000`，范围：0-10000，步长：100）
- **StreetPickPrice**: 从所有物品抽奖的花费（默认：`3000`，范围：0-10000，步长：100）
- **MeltBasePrice**: 熔炼物品的基础花费（默认：`100`，范围：0-10000，步长：100）
- **EnableDestroyButton**: 启用/关闭物品菜单的销毁按钮（默认：`true`）
- **EnableLotteryButton**: 启用/关闭物品菜单的抽奖按钮（默认：`true`）
- **EnableWeightedLottery**: 启用/关闭加权抽奖（按物品品质加权，默认：`false`）

当配置文件被外部修改时，Mod 会自动重新加载设置。

配置文件示例：

- 基础配置: [config.example.json](config.example.json)
- 复合按键配置: [config.with-modifiers.example.json](config.with-modifiers.example.json)

## 相关链接

源代码仓库: <https://github.com/DarkHighness/DuckovLuckyBox>

Steam 创意工坊: <https://steamcommunity.com/sharedfiles/filedetails/?id=3589741459>
