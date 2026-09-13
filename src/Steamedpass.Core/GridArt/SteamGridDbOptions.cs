namespace Steamedpass.Core.GridArt;

/// <summary>
/// Fixed filter vocabularies for SteamGridDB queries, matching UWPHook's
/// settings (https://github.com/BrianLima/UWPHook), MIT License,
/// Copyright (c) 2016 Brian Lima.
/// </summary>
public static class SteamGridDbOptions
{
    public static readonly string[] Styles = { "any", "alternate", "blurred", "white_logo", "material", "no_logo" };
    public static readonly string[] Types = { "any", "static", "animated" };
    public static readonly string[] Nsfw = { "false", "any", "true" };
    public static readonly string[] Humor = { "false", "any", "true" };

    public static string BuildQueryParameters(int styleIndex, int typeIndex, int nsfwIndex, int humorIndex, string? dimensions)
    {
        string style = Styles[Clamp(styleIndex, Styles.Length)];
        string type = Types[Clamp(typeIndex, Types.Length)];
        string nsfw = Nsfw[Clamp(nsfwIndex, Nsfw.Length)];
        string humor = Humor[Clamp(humorIndex, Humor.Length)];

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(dimensions))
        {
            parts.Add($"dimensions={dimensions}");
        }

        if (type != "any") parts.Add($"types={type}");
        if (style != "any") parts.Add($"styles={style}");
        if (nsfw != "any") parts.Add($"nsfw={nsfw}");
        if (humor != "any") parts.Add($"humor={humor}");

        return string.Join("&", parts);
    }

    private static int Clamp(int index, int length) => index >= 0 && index < length ? index : 0;
}
