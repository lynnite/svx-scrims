using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._SVX.Clumsy;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SVXClumsyComponent : Component
{

    [DataField]
    public SoundSpecifier ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    [DataField, AutoNetworkedField]
    public float ClumsyDefaultCheck = 0.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan ClumsyDefaultStunTime = TimeSpan.FromSeconds(2.5);

    [DataField]
    public SoundCollectionSpecifier TableBonkSound = new SoundCollectionSpecifier("TrayHit");

    [DataField, AutoNetworkedField]
    public TimeSpan GunShootFailStunTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public DamageSpecifier? GunShootFailDamage;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? CatchingFailDamage;

    [DataField]
    public SoundSpecifier GunShootFailSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg");

    [DataField, AutoNetworkedField]
    public bool ClumsyHypo = true;

    [DataField, AutoNetworkedField]
    public bool ClumsyDefib = true;

    [DataField, AutoNetworkedField]
    public bool ClumsyGuns = true;

    [DataField, AutoNetworkedField]
    public bool ClumsyCatching = true;

    [DataField, AutoNetworkedField]
    public bool ClumsyVaulting = true;

    [DataField]
    public LocId HypoFailedMessage = "clumsy-hypospray-fail-message";

    [DataField]
    public LocId GunFailedMessage = "clumsy-gun-fail-message";

    [DataField]
    public LocId CatchingFailedMessageSelf = "clumsy-catch-fail-message-user";

    [DataField]
    public LocId CatchingFailedMessageOthers = "clumsy-catch-fail-message-others";

    [DataField]
    public LocId VaulingFailedMessageSelf = "clumsy-vaulting-fail-message-user";

    [DataField]
    public LocId VaulingFailedMessageOthers = "clumsy-vaulting-fail-message-others";

    [DataField]
    public LocId VaulingFailedMessageForced = "clumsy-vaulting-fail-forced-message";
}
