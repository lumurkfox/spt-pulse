# Pulse - Bot Wave Generator for SPT

**Pulse** is a customizable bot wave generator mod for Single Player Tarkov (SPT) that enhances your raid experience by controlling bot spawning behavior, group sizes, and raid dynamics.

## Features

- **Customizable PMC Group Sizes** - Control the maximum size of PMC groups
- **PMC/Scav Ratio Control** - Set the percentage of PMCs vs Scavs per assault wave
- **Scav Wave Configuration** - Adjust scav group sizes and spawn patterns
- **Bot Count Multipliers** - Scale bot populations with configurable min/max multipliers
- **Global Bot Limit** - Set a hard cap on maximum bot count that overrides multipliers
- **Assault Wave Control** - Set the number of assault waves per raid
- **Boss Spawn Control** - Adjust boss spawn chances with percentage multiplier
- **PMC Difficulty Settings** - Choose PMC bot difficulty levels
- **Debug Mode** - Enable detailed logging for troubleshooting

## Installation

1. Download the latest release
2. Extract the `Pulse` folder to your `SPT/user/mods/` directory
3. Configure settings in `config/config.json` (optional)
4. Launch SPT and enjoy!

## Configuration

Edit `config/config.json` to customize your experience:

```json
{
  "MaxPmcGroupSize": 3,
  "MaxScavGroupSize": 3,
  "PmcBotDifficulty": "normal",
  "AssaultWaveCount": 3,
  "BotCountMultiplierMin": 1.5,
  "BotCountMultiplierMax": 2.0,
  "GlobalBotLimit": 0,
  "BossSpawnChancePercent": 100.0,
  "PmcPercentage": 33.0,
  "Debug": false
}
```

### Options

- **MaxPmcGroupSize**: Maximum PMCs per group (default: 3)
- **MaxScavGroupSize**: Maximum Scavs per group (default: 3)
- **PmcBotDifficulty**: PMC difficulty - "easy", "normal", "hard", or "impossible" (default: "normal")
- **AssaultWaveCount**: Number of assault waves (default: 3)
- **BotCountMultiplierMin**: Minimum bot count multiplier (default: 1.5)
- **BotCountMultiplierMax**: Maximum bot count multiplier (default: 2.0)
- **GlobalBotLimit**: Hard cap on bot count. If the multiplier result exceeds this limit, bot count is capped at this value. Set to 0 to disable (default: 0)
- **BossSpawnChancePercent**: Boss spawn chance percentage. 100.0 = normal spawn chances, 50.0 = half the chance, 200.0 = double the chance (default: 100.0)
- **PmcPercentage**: Percentage of bots that spawn as PMCs per assault wave. 33.0 = 1/3 PMCs and 2/3 Scavs, 50.0 = equal split, 75.0 = mostly PMCs (default: 33.0)
- **Debug**: Enable debug logging (default: false)

## Credits

**Pulse** is a customized fork of [Unda](https://github.com/barlog-m/spt-unda) by Barlog_M.

## Author

**LumurkFox**

## License

MIT License - See LICENSE file for details

## Links

- GitHub: https://github.com/lumurkfox/spt-pulse
- Original Unda Mod: https://github.com/barlog-m/spt-unda

## Version

1.0.2 - Compatible with SPT 4.0.x
