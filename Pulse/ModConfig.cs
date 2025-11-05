namespace Pulse;

public class ModConfig
{
    public int MaxPmcGroupSize { get; set; }
    public int MaxScavGroupSize { get; set; }
    public string PmcBotDifficulty { get; set; } = "normal";
    public int AssaultWaveCount { get; set; } = 3;
    public double BotCountMultiplierMin { get; set; } = 1.5;
    public double BotCountMultiplierMax { get; set; } = 2.0;
    public int GlobalBotLimit { get; set; } = 0;
    public double BossSpawnChancePercent { get; set; } = 100.0;
    public bool Debug { get; set; }
}
