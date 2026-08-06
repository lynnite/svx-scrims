using System.Numerics;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Heal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoHealerDroneSystem))]
public sealed partial class XenoHealerDroneChannelComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Performer;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField]
    public TimeSpan HealInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan NextHealAt;

    [DataField]
    public float WalkModifier = -0.15f;

    [DataField]
    public float SprintModifier = -0.15f;

    [DataField]
    public float MaxRange = 7f;

    [DataField]
    public FixedPoint2 HealAmount = 12;

    [DataField]
    public FixedPoint2 PlasmaTransferAmount = 5;

    [DataField]
    public EntProtoId BeamPrototype = "SVXHealerDroneBeam";

    [DataField]
    public List<EntityUid> BeamLines = new();

    [DataField]
    public bool SpeedModifierApplied;
}
