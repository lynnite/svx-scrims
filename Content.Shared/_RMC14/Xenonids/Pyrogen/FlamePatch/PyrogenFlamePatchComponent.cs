using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Pyrogen;

[RegisterComponent]
public sealed partial class PyrogenFlameChargeComponent : Component
{
    [DataField]
    public EntProtoId FireSpawn = "SVXTileFireHumanoidOnly";

    [DataField]
    public int PlasmaCost = 90;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.5);

    [DataField]
    public int FireRange = 6;

    [DataField]
    public TimeSpan LayerDelay = TimeSpan.FromMilliseconds(500);

    [DataField]
    public int Intensity = 30;

    [DataField]
    public int Duration = 20;

    [DataField]
    public int CameraShakeShakes = 2;

    [DataField]
    public int CameraShakeStrength = 1;

    [DataField]
    public SoundSpecifier ExplosionSound = new SoundPathSpecifier("/Audio/Effects/explosion_small2.ogg");
}

public sealed partial class PyrogenFlameChargeActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class PyrogenFlameChargeDoAfterEvent : SimpleDoAfterEvent;
