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
⚠️ Note: If there are key conflicts, please manually modify the key settings in the configuration file. Composite keys such as "Ctrl+F1" are supported (Note: cannot conflict with system shortcuts or in-game shortcuts, otherwise the key cannot be recorded properly).

## Future Plans

1. 📦 Add "Lucky Box" item that randomly draws an item or BUFF when opened
2. ~~Settings persistence~~ ✅ **Completed!**

## Configuration

Settings are automatically saved to `{Application.persistentDataPath}/Duckov.LuckyBox/config.json`. A typical path is: `C:/Users/$USER/AppData/LocalLow/TeamSoda/Duckov\Duckov.LuckyBox\config.json`

The configuration file supports:
- **EnableAnimation**: Enable/disable lottery animation (default: `true`)
- **SettingsHotkey**: Hotkey to open settings panel (default: `"F1"`)
  - Supports modifier keys, e.g.: `"Ctrl+F1"`, `"Shift+F2"`, `"Alt+F3"`, `"Ctrl+Shift+F4"`
  - Available modifiers: `Ctrl`, `Shift`, `Alt`
- **RefreshStockPrice**: Cost to refresh merchant inventory (default: `100`, range: 0-5000, step: 100)
- **StorePickPrice**: Cost to pick from merchant's stock (default: `100`, range: 0-5000, step: 100)
- **StreetPickPrice**: Cost to pick from all items (default: `100`, range: 0-5000, step: 100)

The mod will automatically reload settings when the config file is modified externally.

Sample configuration files:
- Basic configuration: [config.example.json](config.example.json)
- With modifier keys: [config.with-modifiers.example.json](config.with-modifiers.example.json)

## Links

Source Code: https://github.com/DarkHighness/DuckovLuckyBox

Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=3589741459