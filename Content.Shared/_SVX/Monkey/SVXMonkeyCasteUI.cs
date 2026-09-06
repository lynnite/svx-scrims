using Robust.Shared.Serialization;

namespace Content.Shared._SVX.Monkey;

[Serializable, NetSerializable]
public enum SVXMonkeyCasteUIKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SVXMonkeyCasteOption(
    string castId,
    string displayName,
    bool unlocked,
    bool current,
    string unlockHint)
{
    public readonly string CastId = castId;
    public readonly string DisplayName = displayName;
    public readonly bool Unlocked = unlocked;
    public readonly bool Current = current;
    public readonly string UnlockHint = unlockHint;
}

[Serializable, NetSerializable]
public sealed class SVXMonkeyCasteBuiState(
    List<SVXMonkeyCasteOption> options,
    float cooldownRemainingSeconds)
    : BoundUserInterfaceState
{
    public readonly List<SVXMonkeyCasteOption> Options = options;
    public readonly float CooldownRemainingSeconds = cooldownRemainingSeconds;
}

[Serializable, NetSerializable]
public sealed class SVXMonkeyCastePickBuiMsg(string castId) : BoundUserInterfaceMessage
{
    public readonly string CastId = castId;
}
