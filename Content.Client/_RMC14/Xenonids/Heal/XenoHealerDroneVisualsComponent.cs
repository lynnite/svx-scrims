using System.Collections.Generic;
using System.Numerics;

namespace Content.Client._RMC14.Xenonids.Heal;

[RegisterComponent]
public sealed partial class XenoHealerDroneVisualsComponent : Component
{
    public bool BeamVisible;

    public List<EntityUid> BeamSegments = new();

    public Vector2 LastBeamFrom;

    public Vector2 LastBeamTo;
}
