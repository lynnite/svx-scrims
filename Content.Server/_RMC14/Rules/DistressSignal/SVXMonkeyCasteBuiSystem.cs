using Content.Server.GameTicking;
using Content.Shared.Actions;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared._SVX.Monkey;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class SVXMonkeyCasteBuiSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SVXMonkeyCastePickSystem _caste = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<SVXMonkeyComponent>(SVXMonkeyCasteUIKey.Key, subs =>
        {
            subs.Event<SVXMonkeyCastePickBuiMsg>(OnPicked);
        });

        SubscribeLocalEvent<SVXMonkeyLoadoutComponent, MapInitEvent>(OnLoadoutMapInit);
        SubscribeLocalEvent<SVXMonkeyLoadoutComponent, SVXMonkeyLoadoutActionEvent>(OnLoadoutAction);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnLoadoutMapInit(Entity<SVXMonkeyLoadoutComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnLoadoutAction(Entity<SVXMonkeyLoadoutComponent> ent, ref SVXMonkeyLoadoutActionEvent args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var mindId, out _))
            return;

        OpenForMob(ent.Owner, mindId);
    }

    public void OpenForMob(EntityUid monkey, EntityUid mindId)
    {
        if (_gameTicker.RunLevel != GameRunLevel.InRound || !TryComp(monkey, out ActorComponent? actor))
            return;

        _ui.OpenUi(monkey, SVXMonkeyCasteUIKey.Key, actor.PlayerSession);
        SendState(monkey, mindId);
    }

    private void SendState(EntityUid monkey, EntityUid mindId)
    {
        if (!_ui.HasUi(monkey, SVXMonkeyCasteUIKey.Key))
            return;

        var desired = _caste.GetDesired(mindId);
        var options = _caste.BuildOptionList(mindId, desired);
        var cooldown = _caste.CooldownRemaining(mindId);
        _ui.SetUiState(monkey, SVXMonkeyCasteUIKey.Key, new SVXMonkeyCasteBuiState(options, cooldown));
    }

    private void OnPicked(Entity<SVXMonkeyComponent> monkey, ref SVXMonkeyCastePickBuiMsg args)
    {
        var actor = args.Actor;
        if (!_mind.TryGetMind(actor, out var mindId, out _))
            return;

        var gear = new ProtoId<StartingGearPrototype>(args.CastId);
        _caste.TryPickCaste(mindId, gear);

        if (!Deleted(monkey.Owner))
            SendState(monkey.Owner, mindId);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
    }
}

