using ChangeAnalyst.Services.Sync;
using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Tests.Fakes;

public sealed class FakeRemoteSyncClient(bool isConfigured = true) : IRemoteSyncClient
{
    public bool IsConfigured => isConfigured;

    public List<SyncRecord> Pushed { get; } = [];

    public List<DateTimeOffset?> PullCursors { get; } = [];

    public List<SyncRecord> ToPull { get; } = [];

    public DateTimeOffset ServerTime { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public Exception? FailWith { get; set; }

    /// <summary>Runs during a push, to simulate edits made while it is in flight.</summary>
    public Func<Task>? DuringPush { get; set; }

    public async Task PushAsync(IReadOnlyList<SyncRecord> records, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        if (DuringPush is not null)
        {
            await DuringPush();
        }

        Pushed.AddRange(records);
    }

    public Task<PullResponse> PullAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        if (FailWith is not null)
        {
            throw FailWith;
        }

        PullCursors.Add(since);
        var records = ToPull.ToList();
        ToPull.Clear();
        return Task.FromResult(new PullResponse(records, ServerTime));
    }
}
