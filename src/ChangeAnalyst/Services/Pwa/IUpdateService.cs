namespace ChangeAnalyst.Services.Pwa;

/// <summary>Tells the UI when a new service worker (app version) is waiting.</summary>
public interface IUpdateService
{
    bool IsUpdateAvailable { get; }

    event Action? UpdateAvailable;

    Task InitializeAsync();

    Task ApplyUpdateAsync();
}
