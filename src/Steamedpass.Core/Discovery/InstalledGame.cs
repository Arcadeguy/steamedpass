namespace Steamedpass.Core.Discovery;

/// <summary>
/// An installed Microsoft Store / Xbox Game Pass (UWP) app discovered on this machine.
/// </summary>
public sealed record InstalledGame(string Name, string LogoDirectory, string Aumid, string Executable)
{
    public override string ToString() => $"{Name} ({Aumid})";
}
