using Content.Server.Voting;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private static readonly TimeSpan ModeVoteDuration = TimeSpan.FromSeconds(10);

    private const string DistressPreset = "CMDistressSignal";
    private const string MonkeyPreset = "CMDistressSignalMonkey";

    private IVoteHandle? _modeVote;

    private void ConfigureNextRound()
    {
        var monkeyPlanets = _rmcPlanet.GetCandidatesInRotation(monkey: true);
        var monkeyAvailable = monkeyPlanets.Count > 0;
        if (!monkeyAvailable)
            return;

        if (_modeVote != null)
            return;

        var options = new List<(string text, object data)>
        {
            (Loc.GetString("svx-gamemode-distress"), DistressPreset),
            (Loc.GetString("svx-gamemode-monkey"), MonkeyPreset),
        };

        var vote = new VoteOptions
        {
            Title = Loc.GetString("svx-gamemode-vote-title"),
            Options = options,
            Duration = ModeVoteDuration,
        };
        vote.SetInitiatorOrServer(null);

        Log.Info("[modevote] creating game-mode vote");
        _modeVote = _voteManager.CreateVote(vote);
        _modeVote.OnFinished += (_, args) =>
        {
            _modeVote = null;
            if (args.Votes.Count == 0)
            {
                Log.Info("[modevote] game-mode vote finished with no options; leaving preset unchanged");
                return;
            }

            var distressVotes = args.Votes[0];
            var monkeyVotes = args.Votes.Count > 1 ? args.Votes[1] : 0;
            var chosen = monkeyVotes > distressVotes ? MonkeyPreset : DistressPreset;
            Log.Info($"[modevote] game-mode vote finished: distress={distressVotes} monkey={monkeyVotes} -> preset={chosen}");

            GameTicker.SetGamePreset(chosen);
        };
        _modeVote.OnCancelled += _ => _modeVote = null;
    }
}

