using System.Reflection;

namespace ChangeAnalyst.Services;

public static class AppVersion
{
    /// <summary>The project's &lt;Version&gt;, stamped with the build number by the pipeline.</summary>
    public static string Current { get; } =
        typeof(AppVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "0.0.0";
}
