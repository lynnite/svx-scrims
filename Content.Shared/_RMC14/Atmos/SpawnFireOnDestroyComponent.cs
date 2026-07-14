using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class SpawnFireOnDestroyComponent : Component
{
    /// <summary>
    ///     Range of the fire.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Range = 3;

    /// <summary>
    ///     The fire prototype spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCTileFire";

    /// <summary>
    ///     The intensity of the fire.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Intensity = 20;

    /// <summary>
    ///     The duration of the fire.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? Duration = 15;
}
