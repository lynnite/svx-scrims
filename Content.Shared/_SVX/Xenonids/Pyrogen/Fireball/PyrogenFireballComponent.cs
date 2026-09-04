using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._SVX.Xenonids.Pyrogen;

[RegisterComponent]
public sealed partial class PyrogenFireballComponent : Component
{
    [DataField]
    public EntProtoId FireSpawn = "SVXTileFireHumanoidOnly";

    [DataField]
    public int PlasmaCost = 80;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public EntProtoId ProjectilePrototype = "SVXProjectilePyrogenFireball";

    [DataField]
    public float ProjectileSpeed = 7.5f;

    [DataField]
    public float ProjectileLifetime = 10f;

    [DataField]
    public float Range = 8f;

    [DataField]
    public float MaxRange = 7f;

    [DataField]
    public int FireRange = 1;

    [DataField]
    public int Intensity = 20;

    [DataField]
    public int Duration = 20;

    [DataField]
    public int CameraShakeShakes = 2;

    [DataField]
    public int CameraShakeStrength = 1;

    [DataField]
    public SoundSpecifier ExplosionSound = new SoundPathSpecifier("/Audio/Effects/explosion_small2.ogg");
}

public sealed partial class PyrogenFireballActionEvent : WorldTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class PyrogenFireballDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    public PyrogenFireballDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
