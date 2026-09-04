using System.Numerics;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Stab;
using Content.Shared.Atmos.Components;
using Content.Shared.DoAfter;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._SVX.Xenonids.Pyrogen;

public sealed class PyrogenSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly RMCCameraShakeSystem _cameraShake = default!;
    [Dependency] private readonly RMCProjectileSystem _rmcProjectile = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PyrogenFireballComponent, PyrogenFireballActionEvent>(OnFireballAction);
        SubscribeLocalEvent<PyrogenFireballProjectileComponent, ProjectileHitEvent>(OnFireballProjectileHit);
        SubscribeLocalEvent<PyrogenFireballProjectileComponent, EntityTerminatingEvent>(OnFireballProjectileTerminating);
        SubscribeLocalEvent<PyrogenFlameChargeComponent, PyrogenFlameChargeActionEvent>(OnFlameChargeAction);
        SubscribeLocalEvent<PyrogenFlameChargeComponent, PyrogenFlameChargeDoAfterEvent>(OnFlameChargeDoAfter);
        SubscribeLocalEvent<PyrogenDashComponent, PyrogenDashActionEvent>(OnDashAction);
        SubscribeLocalEvent<PyrogenTailStabComponent, MeleeHitEvent>(OnPyrogenTailStabHit);
    }

    private void OnFireballAction(Entity<PyrogenFireballComponent> ent, ref PyrogenFireballActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
            return;

        args.Handled = true;

        var source = _transform.GetMapCoordinates(ent);
        var target = _transform.ToMapCoordinates(args.Target);
        if (source.MapId != target.MapId)
            return;

        var direction = target.Position - source.Position;
        if (direction.Length() > ent.Comp.Range)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
            return;

        if (_net.IsClient)
            return;

        if (direction.Length() < 0.1f)
            return;

        var projectile = Spawn(ent.Comp.ProjectilePrototype, source);
        var projectileComp = EnsureComp<PyrogenFireballProjectileComponent>(projectile);
        projectileComp.FireSpawn = ent.Comp.FireSpawn;
        projectileComp.FireRange = ent.Comp.FireRange;
        projectileComp.Intensity = ent.Comp.Intensity;
        projectileComp.Duration = ent.Comp.Duration;
        projectileComp.CameraShakeShakes = ent.Comp.CameraShakeShakes;
        projectileComp.CameraShakeStrength = ent.Comp.CameraShakeStrength;
        projectileComp.ExplosionSound = ent.Comp.ExplosionSound;
        Dirty(projectile, projectileComp);

        var timed = EnsureComp<TimedDespawnComponent>(projectile);
        timed.Lifetime = ent.Comp.ProjectileLifetime;

        _rmcProjectile.SetMaxRange(projectile, ent.Comp.MaxRange);
        _gun.ShootProjectile(projectile, direction, Vector2.Zero, ent, ent, speed: ent.Comp.ProjectileSpeed);
    }

    private void OnFireballProjectileHit(Entity<PyrogenFireballProjectileComponent> ent, ref ProjectileHitEvent args)
    {
        if (ent.Comp.Fired || _net.IsClient)
            return;

        ent.Comp.Fired = true;
        Dirty(ent);
        var coordinates = _transform.GetMoverCoordinates(ent);
        _rmcFlammable.SpawnFireDiamond(ent.Comp.FireSpawn, coordinates, ent.Comp.FireRange, ent.Comp.Intensity, ent.Comp.Duration);
        _audio.PlayPvs(ent.Comp.ExplosionSound, coordinates, AudioParams.Default.WithVolume(-4f));
        _cameraShake.ShakeCamera(Filter.Pvs(coordinates, entityMan: EntityManager), ent.Comp.CameraShakeShakes, ent.Comp.CameraShakeStrength);
    }

    private void OnFireballProjectileTerminating(Entity<PyrogenFireballProjectileComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Fired || _net.IsClient)
            return;

        ent.Comp.Fired = true;
        Dirty(ent);
        var coordinates = _transform.GetMoverCoordinates(ent);
        _rmcFlammable.SpawnFireDiamond(ent.Comp.FireSpawn, coordinates, ent.Comp.FireRange, ent.Comp.Intensity, ent.Comp.Duration);
        _audio.PlayPvs(ent.Comp.ExplosionSound, coordinates, AudioParams.Default.WithVolume(-4f));
        _cameraShake.ShakeCamera(Filter.Pvs(coordinates, entityMan: EntityManager), ent.Comp.CameraShakeShakes, ent.Comp.CameraShakeStrength);
    }

    private void OnFlameChargeAction(Entity<PyrogenFlameChargeComponent> ent, ref PyrogenFlameChargeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        var layerCount = Math.Max(1, (int)Math.Ceiling(ent.Comp.FireRange / 2f));
        var rootDuration = ent.Comp.Delay + ent.Comp.LayerDelay * layerCount + TimeSpan.FromMilliseconds(500);
        _slow.TryRoot(ent.Owner, rootDuration, true);

        var ev = new PyrogenFlameChargeDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.Delay, ev, ent)
        {
            BreakOnMove = true,
            RootEntity = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnFlameChargeDoAfter(Entity<PyrogenFlameChargeComponent> ent, ref PyrogenFlameChargeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (TerminatingOrDeleted(ent.Owner) || EntityManager.IsQueuedForDeletion(ent.Owner))
            return;

        args.Handled = true;
        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
            return;

        if (_net.IsClient)
            return;

        var originCoords = _transform.GetMoverCoordinates(ent);
        var owner = ent.Owner;
        var fireSpawn = ent.Comp.FireSpawn;
        var intensity = ent.Comp.Intensity;
        var duration = ent.Comp.Duration;
        var layerCount = Math.Max(1, (int)Math.Ceiling(ent.Comp.FireRange / 2f));
        var shakeShakes = ent.Comp.CameraShakeShakes;
        var shakeStrength = ent.Comp.CameraShakeStrength;
        var explosionSound = ent.Comp.ExplosionSound;

        for (var layer = 0; layer < layerCount; layer++)
        {
            var explosionOffsets = new List<Vector2>();
            explosionOffsets.Add(Vector2.Zero);
            for (var i = 0; i < 8; i++)
            {
                var randomOffset = new Vector2(
                    _random.Next(-3, 4),
                    _random.Next(-3, 4));

                if (randomOffset == Vector2.Zero)
                    continue;

                explosionOffsets.Add(randomOffset);
            }

            Timer.Spawn(ent.Comp.LayerDelay * layer, () =>
            {
                if (TerminatingOrDeleted(owner) || EntityManager.IsQueuedForDeletion(owner))
                    return;

                foreach (var offset in explosionOffsets)
                {
                    var explosionCoords = originCoords.Offset(offset);
                    _rmcExplosion.QueueExplosion(
                        _transform.ToMapCoordinates(explosionCoords),
                        "RMC",
                        1f,
                        0.5f,
                        1f,
                        owner,
                        canCreateVacuum: false);
                }

                foreach (var offset in explosionOffsets)
                {
                    if (offset == Vector2.Zero)
                        continue;

                    _rmcFlammable.SpawnSingleFire(
                        fireSpawn,
                        originCoords.Offset(offset),
                        intensity,
                        duration);
                }

                _audio.PlayPvs(explosionSound, originCoords, AudioParams.Default.WithVolume(-4f));
                _cameraShake.ShakeCamera(Filter.Pvs(originCoords, entityMan: EntityManager), shakeShakes, shakeStrength);
            });
        }
    }

    private void OnDashAction(Entity<PyrogenDashComponent> ent, ref PyrogenDashActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
            return;

        var origin = _transform.GetMapCoordinates(ent);
        var target = _transform.ToMapCoordinates(args.Target);
        if (origin.MapId != target.MapId)
            return;

        var direction = target.Position - origin.Position;
        if (direction.Length() < 0.1f)
            return;

        var length = Math.Min(direction.Length(), ent.Comp.Range);
        if (length < 0.1f)
            return;

        var normalizedDirection = direction.Normalized();
        _throwing.TryThrow(ent.Owner, normalizedDirection * length, ent.Comp.ThrowSpeed, animated: false, compensateFriction: true, doSpin: false);

        var originCoords = _transform.GetMoverCoordinates(ent.Owner);
        var steps = Math.Max(4, (int)MathF.Ceiling(length * 2f));
        for (var i = 1; i <= steps; i++)
        {
            var t = (float) i / steps;
            var trailOffset = normalizedDirection * (length * t);
            var trailCoords = originCoords.Offset(trailOffset);
            if (_random.Next(0, 100) < 35)
                trailCoords = trailCoords.Offset(new Vector2(_random.Next(-1, 2), _random.Next(-1, 2)));

            _rmcFlammable.SpawnSingleFire("SVXTileFireHumanoidOnly", trailCoords, ent.Comp.Intensity, ent.Comp.Duration);
        }

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/explosion_small2.ogg"), originCoords, AudioParams.Default.WithVolume(-4f));
        _cameraShake.ShakeCamera(Filter.Pvs(originCoords, entityMan: EntityManager), 2, 1);
    }

    private void OnPyrogenTailStabHit(Entity<PyrogenTailStabComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        foreach (var hit in args.HitEntities)
        {
            if (!TryComp(hit, out FlammableComponent? flammable))
                continue;

            if (!HasComp<XenoComponent>(hit) && _rmcFlammable.CanBeIgnited(hit, ent.Owner, ent.Comp.IgnitionIntensity))
            {
                _rmcFlammable.Ignite((hit, flammable), ent.Comp.IgnitionIntensity, ent.Comp.IgnitionDuration, ent.Comp.MaxStacks);
            }

            _rmcFlammable.AdjustStacks((hit, flammable), (int)ent.Comp.FireStacksOnHit);
        }
    }

}
