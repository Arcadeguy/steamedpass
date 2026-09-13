using System.Text.Json.Serialization;

namespace Steamedpass.Core.GridArt;

public sealed record SteamGridDbGame(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);

public sealed record SteamGridDbImage([property: JsonPropertyName("url")] string Url);

internal sealed class SteamGridDbResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T[]? Data { get; set; }
}
