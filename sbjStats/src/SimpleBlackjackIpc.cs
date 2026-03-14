using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using ECommons.EzIpcManager;
using ECommons.Logging;
using Newtonsoft.Json;
using Serilog;

namespace sbjStats;

public class SimpleBlackjackIpc
{
    private readonly Action<StatsRecording> _processRound;
    public SimpleBlackjackIpc(
        Func<string, List<StatsRecording>> getStats,
        Func<Dictionary<string, string>> getArchives,
        Action<StatsRecording> processRound
        )
    {
        PluginLog.Information("SimpleBlackjackIpc constructor called.");
        GetStats = getStats;
        GetArchives = getArchives;
        _processRound = processRound;
        EzIPC.Init(this, "SimpleBlackjack");
        PluginLog.Information("EzIPC.Init called for SimpleBlackjack.");
    }

    [EzIPC]
    public Func<string, List<StatsRecording>> GetStats;

    [EzIPC]
    public Func<Dictionary<string, string>> GetArchives;

    [EzIPCEvent]
    public void OnGameFinished()
    {
        Log.Information("OnGameFinished called in SimpleBlackjackIpc.");
    }

    [EzIPCEvent]
    public void OnGameFinishedEx(StatsRecording stats)
    {
        Log.Information("OnGameFinished called in SimpleBlackjackIpc EX.");
        Log.Information(stats.Time.ToString());
        
        _processRound(stats);
        
    }
    
    private string GetApiKey()
    {
        var config = Plugin.PluginInterface.GetPluginConfig() as Configuration;
        return config?.ApiKey?.Trim() ?? string.Empty;
    }
}