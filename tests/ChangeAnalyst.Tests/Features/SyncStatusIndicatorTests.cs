using Bunit;
using ChangeAnalyst.Features.Status;
using ChangeAnalyst.Services.Sync;
using ChangeAnalyst.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace ChangeAnalyst.Tests.Features;

public class SyncStatusIndicatorTests : BunitContext, IAsyncLifetime
{
    private readonly FakeSyncService _sync = new();

    public SyncStatusIndicatorTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ISyncService>(_sync);
    }

    // MudBlazor registers services that only support async disposal.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    private string ChipText(IRenderedComponent<SyncStatusIndicator> cut) =>
        cut.Find("[data-testid=sync-status] button").TextContent.Trim();

    [Theory]
    [InlineData(true, true, false, 0, null, "Synced")]
    [InlineData(true, false, false, 0, null, "Offline")]
    [InlineData(true, false, false, 3, null, "Offline · 3")]
    [InlineData(true, true, true, 0, null, "Syncing…")]
    [InlineData(true, true, false, 2, null, "2 pending")]
    [InlineData(true, true, false, 0, "Sync failed", "Sync error")]
    [InlineData(false, true, false, 1, null, "Local only")]
    public void Shows_the_current_sync_state(bool configured, bool online, bool syncing, int pending, string? error, string expected)
    {
        _sync.SetStatus(new SyncStatus(configured, online, syncing, pending, null, error));

        var cut = Render<SyncStatusIndicator>();

        Assert.Equal(expected, ChipText(cut));
    }

    [Fact]
    public void Updates_when_the_status_changes()
    {
        var cut = Render<SyncStatusIndicator>();

        _sync.SetStatus(_sync.Status with { IsOnline = false, PendingChanges = 1 });

        cut.WaitForAssertion(() => Assert.Equal("Offline · 1", ChipText(cut)));
    }

    [Fact]
    public void Shows_a_snackbar_for_sync_notifications()
    {
        Render<MudSnackbarProvider>();
        Render<SyncStatusIndicator>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        _sync.Notify(new SyncNotification(true, "Sync failed: boom"));

        Assert.Contains(snackbar.ShownSnackbars, s => s.Message == "Sync failed: boom" && s.Severity == Severity.Error);
    }
}
