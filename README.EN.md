# Lucky "Box"

[中文](README.md) | **English**

A mod for "Escape from Duckov" that allows players to use additional features in the shop.

![Screenshot](imgs/Screenshot.png)

## Features

⚠️ Note: This mod references item value from the "Item Value Rarity and Search Sounds" mod. If the version of the rarity and value mod you have installed is inconsistent with this mod, it may cause discrepancies between the draw probabilities and in-game experience!

### Shop Features

1. ⌚ A decent-looking lottery animation (can be skipped by left-clicking the mouse while playing) (up to 3 lotteries can be opened at once, requires settings to be enabled)

2. ♻️ "Refresh": Refresh the current merchant's inventory

3. 👌 "Pick One from Merchant": Randomly draw one item from the current merchant's inventory (reduces stock). Double-clicking can trigger multiple draws❗

4. 😊 "Pick One from Roadside": Randomly draw one item from all possible items in the game (doesn't affect the stock). Double-clicking can trigger multiple draws❗

### Item Menu Features

5. 🗑️ "Destroy": Destroy the selected item (right-click item to open menu)

6. 🎰 "Melt": Use the selected item for melting
   1. Based on the item's **Quality** and **StackCount**, melt to get a corresponding quantity and quality of random items
   2. Melting results include: quality upgrade, quality downgrade, quality unchanged, or destroyed (no item output)

### Recycling Contract Feature

1. 🗂️ "Recycling Contract": Put items into the trash bin to get rewards based on item value
   - **Supported Item Types**: Weapons, Melee Weapons, Helmets, Medical Supplies, Face Masks, Armor, Luxury Items, Injectors, Electronics, Totems, Tools
   - Support dragging items into the trash bin
   - Calculate reward level based on item quality and value
   - High-quality items trigger special reward animations and sound effects
   - Reward items are of equal or higher quality than the original items

### Special Items

- 🎁 **Knife Box** and other specific items: Triggers lottery animation when opened, automatically draws random items from that item

### Settings

- ⚙️ The settings panel has been integrated into the game settings
  - Enable/disable lottery animation
  - Enable/disable high-quality item sound effects (customize sound file path)
  - Customize the cost of shop features (default: 100) (both account balance and cash can be used for payment)
  - Enable/disable the Destroy and Lottery buttons in the item menu
  - Enable/disable weighted lottery (weight by item quality)
  - Enable/disable in-game item lottery animation

⚠️ Warning: "Pick One from Roadside" and "Lottery" features may draw illegal items that are not available in the game (although "most" have been filtered out). Please use it carefully❗ Especially for recipes that "look very abnormal"❗ This may corrupt your save file. Please backup your save before using❗
⚠️ Note: If there are key conflicts, please manually modify the key settings in the configuration file. Composite keys such as "Ctrl+F1" are supported (Note: cannot conflict with system shortcuts or in-game shortcuts, otherwise the key cannot be recorded properly).

## Future Plans

1. 📦 Add "Lucky Box" item that randomly draws an item or BUFF when opened

## Configuration

Settings are automatically saved to `{Application.persistentDataPath}/Duckov.LuckyBox/config.json`. A typical path is: `C:/Users/$USER/AppData/LocalLow/TeamSoda/Duckov\Duckov.LuckyBox\config.json`

The configuration file supports:

- **EnableAnimation**: Enable/disable lottery animation (default: `true`)
- **EnableHighQualitySound**: Enable/disable high-quality item sound effects (default: `true`)
- **HighQualitySoundFilePath**: Custom file path for high-quality item sound effects (default: empty string, uses default sound)
 - **RefreshStockPrice**: Cost to refresh merchant inventory (default: `100`, range: 0-10000, step: 100)
 - **StorePickPrice**: Cost to pick from merchant's stock (default: `3000`, range: 0-10000, step: 100)
 - **StreetPickPrice**: Cost to pick from all items (default: `3000`, range: 0-10000, step: 100)
 - **MeltBasePrice**: Base cost for melting items (default: `100`, range: 0-10000, step: 100)
 - **EnableDestroyButton**: Enable/disable the Destroy button in item menu (default: `true`)
 - **EnableLotteryButton**: Enable/disable the Lottery button in item menu (default: `true`)
 - **EnableWeightedLottery**: Enable/disable weighted lottery (weight by item quality, default: `false`)

The mod will automatically reload settings when the config file is modified externally.

Sample configuration files:

- Basic configuration: [config.example.json](config.example.json)
- With modifier keys: [config.with-modifiers.example.json](config.with-modifiers.example.json)

## Links

Source Code: <https://github.com/DarkHighness/DuckovLuckyBox>

Steam Workshop: <https://steamcommunity.com/sharedfiles/filedetails/?id=3589741459>
