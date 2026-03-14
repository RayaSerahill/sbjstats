using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace sbjStats;

public static class CsvUploader
{
    public static void SendStatAsCsv(
        StatsRecording stat,
        string endpoint,
        string apiKey)
    {
        var csv = BuildCsv(stat);
        // Print csv to console for debugging
        Log.Information("Generated CSV:\n{Csv}", csv);
        
        UploadCsvAsync(csv, endpoint, apiKey);
    }
    
    public static async Task SendMassStatsAsCsvAsync(
        IEnumerable<StatsRecording> stats,
        string endpoint,
        string apiKey)
    {
        var csv = BuildCsv(stats);
        await UploadCsvAsync(csv, endpoint, apiKey);
    }

    private static async Task UploadCsvAsync(
        string csv,
        string endpoint,
        string apiKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            Plugin.Instance?.ShowToast("Upload endpoint is missing.",
                Dalamud.Interface.ImGuiNotification.NotificationType.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Plugin.Instance?.ShowToast("API key is missing.",
                Dalamud.Interface.ImGuiNotification.NotificationType.Error);
            return;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var json = JsonConvert.SerializeObject(new
        {
            csvText = csv
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint.Trim(), content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var msg = $"Upload failed: {(int)response.StatusCode} {response.ReasonPhrase} | {responseText}";
            Plugin.Instance?.ShowToast(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Error);
        }
        else
        {
            var msg = $"Stats uploaded successfully \\o/";
            Plugin.Instance?.ShowToast(msg, Dalamud.Interface.ImGuiNotification.NotificationType.Success);
        }
    }

    public static string BuildCsv(StatsRecording stat)
    {
        return BuildCsv(new[] { stat });
    }

    public static string BuildCsv(IEnumerable<StatsRecording> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("sep=;");
        sb.AppendLine("Date and time;Players;Collected;Paid out;Profit;Details");

        foreach (var stat in stats.OrderBy(s => s.Time))
        {
            sb.AppendLine(BuildCsvRow(stat));
        }

        return sb.ToString();
    }

    private static string BuildCsvRow(StatsRecording stat)
    {
        var dateTime = DateTimeOffset
            .FromUnixTimeMilliseconds(stat.Time)
            .ToString("dd/MM/yyyy HH.mm.ss zzz", CultureInfo.InvariantCulture);

        var players = string.Join(", ", stat.Players ?? []);
        var collected = FormatNumber(stat.BetsCollected);
        var paidOut = FormatNumber(stat.Payouts);
        var profit = FormatNumber(stat.BetsCollected - stat.Payouts);

        var handsJson = JsonConvert.SerializeObject(stat.Hands ?? []);
        var details = Convert.ToBase64String(Encoding.UTF8.GetBytes(handsJson));

        return string.Join(";",
            EscapeCsv(dateTime),
            EscapeCsv(players),
            EscapeCsv(collected),
            EscapeCsv(paidOut),
            EscapeCsv(profit),
            EscapeCsv(details)
        );
    }

    private static string FormatNumber(int value)
    {
        return value.ToString("N0", CultureInfo.GetCultureInfo("fi-FI"));
    }

    private static string EscapeCsv(string value)
    {
        value ??= string.Empty;

        var mustQuote =
            value.Contains(';') ||
            value.Contains('"') ||
            value.Contains('\n') ||
            value.Contains('\r');

        if (!mustQuote)
            return value;

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static void UploadCsv(
        string csv,
        string endpoint,
        string apiKey)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new Exception("Upload endpoint is missing.");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new Exception("API key is missing.");

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);

        var json = JsonConvert.SerializeObject(new
        {
            csvText = csv
        });

        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = client.PostAsync(endpoint.Trim(), content).GetAwaiter().GetResult();
        var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Upload failed: {(int)response.StatusCode} {response.ReasonPhrase} | {responseText}");
    }
}