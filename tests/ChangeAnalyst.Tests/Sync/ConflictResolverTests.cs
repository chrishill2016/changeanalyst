using ChangeAnalyst.Services.Sync;
using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Tests.Sync;

public class ConflictResolverTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private static SyncRecord At(DateTimeOffset when) => new("Test", Id, "{}", when);

    [Fact]
    public void Incoming_wins_when_there_is_no_local_copy() =>
        Assert.True(ConflictResolver.IncomingWins(At(T0), null));

    [Fact]
    public void Newer_incoming_wins() =>
        Assert.True(ConflictResolver.IncomingWins(At(T0.AddMinutes(1)), At(T0)));

    [Fact]
    public void Older_incoming_loses() =>
        Assert.False(ConflictResolver.IncomingWins(At(T0), At(T0.AddMinutes(1))));

    [Fact]
    public void Tie_keeps_local() =>
        Assert.False(ConflictResolver.IncomingWins(At(T0), At(T0)));
}
