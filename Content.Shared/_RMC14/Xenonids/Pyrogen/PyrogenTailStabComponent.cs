using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Pyrogen;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PyrogenTailStabComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FireStacksOnHit = 6f;

    [DataField, AutoNetworkedField]
    public int IgnitionIntensity = 20;

    [DataField, AutoNetworkedField]
    public int IgnitionDuration = 20;

    [DataField, AutoNetworkedField]
    public int? MaxStacks = 20;
}
