using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Pyrogen;

[RegisterComponent]
public sealed partial class PyrogenComponent : Component
{
    [DataField]
    public EntProtoId FireSpawn = "RMCTileFire";
}
