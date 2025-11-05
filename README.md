# Pulse - Bot Wave Generator for SPT

**Pulse** is a customizable bot wave generator mod for Single Player Tarkov (SPT) that enhances your raid experience by controlling bot spawning behavior, group sizes, and raid dynamics.

## Features

- **Customizable PMC Group Sizes** - Control the maximum size of PMC groups
- **Scav Wave Configuration** - Adjust scav group sizes and spawn patterns
- **Bot Count Multipliers** - Scale bot populations with configurable min/max multipliers
- **Assault Wave Control** - Set the number of assault waves per raid
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

1.0.0 - Compatible with SPT 4.0.x
