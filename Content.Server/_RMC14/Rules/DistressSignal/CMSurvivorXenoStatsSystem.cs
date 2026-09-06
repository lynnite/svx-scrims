using System.Linq;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Survivor;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Content.Shared._SVX.Monkey;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed class CMSurvivorXenoStatsSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedMindSystem _minds = default!;

    private enum Side : byte
    {
        Survivor,
        Xeno,
    }

    private sealed record LastDamageEntry(EntityUid Attacker, Side AttackerSide, double Damage);

    private sealed class PlayerStats
    {
        public string Name = string.Empty;
        public EntityUid? Body;
        public int Kills;
        public double Damage;
    }

    private readonly Dictionary<EntityUid, LastDamageEntry> _lastDamager = new();

    private readonly Dictionary<Side, Dictionary<EntityUid, PlayerStats>> _stats = new()
    {
        [Side.Survivor] = new(),
        [Side.Xeno] = new(),
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, ProjectileDamageDealtEvent>(OnXenoProjectileHit);
        SubscribeLocalEvent<SVXMonkeyComponent, ProjectileDamageDealtEvent>(OnMonkeyProjectileHit);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<RMCSurvivorComponent, ProjectileDamageDealtEvent>(OnSurvivorProjectileHit);

        SubscribeLocalEvent<DamageableComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndTextAppend);
    }

    public override void Shutdown()
    {
        _lastDamager.Clear();
        _stats[Side.Survivor].Clear();
        _stats[Side.Xeno].Clear();
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _lastDamager.Clear();
        _stats[Side.Survivor].Clear();
        _stats[Side.Xeno].Clear();
    }

    private void OnXenoProjectileHit(Entity<XenoComponent> xeno, ref ProjectileDamageDealtEvent args)
    {
        if (args.Origin is not { } origin ||
            !HasComp<RMCSurvivorComponent>(origin) ||
            args.DamageDelta is not { } delta ||
            !HasPositiveDamage(delta))
        {
            return;
        }

        RegisterDamage(
            victim: xeno.Owner, victimSide: Side.Xeno,
            attacker: origin, attackerSide: Side.Survivor,
            damage: delta.GetTotal().Double());
    }

    private void OnSurvivorProjectileHit(Entity<RMCSurvivorComponent> survivor, ref ProjectileDamageDealtEvent args)
    {
        if (args.Origin is not { } origin ||
            !IsEnemyEntity(origin) ||
            args.DamageDelta is not { } delta ||
            !HasPositiveDamage(delta))
        {
            return;
        }

        RegisterDamage(
            victim: survivor.Owner, victimSide: Side.Survivor,
            attacker: origin, attackerSide: Side.Xeno,
            damage: delta.GetTotal().Double());
    }

    private void OnMonkeyProjectileHit(Entity<SVXMonkeyComponent> monkey, ref ProjectileDamageDealtEvent args)
    {
        if (args.Origin is not { } origin ||
            !HasComp<RMCSurvivorComponent>(origin) ||
            args.DamageDelta is not { } delta ||
            !HasPositiveDamage(delta))
        {
            return;
        }

        RegisterDamage(
            victim: monkey.Owner, victimSide: Side.Xeno,
            attacker: origin, attackerSide: Side.Survivor,
            damage: delta.GetTotal().Double());
    }

    private void OnMeleeHit(Entity<MeleeWeaponComponent> weapon, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0 || !IsEnemyEntity(args.User))
            return;

        var swing = (args.BaseDamage + args.BonusDamage).GetTotal().Double();
        if (swing <= 0)
            return;

        var attacker = args.User;
        foreach (var hit in args.HitEntities)
        {
            if (!HasComp<RMCSurvivorComponent>(hit))
                continue;

            RegisterDamage(
                victim: hit, victimSide: Side.Survivor,
                attacker: attacker, attackerSide: Side.Xeno,
                damage: swing);
        }
    }

    private static bool HasPositiveDamage(DamageSpecifier delta) => delta.GetTotal().Float() > 0;

    private bool IsMonkeyRound()
    {
        var rules = EntityQueryEnumerator<ActiveGameRuleComponent, CMDistressSignalRuleComponent>();
        while (rules.MoveNext(out _, out _, out var rule))
        {
            if (rule.Monkey)
                return true;
        }

        return false;
    }

    private bool IsEnemyEntity(EntityUid ent)
    {
        return HasComp<SVXMonkeyComponent>(ent) || HasComp<XenoComponent>(ent);
    }

    private void RegisterDamage(EntityUid victim, Side victimSide, EntityUid attacker, Side attackerSide, double damage)
    {
        if (attackerSide == victimSide || damage <= 0)
            return;

        _lastDamager[victim] = new LastDamageEntry(attacker, attackerSide, damage);

        if (TryGetStats(attacker, attackerSide, out var stats))
            stats.Damage += damage;
    }


    private void OnMobStateChanged(Entity<DamageableComponent> victim, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        Side victimSide;
        if (HasComp<XenoComponent>(victim) || HasComp<SVXMonkeyComponent>(victim))
            victimSide = Side.Xeno;
        else if (HasComp<RMCSurvivorComponent>(victim))
            victimSide = Side.Survivor;
        else
            return;

        if (args.Origin is { } origin &&
            TryGetSide(origin, out var originSide) &&
            originSide != victimSide)
        {
            if (TryGetStats(origin, originSide, out var originStats))
                originStats.Kills++;
            return;
        }

        CreditLastDamager(victim, victimSide);
    }

    private void CreditLastDamager(EntityUid victim, Side victimSide)
    {
        if (!_lastDamager.Remove(victim, out var last))
            return;

        if (last.AttackerSide == victimSide)
            return;

        if (TryGetStats(last.Attacker, last.AttackerSide, out var stats))
            stats.Kills++;
    }


    private bool TryGetSide(EntityUid ent, out Side side)
    {
        if (HasComp<XenoComponent>(ent) || HasComp<SVXMonkeyComponent>(ent))
        {
            side = Side.Xeno;
            return true;
        }

        if (HasComp<RMCSurvivorComponent>(ent))
        {
            side = Side.Survivor;
            return true;
        }

        side = default;
        return false;
    }

    private bool TryGetStats(EntityUid ent, Side side, out PlayerStats stats)
    {
        stats = null!;

        if (!_minds.TryGetMind(ent, out var mindId, out var mind) ||
            mind.UserId == null)
        {
            return false;
        }

        var bucket = _stats[side];
        if (bucket.TryGetValue(mindId, out var existing))
        {
            stats = existing;

            if (LooksPlaceholder(stats.Name) && ent.IsValid() && !Deleted(ent))
                stats.Name = Name(ent);

            return true;
        }

        stats = new PlayerStats
        {
            Name = ResolveName(ent, mind),
            Body = ent,
        };
        bucket[mindId] = stats;
        return true;
    }

    private static bool LooksPlaceholder(string name)
    {
        return string.IsNullOrWhiteSpace(name) ||
               name == "<unknown>" ||
               name.Contains("Unknown", StringComparison.Ordinal) ||
               name.StartsWith("Human", StringComparison.Ordinal) ||
               name.StartsWith("MobHuman", StringComparison.Ordinal);
    }

    private string ResolveName(EntityUid ent, MindComponent mind)
    {
        if (ent.IsValid() && !Deleted(ent) && !LooksPlaceholder(Name(ent)))
            return Name(ent);

        if (!string.IsNullOrWhiteSpace(mind.CharacterName))
            return mind.CharacterName;

        return "<unknown>";
    }


    private void OnRoundEndTextAppend(RoundEndTextAppendEvent ev)
    {
        if (!_gameTicker.IsGameRuleActive<CMDistressSignalRuleComponent>())
            return;

        var survivors = RankedSide(Side.Survivor);
        var enemies = RankedSide(Side.Xeno);

        if (survivors.Count == 0 && enemies.Count == 0)
            return;

        var monkey = IsMonkeyRound();

        if (survivors.Count > 0)
            AppendSection(ev, "rmc-survivors-roundend-header", survivors, "green");

        if (enemies.Count > 0)
        {
            var header = monkey ? "svx-monkey-roundend-monkeys-header" : "rmc-xenos-roundend-header";
            var color = monkey ? "red" : "purple";
            AppendSection(ev, header, enemies, color);
        }
    }

    private void AppendSection(RoundEndTextAppendEvent ev, string headerLoc, List<PlayerStats> entries, string color)
    {
        if (!string.IsNullOrWhiteSpace(ev.Text))
            ev.AddLine(string.Empty);

        ev.AddLine($"[bold][color={color}]{Loc.GetString(headerLoc)}[/color][/bold]");
        foreach (var stats in entries)
        {
            var kills = Math.Max(0, stats.Kills);
            var damage = Math.Max(0, (int)Math.Round(stats.Damage));
            var text = Loc.GetString("rmc-roundend-combat-stat", ("name", stats.Name), ("kills", kills), ("damage", damage));
            ev.AddLine($"[color={color}]{text}[/color]");
        }
    }

    private List<PlayerStats> RankedSide(Side side)
    {
        var list = new List<PlayerStats>();
        foreach (var (mindId, stats) in _stats[side])
        {
            if (stats.Kills <= 0 && stats.Damage <= 0)
                continue;

            FinalizeName(mindId, stats);
            list.Add(stats);
        }

        return list
            .OrderByDescending(s => s.Kills)
            .ThenByDescending(s => s.Damage)
            .ToList();
    }

    private void FinalizeName(EntityUid mindId, PlayerStats stats)
    {
        if (!LooksPlaceholder(stats.Name))
            return;

        if (stats.Body is { } body && body.IsValid() && !Deleted(body) && !LooksPlaceholder(Name(body)))
        {
            stats.Name = Name(body);
            return;
        }

        if (_minds.TryGetMind(mindId, out _, out var mind))
        {
            if (mind.CurrentEntity is { } current && current.IsValid() && !Deleted(current) &&
                !LooksPlaceholder(Name(current)))
            {
                stats.Name = Name(current);
                return;
            }

            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
            {
                stats.Name = mind.CharacterName;
                return;
            }
        }

        stats.Name = "<unknown>";
    }
}
