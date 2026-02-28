using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.Text.Json;
using MTPhotosWallpaper.Models;

namespace MTPhotosWallpaper.Services;

public class MTPhotosApiService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static void Log(string message)
    {
        try
        {
            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MTPhotosWallpaper", "debug.log");
            var dir = Path.GetDirectoryName(logPath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.AppendAllText(logPath, $"[{DateTime.Now}] {message}\n");
        }
        catch { }
    }

    public MTPhotosApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public async Task<(bool Success, string? AccessToken, string? AuthCode, string? Error)> AuthenticateAsync(
        string serverUrl, string username, string password)
    {
        try
        {
            var authUrl = NormalizeServerUrl(serverUrl) + "/auth/login";
            var request = new
            {
                username,
                password
            };

            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(authUrl, content);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (false, null, null, "Invalid username or password");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return (false, null, null, "Rate limited (429) - Too many requests");
            }

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, null, $"Server error (status {(int)response.StatusCode})");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseJson, JsonOptions);

            if (authResponse?.AccessToken == null || authResponse.AuthCode == null)
            {
                return (false, null, null, "Invalid login response");
            }

            return (true, authResponse.AccessToken, authResponse.AuthCode, null);
        }
        catch (HttpRequestException)
        {
            return (false, null, null, "Could not connect to server");
        }
        catch (Exception ex)
        {
            return (false, null, null, $"Connection error: {ex.Message}");
        }
    }

    public async Task<List<Album>> GetAlbumsAsync(string serverUrl, string accessToken)
    {
        try
        {
            var url = NormalizeServerUrl(serverUrl) + "/api-album";
            Log($"GetAlbumsAsync: URL = {url}");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            Log($"GetAlbumsAsync: StatusCode = {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            Log($"GetAlbumsAsync: Response = {json.Substring(0, Math.Min(500, json.Length))}");
            var albums = JsonSerializer.Deserialize<List<Album>>(json, JsonOptions);

            return albums ?? new List<Album>();
        }
        catch (Exception ex)
        {
            Log($"GetAlbumsAsync Error: {ex.Message}");
            return new List<Album>();
        }
    }

    public async Task<List<Photo>> GetPhotosAsync(
        string serverUrl, string albumId, string accessToken, double minAspectRatio)
    {
        try
        {
            var url = NormalizeServerUrl(serverUrl) + $"/api-album/filesV2/{albumId}?listVer=v2";
            Log($"GetPhotosAsync: URL = {url}");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);
            Log($"GetPhotosAsync: StatusCode = {response.StatusCode}");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            Log($"GetPhotosAsync: Response = {json.Substring(0, Math.Min(500, json.Length))}");

            var photoResponse = JsonSerializer.Deserialize<PhotoListResponse>(json, JsonOptions);
            Log($"GetPhotosAsync: PhotoResponse = {photoResponse?.Result?.Count} day groups");

            var photos = new List<Photo>();
            if (photoResponse?.Result != null)
            {
                foreach (var dayGroup in photoResponse.Result)
                {
                    if (dayGroup?.List != null)
                    {
                        photos.AddRange(dayGroup.List.Where(p =>
                            p != null &&
                            (p.FileType == "JPEG" || p.FileType == "PNG") &&
                            p.Width > 0 &&
                            p.Height > 0 &&
                            p.GetAspectRatio() >= minAspectRatio));
                    }
                }
            }

            Log($"GetPhotosAsync: Filtered {photos.Count} photos");
            return photos;
        }
        catch (Exception ex)
        {
            Log($"GetPhotosAsync Error: {ex.Message}");
            return new List<Photo>();
        }
    }

    public async Task<string?> DownloadPhotoAsync(
        string serverUrl, Photo photo, string authCode, string cachePath)
    {
        try
        {
            var encodedAuthCode = Uri.EscapeDataString(authCode);
            var url = NormalizeServerUrl(serverUrl) +
                      $"/gateway/fileDownload/{photo.Id}/{photo.MD5}?auth_code={encodedAuthCode}";

            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var directory = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var fileStream = File.Create(cachePath);
            var stream = await response.Content.ReadAsStreamAsync();
            await stream.CopyToAsync(fileStream);

            return cachePath;
        }
        catch
        {
            return null;
        }
    }

    private string NormalizeServerUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        return url.EndsWith('/') ? url.Substring(0, url.Length - 1) : url;
    }

    private class AuthResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("auth_code")]
        public string? AuthCode { get; set; }
    }

    private class PhotoListResponse
    {
        [JsonPropertyName("result")]
        public List<DayGroup>? Result { get; set; }
    }

    private class DayGroup
    {
        [JsonPropertyName("list")]
        public List<Photo>? List { get; set; }
    }
}
