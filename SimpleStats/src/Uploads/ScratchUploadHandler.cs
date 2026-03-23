using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ECommons.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace sbjStats;

public sealed class ScratchUploadHandler : GameUploadHandlerBase
{
    public ScratchUploadHandler(Plugin plugin) : base(plugin)
    {
    }

    public void HandleGameEnded(string json, long archivedAtUnixSeconds)
    {
        if (!IsLiveUploadEnabled("SimpleScratch") || !HasUploadConfiguration("SimpleScratch", notifyUser: false))
            return;

        _ = UploadLiveRoundAsync(json, archivedAtUnixSeconds);
    }

    public async Task UploadExistingAsync(SimpleScratchIpc ipc)
    {
        if (!HasUploadConfiguration("SimpleScratch", notifyUser: true))
            return;

        var archiveJson = await ipc.GetArchiveAsync();
        if (string.IsNullOrWhiteSpace(archiveJson))
        {
            Plugin.ShowToast("SimpleScratch: archive payload was empty.", Dalamud.Interface.ImGuiNotification.NotificationType.Info);
            return;
        }

        await UploadArchiveSnapshotAsync(archiveJson);
        Plugin.ShowToast("SimpleScratch archive uploaded.", Dalamud.Interface.ImGuiNotification.NotificationType.Success);
    }

    public async Task UploadLiveRoundAsync(string json, long archivedAtUnixSeconds)
    {
        try
        {
            var transformedJson = TransformLivePayloadJson(json, archivedAtUnixSeconds);
            if (transformedJson is null)
            {
                PluginLog.Warning("SimpleScratch live upload skipped: payload could not be transformed.");
                return;
            }

            var request = BuildLiveUploadRequest(transformedJson);
            await SendScratchUploadAsync(request);
        }
        catch (Exception ex)
        {
            PluginLog.Error($"SimpleScratch live upload failed: {ex}");
        }
    }

    public ScratchUploadRequest BuildLiveUploadRequest(string transformedJson)
    {
        var payload = TryParseObject(transformedJson);

        return new ScratchUploadRequest
        {
            UploadType = "live",
            RawJson = transformedJson,
            PlayerName = payload?["player_name"]?.ToString(),
            GameId = payload?["player_id"]?.ToString(),
            OccurredAtUnixSeconds = payload?["archived_at"]?.Value<long?>(),
        };
    }

    public ScratchUploadRequest BuildArchiveUploadRequest(string transformedJson)
    {
        return new ScratchUploadRequest
        {
            UploadType = "archive",
            RawJson = transformedJson,
        };
    }

    public async Task SendScratchUploadAsync(ScratchUploadRequest request)
    {
        PluginLog.Information(
            $"SimpleScratch upload sending '{request.UploadType}' payload for player '{request.PlayerName ?? "<unknown>"}' with game '{request.GameId ?? "<unknown>"}'.");
        Log.Information($"Raw JSON payload: {request.RawJson}");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Plugin.Configuration.ApiKey);

        using var content = new StringContent(request.RawJson, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(Plugin.EndpointScratch.Trim(), content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var msg = $"Upload failed: {(int)response.StatusCode} {response.ReasonPhrase} | {responseText}";
            Plugin.Instance?.ShowToast(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Error);
        }
        else
        {
            var msg = "Stats uploaded successfully \\o/";
            Plugin.Instance?.ShowToast(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Success);
        }
    }

    public async Task UploadArchiveSnapshotAsync(string archiveJson)
    {
        var transformedJson = TransformArchivePayloadJson(archiveJson);
        if (transformedJson is null)
        {
            PluginLog.Warning("SimpleScratch archive upload skipped: payload could not be transformed.");
            return;
        }

        var request = BuildArchiveUploadRequest(transformedJson);
        await SendScratchUploadAsync(request);
        PluginLog.Information("SimpleScratch archive snapshot transformed and uploaded.");
    }

    private static string? TransformLivePayloadJson(string json, long archivedAtUnixSeconds)
    {
        var payload = TryParseObject(json);
        if (payload is null)
            return null;

        payload["archived_at"] = archivedAtUnixSeconds;
        return payload.ToString(Formatting.None);
    }

    private static string? TransformArchivePayloadJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var payload = JArray.Parse(json);
            foreach (var item in payload.OfType<JObject>())
            {
                var archivedAtString = item["archived_at"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(archivedAtString))
                    continue;

                if (!TryParseArchiveTimestamp(archivedAtString, out var archivedAtUnixSeconds))
                {
                    PluginLog.Warning($"SimpleScratch archive item had an invalid archived_at value: {archivedAtString}");
                    continue;
                }

                item["archived_at"] = archivedAtUnixSeconds;
            }

            return payload.ToString(Formatting.None);
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to transform SimpleScratch archive payload: {ex}");
            return null;
        }
    }

    private static JObject? TryParseObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JObject.Parse(json);
        }
        catch (Exception ex)
        {
            PluginLog.Error($"Failed to parse SimpleScratch JSON object: {ex}");
            return null;
        }
    }

    private static bool TryParseArchiveTimestamp(string value, out long unixSeconds)
    {
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            unixSeconds = parsed.ToUnixTimeSeconds();
            return true;
        }

        unixSeconds = 0;
        return false;
    }
}
