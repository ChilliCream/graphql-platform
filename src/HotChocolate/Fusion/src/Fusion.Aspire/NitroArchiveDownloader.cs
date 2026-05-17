using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

internal static class NitroArchiveDownloader
{
    private static readonly TimeSpan s_cacheTtl = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<string?> DownloadOrGetCachedAsync(
        NitroConfigurationAnnotation config,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var cachePath = GetCachePath(config);

        if (!config.AlwaysDownload && IsCacheFresh(cachePath))
        {
            logger.LogInformation("📦 Using cached Nitro archive for API '{ApiId}' stage '{Stage}'.",
                config.ApiId, config.Stage);
            return cachePath;
        }

        var session = ReadSession(logger);
        if (session?.Tokens is null)
        {
            if (File.Exists(cachePath))
            {
                logger.LogWarning(
                    "Nitro session not found. Using cached archive. "
                    + "Run 'nitro login' to authenticate.");
                return cachePath;
            }

            logger.LogError(
                "Nitro session not found and no cached archive available. "
                + "Run 'nitro login' to authenticate.");
            return null;
        }

        if (session.Tokens.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            if (File.Exists(cachePath))
            {
                logger.LogWarning(
                    "Nitro session token expired. Using cached archive. "
                    + "Run 'nitro login' to re-authenticate.");
                return cachePath;
            }

            logger.LogError(
                "Nitro session token expired and no cached archive available. "
                + "Run 'nitro login' to re-authenticate.");
            return null;
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Add(
                "Authorization", $"Bearer {session.Tokens.AccessToken}");

            var url = $"{session.ApiUrl.TrimEnd('/')}/api/v1/apis/"
                + $"{Uri.EscapeDataString(config.ApiId)}"
                + "/fusion/configurations/latest/download"
                + $"?stage={Uri.EscapeDataString(config.Stage)}"
                + $"&format={Uri.EscapeDataString("far")}"
                + $"&fusionVersion={Uri.EscapeDataString(WellKnownVersions.LatestGatewayFormatVersion.ToString())}";

            using var response = await httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                if (File.Exists(cachePath))
                {
                    logger.LogWarning(
                        "Nitro authentication failed (HTTP {Status}). Using cached archive. "
                        + "Run 'nitro login' to re-authenticate.",
                        (int)response.StatusCode);
                    return cachePath;
                }

                logger.LogError(
                    "Nitro authentication failed (HTTP {Status}) and no cached archive available. "
                    + "Run 'nitro login' to authenticate.",
                    (int)response.StatusCode);
                return null;
            }

            if (response.StatusCode is HttpStatusCode.NotFound)
            {
                logger.LogError(
                    "Nitro archive not found for API '{ApiId}' stage '{Stage}'.",
                    config.ApiId, config.Stage);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (File.Exists(cachePath))
                {
                    logger.LogWarning(
                        "Nitro download failed (HTTP {Status}). Using cached archive.",
                        (int)response.StatusCode);
                    return cachePath;
                }

                logger.LogError(
                    "Nitro download failed (HTTP {Status}) and no cached archive available.",
                    (int)response.StatusCode);
                return null;
            }

            var cacheDir = IOPath.GetDirectoryName(cachePath)!;
            Directory.CreateDirectory(cacheDir);

            var tempPath = cachePath + $".tmp.{IOPath.GetRandomFileName()}";

            await using (var fs = File.Create(tempPath))
            {
                await response.Content.CopyToAsync(fs, cancellationToken);
            }

            File.Move(tempPath, cachePath, overwrite: true);

            logger.LogInformation(
                "✅ Downloaded Nitro archive for API '{ApiId}' stage '{Stage}'.",
                config.ApiId, config.Stage);
            return cachePath;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            if (File.Exists(cachePath))
            {
                logger.LogWarning(
                    ex,
                    "Nitro download failed. Using cached archive.");
                return cachePath;
            }

            logger.LogError(
                ex,
                "Nitro download failed and no cached archive available.");
            return null;
        }
    }

    private static string GetCachePath(NitroConfigurationAnnotation config)
    {
        var key = $"{config.ApiId}_{config.Stage}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16];
        return IOPath.Combine(IOPath.GetTempPath(), "nitro-aspire", $"{hash}.far");
    }

    private static bool IsCacheFresh(string path)
        => File.Exists(path)
           && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < s_cacheTtl;

    private static Session? ReadSession(ILogger logger)
    {
        try
        {
            var sessionPath = IOPath.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "nitro",
                "session.json");

            if (!File.Exists(sessionPath))
            {
                return null;
            }

            var json = File.ReadAllText(sessionPath);
            return JsonSerializer.Deserialize<Session>(json, s_jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read Nitro session file.");
            return null;
        }
    }
}
