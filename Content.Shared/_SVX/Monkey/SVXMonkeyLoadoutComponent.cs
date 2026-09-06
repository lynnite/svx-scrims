using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._SVX.Monkey;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SVXMonkeyLoadoutComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionMonkeyLoadout";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}

