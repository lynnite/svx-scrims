using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Pyrogen;

[RegisterComponent]
public sealed partial class PyrogenDashComponent : Component
{
    [DataField]
    public int PlasmaCost = 75;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField]
    public float Range = 6f;

    [DataField]
    public float ThrowSpeed = 22f;

    [DataField]
    public int Intensity = 20;

    [DataField]
    public int Duration = 15;
}

public sealed partial class PyrogenDashActionEvent : WorldTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class PyrogenDashDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public NetCoordinates Origin;

    [DataField]
    public NetCoordinates Coordinates;

    public PyrogenDashDoAfterEvent(NetCoordinates origin, NetCoordinates coordinates)
    {
        Origin = origin;
        Coordinates = coordinates;
    }
}
