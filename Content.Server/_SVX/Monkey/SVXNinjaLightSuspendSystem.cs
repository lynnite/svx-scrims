using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared._SVX.Monkey;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._SVX.Monkey;

public sealed class SVXNinjaLightSuspendSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float Radius = 7f;

    private static readonly TimeSpan SuspendDuration = TimeSpan.FromSeconds(10);

    private EntityQuery<PoweredLightComponent> _poweredQuery;
    private EntityQuery<PointLightComponent> _pointLightQuery;

    private readonly Dictionary<EntityUid, (List<LightSuspend> Lights, TimeSpan RestoreAt)> _suspensions = new();

    private readonly List<EntityUid> _expired = new();

    private readonly record struct LightSuspend(EntityUid Uid, bool IsFixture, bool WasEnabled);

    public override void Initialize()
    {
        base.Initialize();

        _poweredQuery = GetEntityQuery<PoweredLightComponent>();
        _pointLightQuery = GetEntityQuery<PointLightComponent>();

        SubscribeLocalEvent<SVXNinjaLightSuspendActionEvent>(OnSuspendAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_suspensions.Count == 0)
            return;

        var now = _timing.CurTime;
        foreach (var (user, suspension) in _suspensions)
        {
            if (now < suspension.RestoreAt)
                continue;

            foreach (var light in suspension.Lights)
            {
                if (Deleted(light.Uid))
                    continue;

                if (light.IsFixture)
                {
                    if (_poweredQuery.HasComponent(light.Uid))
                        _poweredLight.SetState(light.Uid, true);
                }
                else if (_pointLightQuery.HasComponent(light.Uid))
                {
                    _pointLight.SetEnabled(light.Uid, light.WasEnabled);
                }
            }

            _expired.Add(user);
        }

        foreach (var user in _expired)
        {
            _suspensions.Remove(user);
        }

        _expired.Clear();
    }

    private void OnSuspendAction(SVXNinjaLightSuspendActionEvent ev)
    {
        var user = ev.Performer;
        if (TerminatingOrDeleted(user))
            return;

        var lights = LookupNearbyLights(user);
        if (lights.Count == 0)
            return;

        foreach (var light in lights)
        {
            if (light.IsFixture)
            {
                _poweredLight.SetState(light.Uid, false);
            }
            else
            {
                _pointLight.SetEnabled(light.Uid, false);
            }
        }

        _suspensions[user] = (lights, _timing.CurTime + SuspendDuration);
    }

    private List<LightSuspend> LookupNearbyLights(EntityUid user)
    {
        var results = new List<LightSuspend>();
        if (!HasComp<TransformComponent>(user))
            return results;

        HashSet<EntityUid> nearby = new();
        _lookup.GetEntitiesInRange(user, Radius, nearby, LookupFlags.StaticSundries);
        _lookup.GetEntitiesInRange(user, Radius, nearby, LookupFlags.Dynamic | LookupFlags.Sundries);

        foreach (var uid in nearby)
        {
            if (Deleted(uid))
                continue;

            if (_poweredQuery.HasComponent(uid))
            {
                results.Add(new LightSuspend(uid, IsFixture: true, WasEnabled: false));
                continue;
            }

            if (_pointLightQuery.TryGetComponent(uid, out var pointLight) && pointLight.Enabled)
            {
                results.Add(new LightSuspend(uid, IsFixture: false, WasEnabled: true));
            }
        }

        return results;
    }
}
