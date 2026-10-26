namespace ChangeAnalyst.Services.Connectivity;

public interface IConnectivityService
{
    bool IsOnline { get; }

    /// <summary>Raised with the new online state whenever it changes.</summary>
    event Action<bool>? Changed;

    Task InitializeAsync();
}
