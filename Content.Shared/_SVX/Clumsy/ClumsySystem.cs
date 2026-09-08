using Content.Shared.CCVar;
using Content.Shared.Chemistry.Hypospray.Events;
using Content.Shared.Climbing.Components;
using Content.Shared.Climbing.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._SVX.Clumsy;

public sealed class ClumsySystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SVXClumsyComponent, SelfBeforeHyposprayInjectsEvent>(BeforeHyposprayEvent);
        SubscribeLocalEvent<SVXClumsyComponent, SelfBeforeDefibrillatorZapsEvent>(BeforeDefibrillatorZapsEvent);
        SubscribeLocalEvent<SVXClumsyComponent, SelfBeforeGunShotEvent>(BeforeGunShotEvent);
        SubscribeLocalEvent<SVXClumsyComponent, CatchAttemptEvent>(OnCatchAttempt);
        SubscribeLocalEvent<SVXClumsyComponent, SelfBeforeClimbEvent>(OnBeforeClimbEvent);

        SubscribeLocalEvent<SVXClumsyItemComponent, GotEquippedEvent>(OnItemEquipped);
        SubscribeLocalEvent<SVXClumsyItemComponent, GotUnequippedEvent>(OnItemUnequipped);
        SubscribeLocalEvent<SVXClumsyItemComponent, GotEquippedHandEvent>(OnItemHandEquipped);
        SubscribeLocalEvent<SVXClumsyItemComponent, GotUnequippedHandEvent>(OnItemHandUnequipped);
        SubscribeLocalEvent<SVXClumsyItemComponent, MapInitEvent>(OnItemMapInit);
    }

    #region Clumsy interaction events
    private void BeforeHyposprayEvent(Entity<SVXClumsyComponent> ent, ref SelfBeforeHyposprayInjectsEvent args)
    {

        if (!ent.Comp.ClumsyHypo)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        args.TargetGettingInjected = args.EntityUsingHypospray;
        args.InjectMessageOverride = Loc.GetString(ent.Comp.HypoFailedMessage);
        _audio.PlayPredicted(ent.Comp.ClumsySound, ent, args.EntityUsingHypospray);
    }

    private void BeforeDefibrillatorZapsEvent(Entity<SVXClumsyComponent> ent, ref SelfBeforeDefibrillatorZapsEvent args)
    {

        if (!ent.Comp.ClumsyDefib)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        args.DefibTarget = args.EntityUsingDefib;
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

    }

    private void OnCatchAttempt(Entity<SVXClumsyComponent> ent, ref CatchAttemptEvent args)
    {

        if (!ent.Comp.ClumsyCatching)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(args.Item).Id });
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        args.Cancelled = true;

        if (ent.Comp.CatchingFailDamage != null)
            _damageable.TryChangeDamage(ent, ent.Comp.CatchingFailDamage, origin: args.Item);

        if (_net.IsClient)
            return;

        var selfMessage = Loc.GetString(ent.Comp.CatchingFailedMessageSelf, ("item", ent.Owner), ("catcher", Identity.Entity(ent.Owner, EntityManager)));
        var othersMessage = Loc.GetString(ent.Comp.CatchingFailedMessageOthers, ("item", ent.Owner), ("catcher", Identity.Entity(ent.Owner, EntityManager)));
        _popup.PopupEntity(selfMessage, ent.Owner, ent.Owner);
        _popup.PopupEntity(othersMessage, ent.Owner, Filter.PvsExcept(ent.Owner), true);
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);
    }

    private void BeforeGunShotEvent(Entity<SVXClumsyComponent> ent, ref SelfBeforeGunShotEvent args)
    {

        if (!ent.Comp.ClumsyGuns)
            return;

        if (args.Gun.Comp.ClumsyProof)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(args.Gun).Id });
        var rand = new System.Random(seed);
        if (!rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        if (ent.Comp.GunShootFailDamage != null)
            _damageable.TryChangeDamage(ent, ent.Comp.GunShootFailDamage, origin: ent);

        _stun.TryParalyze(ent, ent.Comp.GunShootFailStunTime, true);

        _audio.PlayPvs(ent.Comp.GunShootFailSound, ent);
        _audio.PlayPvs(ent.Comp.ClumsySound, ent);

        _popup.PopupEntity(Loc.GetString(ent.Comp.GunFailedMessage), ent, ent);
        args.Cancel();
    }

    private void OnBeforeClimbEvent(Entity<SVXClumsyComponent> ent, ref SelfBeforeClimbEvent args)
    {
        if (!ent.Comp.ClumsyVaulting)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_timing.CurTick.Value, GetNetEntity(ent).Id });
        var rand = new System.Random(seed);
        if (!_cfg.GetCVar(CCVars.GameTableBonk) && !rand.Prob(ent.Comp.ClumsyDefaultCheck))
            return;

        HitHeadClumsy(ent, args.BeingClimbedOn);

        _audio.PlayPredicted(ent.Comp.ClumsySound, ent, ent);

        _audio.PlayPredicted(ent.Comp.TableBonkSound, ent, ent);

        var gettingPutOnTableName = Identity.Entity(args.GettingPutOnTable, EntityManager);
        var puttingOnTableName = Identity.Entity(args.PuttingOnTable, EntityManager);

        if (args.PuttingOnTable == ent.Owner)
        {
            _popup.PopupPredicted(
                Loc.GetString(ent.Comp.VaulingFailedMessageSelf, ("bonkable", args.BeingClimbedOn)),
                Loc.GetString(ent.Comp.VaulingFailedMessageOthers, ("victim", gettingPutOnTableName), ("bonkable", args.BeingClimbedOn)),
                ent,
                ent);
        }
        else
        {
            _popup.PopupPredicted(
                Loc.GetString(ent.Comp.VaulingFailedMessageForced,
                    ("bonker", puttingOnTableName),
                    ("victim", gettingPutOnTableName),
                    ("bonkable", args.BeingClimbedOn)),
                ent,
                null);
        }


        args.Cancel();
    }

    private void OnItemMapInit(Entity<SVXClumsyItemComponent> ent, ref MapInitEvent args)
    {
        if (Transform(ent).ParentUid is { Valid: true } wearer && HasComp<SVXClumsyComponent>(wearer) == false)
        {
            ApplyClumsy(wearer, ent.Comp);
        }
    }

    private void OnItemEquipped(Entity<SVXClumsyItemComponent> ent, ref GotEquippedEvent args)
    {
        ApplyClumsy(args.Equipee, ent.Comp);
    }

    private void OnItemUnequipped(Entity<SVXClumsyItemComponent> ent, ref GotUnequippedEvent args)
    {
        RemoveClumsy(args.Equipee);
    }

    private void OnItemHandEquipped(Entity<SVXClumsyItemComponent> ent, ref GotEquippedHandEvent args)
    {
        ApplyClumsy(args.User, ent.Comp);
    }

    private void OnItemHandUnequipped(Entity<SVXClumsyItemComponent> ent, ref GotUnequippedHandEvent args)
    {
        RemoveClumsy(args.User);
    }
    #endregion

    #region Helper functions
    private void ApplyClumsy(EntityUid user, SVXClumsyItemComponent itemComp)
    {
        var clumsy = EnsureComp<SVXClumsyComponent>(user);

        clumsy.ClumsySound = itemComp.Clumsy.ClumsySound;
        clumsy.ClumsyDefaultCheck = itemComp.Clumsy.ClumsyDefaultCheck;
        clumsy.ClumsyDefaultStunTime = itemComp.Clumsy.ClumsyDefaultStunTime;
        clumsy.ClumsyHypo = itemComp.Clumsy.ClumsyHypo;
        clumsy.ClumsyDefib = itemComp.Clumsy.ClumsyDefib;
        clumsy.ClumsyGuns = itemComp.Clumsy.ClumsyGuns;
        clumsy.ClumsyCatching = itemComp.Clumsy.ClumsyCatching;
        clumsy.ClumsyVaulting = itemComp.Clumsy.ClumsyVaulting;

        Dirty(user, clumsy);
    }

    private void RemoveClumsy(EntityUid user)
    {
        RemComp<SVXClumsyComponent>(user);
    }

    public void HitHeadClumsy(Entity<SVXClumsyComponent> target, EntityUid table)
    {
        var stunTime = target.Comp.ClumsyDefaultStunTime;

        if (TryComp<BonkableComponent>(table, out var bonkComp))
        {
            stunTime = bonkComp.BonkTime;
            if (bonkComp.BonkDamage != null)
                _damageable.TryChangeDamage(target, bonkComp.BonkDamage, true);
        }

        _stun.TryParalyze(target, stunTime, true);
    }
    #endregion
}