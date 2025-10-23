# Lucky "Box"

[中文](README.md) | **English**

A mod for "Escape from Duckov" that allows players to use additional features in the shop.

![Screenshot](imgs/Screenshot.png)

## Features

1. ⌚ A decent-looking lottery animation

2. ♻️ "Refresh": Refresh the current merchant's inventory

3. 👌 "Pick One from Merchant": Randomly draw one item from the current merchant's inventory (reduces stock)

4. 😊 "Pick One from Roadside": Randomly draw one item from all possible items in the game (doesn't affect the stock)

5. ⚙️ Press F1 to open the settings panel to enable/disable animations and customize the cost of the above three features (default values: 5, 50, 50) (both account balance and cash can be used for payment)

⚠️ Warning: "Pick One from Roadside" may draw illegal items that are not available in the game (although "most" have been filtered out). Please use it carefully❗ Especially for recipes that "look very abnormal"❗ This may corrupt your save file. Please backup your save before using❗

## Future Plans

1. 📦 Add "Lucky Box" item that randomly draws an item or BUFF when opened
2. ~~Settings persistence~~ ✅ **Completed!**

## Configuration

Settings are automatically saved to `{Application.persistentDataPath}/Duckov.LuckyBox/config.json`.

The configuration file supports:
- **EnableAnimation**: Enable/disable lottery animation (default: `true`)
- **SettingsHotkey**: Hotkey to open settings panel (default: `"F1"`)
- **RefreshStockPrice**: Cost to refresh merchant inventory (default: `5`)
- **StorePickPrice**: Cost to pick from merchant's stock (default: `50`)
- **StreetPickPrice**: Cost to pick from all items (default: `50`)

The mod will automatically reload settings when the config file is modified externally.

See [config.example.json](config.example.json) for a sample configuration file.

## Links

Source Code: https://github.com/DarkHighness/DuckovLuckyBox

Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3589741459