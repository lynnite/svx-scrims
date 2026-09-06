using Robust.Shared.Player;

namespace Content.Shared._SVX.Monkey;

public sealed class RequestMonkeyJoinEvent(ICommonSession player) : EntityEventArgs
{
    public ICommonSession Player { get; } = player;
}
