using Content.Shared.Projectiles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Pyrogen;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PyrogenFireballProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Fired;

    [DataField, AutoNetworkedField]
    public EntProtoId FireSpawn = "RMCTileFire";

    [DataField, AutoNetworkedField]
    public int FireRange = 1;

    [DataField, AutoNetworkedField]
    public int Intensity = 20;

    [DataField, AutoNetworkedField]
    public int Duration = 20;

    [DataField, AutoNetworkedField]
    public int CameraShakeShakes = 2;

    [DataField, AutoNetworkedField]
    public int CameraShakeStrength = 1;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ExplosionSound = new SoundPathSpecifier("/Audio/Effects/explosion_small2.ogg");
}
