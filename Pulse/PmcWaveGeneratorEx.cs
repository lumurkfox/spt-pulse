using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

namespace Pulse;

[Injectable(InjectionType.Scoped, typeof(PmcWaveGenerator))]
public class PmcWaveGeneratorEx(
    ISptLogger<PmcWaveGeneratorEx> logger,
    DatabaseService databaseService,
    ConfigServer configServer,
    RandomUtil randomUtil,
    Data data,
    ModData modData
) : PmcWaveGenerator(databaseService, configServer)
{
    private readonly ModConfig _modConfig = modData.ModConfig;
    private readonly BotConfig botConfig = configServer.GetConfig<BotConfig>();
    private readonly LocationConfig locationConfig =
        configServer.GetConfig<LocationConfig>();

    public override void ApplyWaveChangesToAllMaps()
    {
        foreach (var locationId in Data.AllMaps)
        {
            var location = databaseService.GetLocation(locationId);
            ApplyWaveChangesToMap(location.Base);
        }
    }

    public override void ApplyWaveChangesToMapByName(string name)
    {
        var location = databaseService.GetLocation(name);
        ApplyWaveChangesToMap(location.Base);
    }

    public override void ApplyWaveChangesToMap(LocationBase location)
    {
        var locationId = location.Id.ToLower();
        DeleteAllCustomWaves(locationId);
        DeleteAllPmcBosses(location);
        UpdateMaxBotsAmount(location);
        GeneratePmcAndScavWaves(location);
        GeneratePmcBossWaves(location);
    }

    void DeleteAllPmcBosses(LocationBase location)
    {
        location.BossLocationSpawn = location.BossLocationSpawn
            .Where(bossLocationSpawn =>
                bossLocationSpawn.BossName != "pmcBEAR" &&
                bossLocationSpawn.BossName != "pmcUSEC")
            .ToList();

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] delete all pmc bosses on location '{location.Name}' location.BossLocationSpawn: {JsonSerializer.Serialize(location.BossLocationSpawn)}",
                LogTextColor.Blue);
        }
    }

    void DeleteAllCustomWaves(string locationName)
    {
        locationConfig.CustomWaves.Boss[locationName] = [];
        locationConfig.CustomWaves.Normal[locationName] = [];

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] after delete locationConfig.customWaves.boss: {JsonSerializer.Serialize(locationConfig.CustomWaves.Boss)}",
                LogTextColor.Blue);
        }
    }

    private void UpdateMaxBotsAmount(LocationBase location)
    {
        var locationId = location.Id.ToLower();
        var generalLocationInfo = data.GeneralLocationInfo[locationId];

        if (Data.SmallMaps.Contains(locationId))
        {
            var maxBots = generalLocationInfo.MaxBots;
            var maxPlayers = generalLocationInfo.MaxPlayers;
            IncreaseMaxBotsAmountForSmallLocation(location, maxBots,
                maxPlayers);
        }
        else
        {
            var maxBots = generalLocationInfo.MaxBots;
            var minPlayers = generalLocationInfo.MinPlayers;
            IncreaseMaxBotsAmountForLargeLocation(location, maxBots,
                minPlayers);
        }
    }

    int IncreaseMaxBotsAmountForLargeLocation(
        LocationBase location,
        int maxBots,
        int minPlayers)
    {
        // Apply random multiplier between min and max (e.g., 1.5-2.0x)
        // Calculate as integer range and convert back to decimal
        int minBots = (int)Math.Round(maxBots * _modConfig.BotCountMultiplierMin);
        int maxBotsRange = (int)Math.Round(maxBots * _modConfig.BotCountMultiplierMax);
        var newMaxBotsValue = randomUtil.GetInt(minBots, maxBotsRange);

        // Apply global bot limit if set (overrides multiplier if exceeded)
        if (_modConfig.GlobalBotLimit > 0 && newMaxBotsValue > _modConfig.GlobalBotLimit)
        {
            newMaxBotsValue = _modConfig.GlobalBotLimit;
        }

        var actualMultiplier = (double)newMaxBotsValue / maxBots;
        return SetMaxBotsAmountForLocation(location, maxBots, newMaxBotsValue, actualMultiplier);
    }

    int IncreaseMaxBotsAmountForSmallLocation(
        LocationBase location,
        int maxBots,
        int maxPlayers)
    {
        // Apply random multiplier between min and max (e.g., 1.5-2.0x)
        // Calculate as integer range and convert back to decimal
        int minBots = (int)Math.Round(maxBots * _modConfig.BotCountMultiplierMin);
        int maxBotsRange = (int)Math.Round(maxBots * _modConfig.BotCountMultiplierMax);
        var newMaxBotsValue = randomUtil.GetInt(minBots, maxBotsRange);

        // Apply global bot limit if set (overrides multiplier if exceeded)
        if (_modConfig.GlobalBotLimit > 0 && newMaxBotsValue > _modConfig.GlobalBotLimit)
        {
            newMaxBotsValue = _modConfig.GlobalBotLimit;
        }

        var actualMultiplier = (double)newMaxBotsValue / maxBots;
        return SetMaxBotsAmountForLocation(location, maxBots, newMaxBotsValue, actualMultiplier);
    }

    int SetMaxBotsAmountForLocation(
        LocationBase location,
        int originalMaxBots,
        int newMaxBotsValue,
        double actualMultiplier)
    {
        location.BotMax = newMaxBotsValue;

        var locationId = location.Id.ToLower();
        botConfig.MaxBotCap[locationId] = newMaxBotsValue;

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] {locationId}.BotMax: {originalMaxBots} -> {newMaxBotsValue} (multiplier: {actualMultiplier:F2}x, range: {_modConfig.BotCountMultiplierMin}-{_modConfig.BotCountMultiplierMax})", LogTextColor.Blue);
        }
        return newMaxBotsValue;
    }

    void GeneratePmcAndScavWaves(LocationBase location)
    {
        var locationId = location.Id.ToLower();
        var maxBots = location.BotMax;

        if (locationId == "laboratory") return;

        var zones =
            new List<string>(data.GeneralLocationInfo[locationId].Zones);
        var marksmanZones =
            data.GeneralLocationInfo[locationId].MarksmanZones;

        // Replace zones with empty names with BotZone
        for (var i = 0; i < zones.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(zones[i]))
                zones[i] = "BotZone";
        }

        CleanWaves(location);

        var maxMarksmanGroupSize = locationId == "shoreline" ? 2 : 1;
        var currentWaveNumber = GenerateMarksmanWaves(
            location,
            marksmanZones,
            maxMarksmanGroupSize);

        int maxScavGroupSize =
            locationId == "tarkovstreets"
                ? 3
                : _modConfig.MaxScavGroupSize;

        // Generate waves with both PMC and Scav spawning per wave
        GenerateWavesWithPmcAndScavs(
            location,
            zones,
            (int)location.EscapeTimeLimit,
            maxBots,
            maxScavGroupSize,
            currentWaveNumber);

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] {locationId}.waves: {JsonSerializer.Serialize(location.Waves)}",
                LogTextColor.Blue);
        }
    }

    void GeneratePmcBossWaves(LocationBase location)
    {
        var locationId = location.Id.ToLower();
        var maxBots = location.BotMax;

        // Calculate PMC count based on configured percentage
        // Remaining percentage reserved for scavs
        var pmcRatio = _modConfig.PmcPercentage / 100.0;
        var maxPmcAmount = (int)Math.Ceiling(maxBots * pmcRatio);

        if (maxPmcAmount <= 0)
        {
            logger.Error(
                $"[Unda] {locationId}.maxPmcAmount: {maxPmcAmount}");
        }

        var zones =
            new List<string>(data.GeneralLocationInfo[locationId].Zones);

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] '{locationId}' maxBots: {maxBots}, maxPmcAmount ({_modConfig.PmcPercentage}% ratio): {maxPmcAmount}", LogTextColor.Blue);
        }

        // Split total PMC amount into random groups
        var groups =
            SplitMaxAmountIntoGroups(maxPmcAmount, _modConfig.MaxPmcGroupSize);
        var groupsByZones =
            SeparateGroupsByZones(zones, groups);

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] '{locationId}' PMC groups {JsonSerializer.Serialize(groupsByZones)}", LogTextColor.Blue);
        }

        // Distribute PMC groups across assault waves, prioritizing first wave
        int waveCount = _modConfig.AssaultWaveCount;
        int groupsPerWave = groupsByZones.Count / waveCount;
        int extraGroups = groupsByZones.Count % waveCount;

        int groupIndex = 0;
        for (int waveIdx = 0; waveIdx < waveCount && groupIndex < groupsByZones.Count; waveIdx++)
        {
            // First wave gets all extra groups plus its base share
            // This ensures first wave is heavily PMC-focused
            int groupsForThisWave = groupsPerWave + (waveIdx == 0 ? extraGroups : 0);

            for (int i = 0; i < groupsForThisWave && groupIndex < groupsByZones.Count; i++)
            {
                var groupByZone = groupsByZones[groupIndex];

                // Find the wave with this wave index to get timing
                var waveTime = location.Waves.FirstOrDefault(w => w.WildSpawnType == WildSpawnType.assault && w.Number == waveIdx);

                if (waveTime != null)
                {
                    // Create PMC boss with random spawn time within the wave window
                    var pmcBoss = GeneratePmcAsBoss(groupByZone.GroupSize, _modConfig.PmcBotDifficulty, groupByZone.ZoneName);

                    // Add some randomization to spawn time within the wave window
                    if (waveTime.TimeMax.HasValue && waveTime.TimeMin.HasValue && waveTime.TimeMax > waveTime.TimeMin)
                    {
                        pmcBoss.Time = randomUtil.GetInt(waveTime.TimeMin.Value, waveTime.TimeMax.Value);
                    }
                    else if (waveTime.TimeMin.HasValue)
                    {
                        pmcBoss.Time = waveTime.TimeMin.Value;
                    }

                    pmcBoss.IsRandomTimeSpawn = true;
                    location.BossLocationSpawn.Add(pmcBoss);
                }
                else
                {
                    // Fallback: spawn at the beginning if wave info not found
                    location.BossLocationSpawn.Add(GeneratePmcAsBoss(groupByZone.GroupSize, _modConfig.PmcBotDifficulty, groupByZone.ZoneName));
                }

                groupIndex++;
            }
        }

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] location.BossLocationSpawn '{locationId}': {JsonSerializer.Serialize(location.BossLocationSpawn)}", LogTextColor.Blue);
        }
    }

    List<ZoneGroupSize> SeparateGroupsByZones(List<string> zones,
        List<int> groups)
    {
        var shuffledZones = ShuffleZonesArray(zones);
        var groupsPool = new List<int>(groups);
        var result = new List<ZoneGroupSize>();

        foreach (var zoneName in shuffledZones)
        {
            if (!groupsPool.Any()) break;

            var groupSize = groupsPool[groupsPool.Count - 1];
            groupsPool.RemoveAt(groupsPool.Count - 1);

            result.Add(new ZoneGroupSize
            {
                ZoneName = zoneName,
                GroupSize = groupSize
            });
        }

        return result;
    }

    BossLocationSpawn GeneratePmcAsBoss(int groupSize,
        string difficulty, string zone)
    {
        var supports = new List<BossSupport>();
        var escortAmount = "0";

        var type = randomUtil.GetBool() ? "pmcBEAR" : "pmcUSEC";

        if (groupSize > 1)
        {
            escortAmount = $"{groupSize - 1}";

            supports.Add(new BossSupport
            {
                BossEscortType = type,
                BossEscortDifficulty = new ListOrT<string>([difficulty], null),
                BossEscortAmount = escortAmount
            });
        }

        return new BossLocationSpawn
        {
            BossChance = 100,
            BossDifficulty = difficulty,
            BossEscortAmount = escortAmount,
            BossEscortDifficulty = difficulty,
            BossEscortType = type,
            BossName = type,
            IsBossPlayer = true,
            BossZone = zone,
            IsRandomTimeSpawn = false,
            ShowOnTarkovMap = false,
            ShowOnTarkovMapPvE = false,
            Time = -1,
            TriggerId = "",
            TriggerName = "",
            ForceSpawn = null,
            IgnoreMaxBots = true,
            DependKarma = null,
            DependKarmaPVE = null,
            Supports = supports,
            SptId = null,
            SpawnMode = new List<string> { "regular", "pve" }
        };
    }

    List<int> SplitMaxAmountIntoGroups(int maxAmount, int maxGroupSize)
    {
        var result = new List<int>();
        var remainingAmount = maxAmount;

        do
        {
            var generatedGroupSize = randomUtil.GetInt(1, maxGroupSize);
            if (generatedGroupSize > remainingAmount)
            {
                result.Add(remainingAmount);
                remainingAmount = 0;
            }
            else
            {
                result.Add(generatedGroupSize);
                remainingAmount -= generatedGroupSize;
            }
        } while (remainingAmount > 0);

        return result;
    }

    List<string> ShuffleZonesArray(List<string> array)
    {
        return randomUtil.Shuffle(array);
    }

    void GenerateWavesWithPmcAndScavs(
        LocationBase location,
        List<string> zones,
        int escapeTimeLimit,
        int maxBots,
        int maxScavGroupSize,
        int currentWaveNumber)
    {
        int waveCount = _modConfig.AssaultWaveCount;
        int raidDurationSeconds = escapeTimeLimit * 60;

        // Generate wave times dynamically based on config
        List<int> waveTimes = GenerateWaveTimes(waveCount, raidDurationSeconds);

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] '{location.Id.ToLowerInvariant()}' generating {waveCount} waves with bot replenishment at times: {JsonSerializer.Serialize(waveTimes)}",
                LogTextColor.Blue);
        }

        // Each wave gets fresh bots based on configured PMC percentage
        var pmcRatio = _modConfig.PmcPercentage / 100.0;
        var scavRatio = (100.0 - _modConfig.PmcPercentage) / 100.0;

        for (int i = 0; i < waveTimes.Count; i++)
        {
            // Calculate PMC count for this wave based on configured percentage
            int wavePmcCount = (int)Math.Ceiling(maxBots * pmcRatio);

            // Calculate Scav count for this wave (remaining percentage)
            int waveScavCount = (int)Math.Ceiling(maxBots * scavRatio);

            // Generate PMC groups for this wave
            var pmcGroups = SplitMaxAmountIntoGroups(wavePmcCount, _modConfig.MaxPmcGroupSize);
            var pmcGroupsByZones = SeparateGroupsByZones(zones, pmcGroups);

            // Generate Scav groups for this wave (includes PMCs as part of scav waves)
            var totalWaveCount = wavePmcCount + waveScavCount;
            var allGroups = SplitMaxAmountIntoGroups(totalWaveCount, maxScavGroupSize);
            var allGroupsByZones = SeparateGroupsByZones(zones, allGroups);

            if (_modConfig.Debug)
            {
                logger.LogWithColor(
                    $"[Unda] '{location.Id.ToLowerInvariant()}' wave {i + 1}: {wavePmcCount} PMCs + {waveScavCount} Scavs = {totalWaveCount} total bots in groups",
                    LogTextColor.Blue);
            }

            // Waves get progressively harder: early waves are normal, later waves are hard
            string difficulty = i < waveTimes.Count / 2 ? "normal" : "hard";

            // Create assault waves with combined PMC and Scav groups
            // PMCs spawn as assault type with player difficulty
            CreateCombinedAssaultWaves(pmcGroupsByZones, allGroupsByZones, location, difficulty, waveTimes[i],
                ref currentWaveNumber, _modConfig.PmcBotDifficulty);
        }
    }

    void CreateCombinedAssaultWaves(
        List<ZoneGroupSize> pmcGroups,
        List<ZoneGroupSize> allGroups,
        LocationBase locationBase,
        string difficulty,
        int timeMin,
        ref int currentWaveNumber,
        string pmcDifficulty)
    {
        var timeMax = timeMin + 120;

        // Add PMC groups first
        foreach (var pmcGroup in pmcGroups)
        {
            var wave = GenerateWave(
                WildSpawnType.assault,
                pmcGroup.ZoneName,
                pmcDifficulty,
                currentWaveNumber++,
                0,
                pmcGroup.GroupSize,
                timeMin,
                timeMax);
            locationBase.Waves.Add(wave);
        }

        // Add remaining Scav groups
        foreach (var scavGroup in allGroups)
        {
            var wave = GenerateWave(
                WildSpawnType.assault,
                scavGroup.ZoneName,
                difficulty,
                currentWaveNumber++,
                0,
                scavGroup.GroupSize,
                timeMin,
                timeMax);
            locationBase.Waves.Add(wave);
        }
    }

    void ReplaceScavWaves(LocationBase location)
    {
        // This method is no longer used, keeping for backwards compatibility
    }

    void CleanWaves(LocationBase locationBase)
    {
        locationBase.Waves.Clear();
    }

    int GenerateMarksmanWaves(
        LocationBase locationBase,
        HashSet<string> zones,
        int maxGroupSize)
    {
        var num = 0;
        var minGroupSize = maxGroupSize > 1 ? 1 : 0;

        foreach (var zone in zones)
        {
            locationBase.Waves.Add(
                GenerateWave(
                    WildSpawnType.marksman,
                    zone,
                    "hard",
                    num++,
                    minGroupSize,
                    maxGroupSize,
                    60,
                    90));
        }

        return num;
    }

    void GenerateAssaultWaves(
        LocationBase location,
        List<string> zones,
        int escapeTimeLimit,
        int maxAssaultScavAmount,
        int maxScavGroupSize,
        int currentWaveNumber)
    {
        int waveCount = _modConfig.AssaultWaveCount;
        int raidDurationSeconds = escapeTimeLimit * 60;

        // Generate wave times dynamically based on config
        List<int> waveTimes = GenerateWaveTimes(waveCount, raidDurationSeconds);

        if (_modConfig.Debug)
        {
            logger.LogWithColor(
                $"[Unda] '{location.Id.ToLowerInvariant()}' generating {waveCount} assault waves at times: {JsonSerializer.Serialize(waveTimes)}",
                LogTextColor.Blue);
        }

        // Each wave gets fresh bots based on configured PMC percentage
        var scavRatio = (100.0 - _modConfig.PmcPercentage) / 100.0;

        for (int i = 0; i < waveTimes.Count; i++)
        {
            // Calculate bot count per wave using same logic as initial MaxBots
            int waveMaxBots = maxAssaultScavAmount;

            // Remaining percentage of available bots for scavs this wave
            int waveScavCount = (int)Math.Ceiling(waveMaxBots * scavRatio);

            // Split scavs for this wave into groups
            var waveGroups = SplitMaxAmountIntoGroups(waveScavCount, maxScavGroupSize);
            var waveGroupsByZones = SeparateGroupsByZones(zones, waveGroups);

            if (_modConfig.Debug)
            {
                logger.LogWithColor(
                    $"[Unda] '{location.Id.ToLowerInvariant()}' wave {i + 1}: {waveScavCount} scavs in groups {JsonSerializer.Serialize(waveGroupsByZones)}",
                    LogTextColor.Blue);
            }

            // Waves get progressively harder: early waves are normal, later waves are hard
            string difficulty = i < waveTimes.Count / 2 ? "normal" : "hard";
            CreateAssaultWaves(waveGroupsByZones, location, difficulty, waveTimes[i],
                ref currentWaveNumber);
        }
    }

    List<int> GenerateWaveTimes(int waveCount, int raidDurationSeconds)
    {
        var waveTimes = new List<int>();

        if (waveCount <= 0)
        {
            return waveTimes;
        }

        // First wave always starts at 60 seconds
        waveTimes.Add(60);

        if (waveCount == 1)
        {
            return waveTimes;
        }

        // For multiple waves, distribute remaining waves evenly
        // Calculate interval between waves based on remaining raid time
        int remainingTime = raidDurationSeconds - 60;
        int interval = remainingTime / waveCount;

        for (int i = 1; i < waveCount; i++)
        {
            int waveTime = 60 + (interval * i);
            waveTimes.Add(waveTime);
        }

        return waveTimes;
    }

    void CreateAssaultWaves(
        List<ZoneGroupSize> groupsByZones,
        LocationBase locationBase,
        string difficulty,
        int timeMin,
        ref int currentWaveNumber)
    {
        var timeMax = timeMin + 120;

        foreach (var zoneByGroup in groupsByZones)
        {
            var wave = GenerateWave(
                WildSpawnType.assault,
                zoneByGroup.ZoneName,
                difficulty,
                currentWaveNumber++,
                0,
                zoneByGroup.GroupSize,
                timeMin,
                timeMax);
            locationBase.Waves.Add(wave);
        }
    }

    Wave GenerateWave(
        WildSpawnType botType,
        string zoneName,
        string difficulty,
        int number,
        int slotsMin,
        int slotsMax,
        int timeMin,
        int timeMax)
    {
        return new Wave
        {
            BotPreset = difficulty,
            BotSide = "Savage",
            KeepZoneOnSpawn = false,
            SpawnPoints = zoneName,
            WildSpawnType = botType,
            IsPlayers = false,
            Number = number,
            SlotsMin = slotsMin,
            SlotsMax = slotsMax,
            TimeMin = timeMin,
            TimeMax = timeMax,
            SpawnMode = ["regular", "pve"]
        };
    }
}
