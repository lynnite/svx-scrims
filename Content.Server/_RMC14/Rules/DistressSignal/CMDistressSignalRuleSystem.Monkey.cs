using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Survivor;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private void CheckMonkeyRoundShouldEnd(CMDistressSignalRuleComponent distress)
    {
        if (distress.Result != null || distress.MonkeyRoundResolved || distress.StartTime == null)
        {
            Log.Info(
                $"[monkey-diag] CheckMonkeyRoundShouldEnd skipping: result={distress.Result}, " +
                $"resolved={distress.MonkeyRoundResolved}, startTimeSet={distress.StartTime != null}");
            return;
        }

        var time = Timing.CurTime;
        var elapsed = time - distress.StartTime.Value;

        if (elapsed >= distress.MonkeyRoundDuration)
        {
            EndMonkeyRound(distress, survivorWin: true);
            return;
        }

        if (!CheckAliveSurvivors())
        {
            Log.Info(
                $"[monkey-diag] no survivors alive at t={elapsed.TotalSeconds:0.0}s of " +
                $"{distress.MonkeyRoundDuration.TotalSeconds:0}s; ending as monkey win");
            EndMonkeyRound(distress, survivorWin: false);
        }
    }

    private bool CheckAliveSurvivors()
    {
        var survivors = 0;
        var query = EntityQueryEnumerator<RMCSurvivorComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState))
        {
            survivors++;
            if (mobState.CurrentState == MobState.Alive)
                return true;
        }

        Log.Info($"[monkey-diag] CheckAliveSurvivors: {survivors} total RMCSurvivor entities, none alive");
        return false;
    }

    private void EndMonkeyRound(CMDistressSignalRuleComponent distress, bool survivorWin)
    {
        if (distress.Result != null || distress.MonkeyRoundResolved)
            return;

        if (!distress.AutoEnd)
            return;

        if (distress.StartTime == null || Timing.CurTime - distress.StartTime.Value < distress.RoundEndCheckDelay)
            return;

        distress.MonkeyRoundResolved = true;

        if (survivorWin)
        {
            Log.Info("[monkey] Round timer reached: survivors win the monkey round.");
            distress.Result = DistressSignalRuleResult.MajorMarineVictory;
            distress.CustomRoundEndMessage = "svx-monkey-roundend-survivor-victory";
        }
        else
        {
            Log.Info("[monkey] All survivors dead: monkey horde wins, survivors lose.");
            distress.Result = DistressSignalRuleResult.MajorXenoVictory;
            distress.CustomRoundEndMessage = "svx-monkey-roundend-monkey-victory";
        }

        _roundEnd.EndRound();
    }
}
