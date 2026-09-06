using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Roles;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.GameTicking;
using Content.Shared._SVX.Monkey;
using Robust.Server.GameObjects;
using Content.Server.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class SVXMonkeyRespawnSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SVXMonkeyCastePickSystem _caste = default!;
    [Dependency] private readonly SharedRMCGameTickerSystem _rmcGameTicker = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly EntProtoId MonkeyMob = "SVXMonkeyXenoBase";

    public override void Initialize()
    {
        SubscribeLocalEvent<SVXMonkeyComponent, MobStateChangedEvent>(OnMonkeyMobStateChanged);

        SubscribeLocalEvent<RequestMonkeyJoinEvent>(OnRequestMonkeyJoin);
    }


    private void OnMonkeyMobStateChanged(Entity<SVXMonkeyComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!_mind.TryGetMind(ent.Owner, out var mindId, out _))
            return;

        if (!TryComp(ent, out ActorComponent? actor))
            return;

        ReborrowMonkey(actor.PlayerSession, mindId);
    }

    private void ReborrowMonkey(ICommonSession session, EntityUid mindId)
    {
        if (!CanRunMonkeyRound())
        {
            Log.Warning($"[monkey] Dropping re-borrow for {session.Name}: monkey round no longer running.");
            return;
        }

        var spawnPoint = PickSpawnPoint();
        if (spawnPoint == null)
        {
            Log.Warning($"[monkey] No xeno spawn point available for {session.Name}.");
            if (session.AttachedEntity is { } ghostEnt)
                _popup.PopupEntity("No xeno spawn point is available right now.", ghostEnt, ghostEnt);
            return;
        }

        var monkeyEnt = SpawnAtPosition(MonkeyMob, Transform(spawnPoint.Value).Coordinates);

        var gearId = _caste.ConsumeChosenCaste(mindId);
        var gear = _prototypes.Index<StartingGearPrototype>(gearId);
        _stationSpawning.EquipStartingGear(monkeyEnt, gear);

        _rmcGameTicker.PlayerJoinGame(session);

        _mind.TransferTo(mindId, monkeyEnt);

        Log.Info($"[monkey] Re-borrowed {session.Name} into {ToPrettyString(monkeyEnt):monkey}");
    }

    private void OnRequestMonkeyJoin(RequestMonkeyJoinEvent ev)
    {
        if (ev.Player.AttachedEntity == null)
            Log.Info($"[monkey] Join request from {ev.Player.Name}: no attached ghost, will create a fresh mind.");
        TrySpawnMonkeyForJoin(ev.Player);
    }

    private void TrySpawnMonkeyForJoin(ICommonSession session)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound || !TryGetMonkeyRule(out _))
        {
            Log.Warning($"[monkey] Refusing latejoin for {session.Name}: not an active in-round monkey round.");
            if (session.AttachedEntity is { } ghostEnt)
                _popup.PopupEntity("You cannot join a monkey round right now.", ghostEnt, ghostEnt);
            return;
        }

        var mindId = EntityUid.Invalid;
        if (session.AttachedEntity is { } attached && _mind.TryGetMind(attached, out mindId, out _))
        {
        }
        else
        {
            mindId = _mind.CreateMind(session.UserId);
        }

        ReborrowMonkey(session, mindId);
    }

    private bool CanRunMonkeyRound()
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound)
            return false;

        if (!TryGetMonkeyRule(out var rule) ||
            rule.MonkeyRoundResolved ||
            rule.Result != null)
        {
            return false;
        }

        return AnyLivingSurvivor();
    }

    private bool TryGetMonkeyRule(out CMDistressSignalRuleComponent rule)
    {
        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.Monkey)
            {
                rule = comp;
                return true;
            }
        }

        rule = null!;
        return false;
    }

    private EntityUid? PickSpawnPoint()
    {
        var points = new List<EntityUid>();
        var query = AllEntityQuery<Content.Shared._RMC14.Spawners.XenoSpawnPointComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!TerminatingOrDeleted(uid))
                points.Add(uid);
        }

        if (points.Count == 0)
            return null;

        return _random.Pick(points);
    }

    private bool AnyLivingSurvivor()
    {
        var query = EntityQueryEnumerator<Content.Shared._RMC14.Survivor.RMCSurvivorComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            if (mobState.CurrentState == MobState.Alive)
                return true;
        }

        return false;
    }
}
