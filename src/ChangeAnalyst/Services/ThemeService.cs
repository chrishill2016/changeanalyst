using ChangeAnalyst.Services.Storage;
using MudBlazor;

namespace ChangeAnalyst.Services;

/// <summary>Dark mode by default; the user's choice is remembered on the device.</summary>
public sealed class ThemeService(ILocalStore store)
{
    private const string DarkModeKey = "darkMode";

    public MudTheme Theme { get; } = new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#7e6fff",
            Secondary = "#ff4081",
        },
        PaletteLight = new PaletteLight
        {
            Primary = "#594ae2",
            Secondary = "#ff4081",
        },
    };

    public bool IsDarkMode { get; private set; } = true;

    public async Task LoadAsync() =>
        IsDarkMode = await store.GetAsync<bool?>(LocalStores.Meta, DarkModeKey) ?? true;

    public async Task ToggleAsync()
    {
        IsDarkMode = !IsDarkMode;
        await store.PutAsync(LocalStores.Meta, DarkModeKey, IsDarkMode);
    }
}
