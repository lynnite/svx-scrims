using Robust.Shared.GameStates;

namespace Content.Shared._SVX.Clumsy;

/// <summary>
/// Added to an item to make the holder or wearer clumsy while equipped or held
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SVXClumsyItemComponent : Component
{
    [DataField]
    public SVXClumsyComponent Clumsy = new();
}