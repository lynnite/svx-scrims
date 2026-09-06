using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared._RMC14.Rules;
using Content.Shared._SVX.Monkey;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class SVXMonkeyCastePickSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    public static readonly ProtoId<StartingGearPrototype> DefaultGear = "SVXMonkeyCasteTactical";

    public sealed record CasteDef(ProtoId<StartingGearPrototype> Gear, string Name, TimeSpan Unlock);

    public static readonly CasteDef[] Castes =
    {
        new(DefaultGear, "Tactical", TimeSpan.Zero),
        new("SVXMonkeyCasteNinja", "Ninja", TimeSpan.FromMinutes(1)),
        new("SVXMonkeyCastePizzaDriver", "Pizza Driver", TimeSpan.FromSeconds(90)),
        new("SVXMonkeyCasteBreacher", "Breacher", TimeSpan.FromSeconds(90)),
        new("SVXMonkeyCasteCLF", "CLF Soldier", TimeSpan.FromMinutes(2)),
        new("SVXMonkeyCasteChimp", "Chimp", TimeSpan.FromMinutes(2)),
        new("SVXMonkeyCasteGrenadier", "Grenadier", TimeSpan.FromMinutes(5)),
        new("SVXMonkeyCasteCommando", "Commando", TimeSpan.FromMinutes(5)),
    };

    private readonly Dictionary<ProtoId<StartingGearPrototype>, CasteDef> _castesByGear = Castes.ToDictionary(c => c.Gear);

    public float CooldownRemaining(EntityUid mindId)
    {
        if (_lastPick.TryGetValue(mindId, out var last))
        {
            var remain = Cooldown - (_timing.CurTime - last);
            if (remain > TimeSpan.Zero)
                return (float)remain.TotalSeconds;
        }

        return 0;
    }

    public ProtoId<StartingGearPrototype>? GetDesired(EntityUid mindId)
    {
        if (_desired.TryGetValue(mindId, out var gear))
            return gear;

        return null;
    }

    private readonly Dictionary<EntityUid, ProtoId<StartingGearPrototype>> _desired = new();

    private readonly Dictionary<EntityUid, TimeSpan> _lastPick = new();

    public readonly record struct PickResult(bool Success, string Reason, string? Detail = null);

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _desired.Clear();
        _lastPick.Clear();
    }

    public List<ProtoId<StartingGearPrototype>> UnlockedForMind(EntityUid mindId)
    {
        var rule = GetActiveMonkeyRule();
        var roundTime = TryGetElapsed(rule, out var t) ? t : TimeSpan.Zero;
        var result = new List<ProtoId<StartingGearPrototype>>();
        foreach (var def in Castes)
        {
            if (def.Unlock <= roundTime)
                result.Add(def.Gear);
        }

        return result;
    }

    public TimeSpan RoundElapsed
    {
        get
        {
            var rule = GetActiveMonkeyRule();
            return TryGetElapsed(rule, out var t) ? t : TimeSpan.Zero;
        }
    }

    public List<SVXMonkeyCasteOption> BuildOptionList(EntityUid mindId, ProtoId<StartingGearPrototype>? currentGear)
    {
        var elapsed = RoundElapsed;
        var options = new List<SVXMonkeyCasteOption>(Castes.Length);
        foreach (var def in Castes)
        {
            string hint;
            if (def.Unlock > elapsed)
                hint = $"unlocks in {(def.Unlock - elapsed).TotalSeconds:0}s";
            else if (def.Gear != currentGear && def.Gear != DefaultGear && CooldownRemaining(mindId) > 0)
                hint = "change locked (cooldown)";
            else
                hint = string.Empty;

            options.Add(new SVXMonkeyCasteOption(
                def.Gear.Id,
                def.Name,
                def.Unlock <= elapsed,
                def.Gear == currentGear,
                hint));
        }

        return options;
    }

    private CMDistressSignalRuleComponent? GetActiveMonkeyRule()
    {
        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.Monkey)
                return comp;
        }

        return null;
    }

    private bool TryGetElapsed(CMDistressSignalRuleComponent? rule, out TimeSpan elapsed)
    {
        if (rule?.StartTime is { } start)
        {
            elapsed = _timing.CurTime - start;
            return true;
        }

        elapsed = default;
        return false;
    }

    public PickResult TryPickCaste(EntityUid mindId, ProtoId<StartingGearPrototype> gear)
    {
        if (!_castesByGear.TryGetValue(gear, out var def))
            return new PickResult(false, "unknown-caste",
                $"'{gear}' is not a selectable monkey caste gear.");

        var rule = GetActiveMonkeyRule();
        if (rule == null)
            return new PickResult(false, "no-round", "No active monkey round to evolve in.");

        if (!TryGetElapsed(rule, out var elapsed))
            return new PickResult(false, "round-not-started", "Round start time not set yet.");

        if (def.Unlock > elapsed)
        {
            var need = def.Unlock - elapsed;
            return new PickResult(false, "locked",
                $"'{def.Name}' unlocks in {need.TotalSeconds:0}s.");
        }

        if (_lastPick.TryGetValue(mindId, out var last))
        {
            var remain = Cooldown - (_timing.CurTime - last);
            if (remain > TimeSpan.Zero)
                return new PickResult(false, "cooldown",
                    $"You can change caste again in {remain.TotalSeconds:0}s.");
        }

        _desired[mindId] = gear;
        _lastPick[mindId] = _timing.CurTime;
        return new PickResult(true, "ok", $"Caste set to {gear}");
    }

    public ProtoId<StartingGearPrototype> ConsumeChosenCaste(EntityUid mindId)
    {
        if (_desired.Remove(mindId, out var gear))
            return gear;

        return DefaultGear;
    }
}
