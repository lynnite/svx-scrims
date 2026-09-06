using Content.Shared.Administration;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Rules.DistressSignal;

[AnyCommand]
sealed class SVXMonkeyCasteCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protos = default!;

    public string Command => "svxcaste";
    public string Description => "Lists or picks your Monkey-game-mode caste kit.";
    public string Help => "svxcaste | svxcaste <casteId> (e.g. SVXMonkeyCasteChimp)";

    public SVXMonkeyCasteCommand()
    {
        IoCManager.InjectDependencies(this);
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player == null)
            return;

        var minds = _entManager.System<SharedMindSystem>();
        if (!minds.TryGetMind(player, out var mindId, out _))
        {
            shell.WriteLine("You don't have a mind.");
            return;
        }

        var pick = _entManager.System<SVXMonkeyCastePickSystem>();

        if (args.Length == 0 || args[0] is "list" or "ls" or "?")
        {
            var open = pick.UnlockedForMind(mindId);
            shell.WriteLine($"Unlocked monkey castes ({open.Count}):");
            foreach (var gear in open)
                shell.WriteLine($"  /svxcaste {gear.Id}");
            return;
        }

        if (!_protos.HasIndex<StartingGearPrototype>(args[0]))
        {
            shell.WriteError($"'{args[0]}' is not a valid starting-gear (caste) id.");
            return;
        }

        var result = pick.TryPickCaste(mindId, new ProtoId<StartingGearPrototype>(args[0]));
        if (result.Success)
        {
            shell.WriteLine(result.Detail ?? "Caste set.");
            shell.WriteLine("It applies on your next monkey respawn.");
        }
        else
        {
            shell.WriteError(result.Detail ?? result.Reason);
        }
    }
}
