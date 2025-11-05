using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;

namespace Pulse;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "pulse.wavegenerator";
    public override string Name { get; init; } = "Pulse";
    public override string Author { get; init; } = "LumurkFox";
    public override List<string>? Contributors { get; init; } = new() { "Based on Unda by Barlog_M" };
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; } = new() { "li.barlog.unda" };
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new();
    public override string? Url { get; init; } = "https://github.com/lumurkfox/spt-pulse";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostSptModLoader + 1)]
public class PulseMod(ISptLogger<PulseMod> logger) : IOnLoad
{
    public Task OnLoad()
    {
        return Task.CompletedTask;
    }
}
