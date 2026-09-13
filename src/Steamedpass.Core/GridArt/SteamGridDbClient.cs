using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Steamedpass.Core.GridArt;

/// <summary>
/// Minimal SteamGridDB API v2 client. Adapted from UWPHook's SteamGridDbApi
/// (https://github.com/BrianLima/UWPHook), MIT License, Copyright (c) 2016 Brian Lima.
/// </summary>
public sealed class SteamGridDbClient
{
    private const string BaseUrl = "https://www.steamgriddb.com/api/v2/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public SteamGridDbClient(string apiKey)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<SteamGridDbGame[]> SearchGameAsync(string gameName)
    {
        var response = await _httpClient.GetAsync($"search/autocomplete/{Uri.EscapeDataString(gameName)}");

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("SteamGridDB API key is invalid.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<SteamGridDbGame>();
        }

        var parsed = await response.Content.ReadFromJsonAsync<SteamGridDbResponse<SteamGridDbGame>>(JsonOptions);
        return parsed?.Data ?? Array.Empty<SteamGridDbGame>();
    }

    public Task<SteamGridDbImage[]> GetGridsAsync(int gameId, string dimensions, string filterParams) =>
        GetImagesAsync($"grids/game/{gameId}?dimensions={dimensions}&{filterParams}");

    public Task<SteamGridDbImage[]> GetHeroesAsync(int gameId, string filterParams) =>
        GetImagesAsync($"heroes/game/{gameId}?{filterParams}");

    public Task<SteamGridDbImage[]> GetLogosAsync(int gameId, string filterParams) =>
        GetImagesAsync($"logos/game/{gameId}?{filterParams}");

    private async Task<SteamGridDbImage[]> GetImagesAsync(string path)
    {
        var response = await _httpClient.GetAsync(path);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<SteamGridDbImage>();
        }

        var parsed = await response.Content.ReadFromJsonAsync<SteamGridDbResponse<SteamGridDbImage>>(JsonOptions);
        return parsed?.Success == true ? parsed.Data ?? Array.Empty<SteamGridDbImage>() : Array.Empty<SteamGridDbImage>();
    }
}
