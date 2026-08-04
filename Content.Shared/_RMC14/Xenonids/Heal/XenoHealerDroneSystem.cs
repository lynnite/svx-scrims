using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Heal;

public sealed class XenoHealerDroneSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TemporarySpeedModifiersSystem _speed = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedXenoHealSystem _xenoHeal = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoComponent, XenoHealerDroneChannelActionEvent>(OnChannelAction);
        SubscribeLocalEvent<XenoHealerDroneChannelComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnChannelAction(Entity<XenoComponent> ent, ref XenoHealerDroneChannelActionEvent args)
    {
        if (TryComp(args.Performer, out XenoHealerDroneChannelComponent? existingChannel) && existingChannel.Active)
        {
            args.Handled = true;
            return;
        }

        if (!_rmcActions.TryUseAction(args))
            return;

        if (!TryComp(args.Performer, out XenoPlasmaComponent? plasma))
            return;

        if (plasma.Plasma < 50)
            return;

        if (args.Target == args.Performer || !Exists(args.Target))
            return;

        var target = args.Target;

        if (!TryComp(target, out XenoComponent? _) || !HasComp<DamageableComponent>(target))
            return;

        if (_mobState.IsDead(target) || !_hive.FromSameHive(args.Performer, target))
            return;

        args.Handled = true;

        var channel = EnsureComp<XenoHealerDroneChannelComponent>(args.Performer);
        channel.Performer = args.Performer;
        channel.Target = target;
        channel.Active = true;
        channel.SpeedModifierApplied = false;
        channel.NextHealAt = _timing.CurTime;
        Dirty(args.Performer, channel);

        _popup.PopupClient("Channel active", args.Performer, args.Performer, PopupType.Medium);
    }

    private void OnRejuvenate(Entity<XenoHealerDroneChannelComponent> ent, ref RejuvenateEvent args)
    {
        RemCompDeferred<XenoHealerDroneChannelComponent>(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<XenoHealerDroneChannelComponent>();
        while (query.MoveNext(out var uid, out var channel))
        {
            if (!channel.Active)
                continue;

            if (!TryComp(uid, out XenoPlasmaComponent? plasma))
            {
                RemCompDeferred<XenoHealerDroneChannelComponent>(uid);
                continue;
            }

            if (plasma.Plasma < 50)
            {
                RemCompDeferred<XenoHealerDroneChannelComponent>(uid);
                continue;
            }

            var now = _timing.CurTime;
            if (channel.Target is not { } target || !Exists(target) || _mobState.IsDead(target))
            {
                channel.Target = null;
                channel.Active = false;
                channel.SpeedModifierApplied = false;
                Dirty(uid, channel);
                continue;
            }

            if (!_hive.FromSameHive(uid, target) || _transform.GetMapId(uid) != _transform.GetMapId(target) ||
                Vector2.Distance(_transform.GetMapCoordinates(uid).Position, _transform.GetMapCoordinates(target).Position) > channel.MaxRange)
            {
                channel.Target = null;
                channel.Active = false;
                channel.SpeedModifierApplied = false;
                Dirty(uid, channel);
                continue;
            }

            if (now >= channel.NextHealAt)
            {
                if (!TryComp(target, out DamageableComponent? _))
                {
                    channel.Target = null;
                    channel.Active = false;
                    Dirty(uid, channel);
                    continue;
                }

                channel.NextHealAt = now + channel.HealInterval;
                _xenoHeal.CreateHealStacks(target, channel.HealAmount, channel.HealInterval, 1, channel.HealInterval);
                Dirty(uid, channel);
            }

            if (!channel.SpeedModifierApplied)
            {
                _speed.ModifySpeed(uid, new List<TemporarySpeedModifierSet>
                {
                    new(TimeSpan.FromSeconds(3), channel.WalkModifier, channel.SprintModifier)
                });
                channel.SpeedModifierApplied = true;
                Dirty(uid, channel);
            }
        }
    }
}
