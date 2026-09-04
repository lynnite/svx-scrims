using System.Numerics;
using Content.Shared._SVX.Xenonids.Heal;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._SVX.Xenonids.Heal;

public sealed class XenoHealerDroneVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoHealerDroneChannelComponent, ComponentStartup>(OnChannelStartup);
        SubscribeLocalEvent<XenoHealerDroneChannelComponent, ComponentShutdown>(OnChannelShutdown);
        SubscribeLocalEvent<XenoHealerDroneChannelComponent, EntityTerminatingEvent>(OnChannelTerminating);
    }

    private void OnChannelStartup(Entity<XenoHealerDroneChannelComponent> ent, ref ComponentStartup args)
    {
        var visuals = EnsureComp<XenoHealerDroneVisualsComponent>(ent.Owner);
        UpdateBeam(ent.Owner, ent.Comp, visuals);
    }

    private void OnChannelShutdown(Entity<XenoHealerDroneChannelComponent> ent, ref ComponentShutdown args)
    {
        RemoveBeam(ent.Owner);
    }

    private void OnChannelTerminating(Entity<XenoHealerDroneChannelComponent> ent, ref EntityTerminatingEvent args)
    {
        RemoveBeam(ent.Owner);
    }

    private void RemoveBeam(EntityUid performer)
    {
        if (!TryComp<XenoHealerDroneVisualsComponent>(performer, out var visuals))
            return;

        RemoveBeamSegments(visuals);
        visuals.BeamVisible = false;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoHealerDroneChannelComponent, XenoHealerDroneVisualsComponent>();
        while (query.MoveNext(out var uid, out var channel, out var visuals))
        {
            if (!channel.Active || channel.Target is not { } target || !Exists(target))
            {
                RemoveBeamSegments(visuals);
                visuals.BeamVisible = false;
                continue;
            }

            UpdateBeam(uid, channel, visuals, target);
        }
    }

    private void UpdateBeam(EntityUid performer, XenoHealerDroneChannelComponent channel, XenoHealerDroneVisualsComponent visuals, EntityUid? target = null)
    {
        target ??= channel.Target;
        if (target is not { } targetUid || !Exists(targetUid))
        {
            RemoveBeamSegments(visuals);
            visuals.BeamVisible = false;
            return;
        }

        var from = _transform.GetWorldPosition(performer);
        var to = _transform.GetWorldPosition(targetUid);
        var direction = to - from;
        var distance = direction.Length();

        if (distance > channel.MaxRange)
        {
            RemoveBeamSegments(visuals);
            visuals.BeamVisible = false;
            return;
        }

        if (distance <= 0.01f)
        {
            RemoveBeamSegments(visuals);
            visuals.BeamVisible = false;
            return;
        }

        var normalizedDirection = direction.Normalized();
        const float segmentLength = 1f;
        var segmentCount = Math.Max(1, (int)MathF.Ceiling(distance / segmentLength));

        while (visuals.BeamSegments.Count < segmentCount)
        {
            var segmentUid = Spawn("SVXHealerDroneBeam", _transform.GetMapCoordinates(performer));
            visuals.BeamSegments.Add(segmentUid);
        }

        while (visuals.BeamSegments.Count > segmentCount)
        {
            var extraSegment = visuals.BeamSegments[^1];
            visuals.BeamSegments.RemoveAt(visuals.BeamSegments.Count - 1);
            if (Exists(extraSegment))
                QueueDel(extraSegment);
        }

        var beamAngle = direction.ToWorldAngle();
        var beamLocalDirection = new Vector2(1f, 0f);
        for (var i = 0; i < visuals.BeamSegments.Count; i++)
        {
            var beamSegmentUid = visuals.BeamSegments[i];
            if (!Exists(beamSegmentUid))
                continue;

            var offset = normalizedDirection * (i * segmentLength + segmentLength * 0.5f);
            var segmentWorldPosition = from + offset;
            var segmentXform = Transform(beamSegmentUid);
            segmentXform.ActivelyLerping = false;
            _transform.SetWorldPosition(beamSegmentUid, segmentWorldPosition);
            _transform.SetWorldRotation(beamSegmentUid, beamAngle);

            if (TryComp<SpriteComponent>(beamSegmentUid, out var sprite))
            {
                _sprite.SetScale((beamSegmentUid, sprite), Vector2.One);
                _sprite.SetOffset((beamSegmentUid, sprite), new Vector2(0f, 0f));
            }
        }

        visuals.BeamVisible = true;
        visuals.LastBeamFrom = from;
        visuals.LastBeamTo = to;
    }

    private void RemoveBeamSegments(XenoHealerDroneVisualsComponent visuals)
    {
        if (visuals.BeamSegments.Count == 0)
            return;

        foreach (var beamSegment in visuals.BeamSegments)
        {
            if (Exists(beamSegment))
                QueueDel(beamSegment);
        }

        visuals.BeamSegments.Clear();
    }
}
