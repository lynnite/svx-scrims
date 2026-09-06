using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[Serializable, NetSerializable]
public sealed class BurrowedLarvaStatusEvent(int larva, bool monkey = false) : EntityEventArgs
{
    public int Larva { get; } = larva;
    public bool Monkey { get; } = monkey;
}

