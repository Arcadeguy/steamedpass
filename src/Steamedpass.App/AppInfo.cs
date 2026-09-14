using System.Reflection;

namespace Steamedpass.App;

/// <summary>App version, shown in the title bar and the Settings About section.</summary>
internal static class AppInfo
{
    public static string Version { get; } =
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "0.0.0";
}
