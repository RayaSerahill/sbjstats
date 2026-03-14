using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;

namespace sbjStats.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string endpoint;
    private string apiKey;
    private bool enableUpload;

    public ConfigWindow(Plugin plugin) : base("SBJ Stats Config###sbjStatsConfig")
    {
        this.plugin = plugin;

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 180),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        endpoint = plugin.Configuration.Endpoint;
        apiKey = plugin.Configuration.ApiKey;
        enableUpload = plugin.Configuration.EnableUpload;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.InputText("Endpoint", ref endpoint, 512);
        ImGui.InputText("API Key", ref apiKey, 512);
        ImGui.Checkbox("Enable Upload", ref enableUpload);

        if (ImGui.Button("Save"))
        {
            plugin.Configuration.Endpoint = endpoint.Trim();
            plugin.Configuration.ApiKey = apiKey.Trim();
            plugin.Configuration.EnableUpload = enableUpload;
            plugin.Configuration.Save();
        }
        
        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.TextWrapped("If you have existing stats that you want to upload, you can do so by clicking the button below. This will upload all stats that have been recorded so far while avoiding duplicates that are already uploaded.");

        if (ImGui.Button("Upload existing stats"))
        {
            if (String.IsNullOrEmpty(plugin.Configuration.Endpoint) || String.IsNullOrEmpty(plugin.Configuration.ApiKey))
            {
                
                plugin.ShowToast("Please enter a valid endpoint and API key.", NotificationType.Error);
                return;
            }
            plugin.UploadExistingStatsAsync();
        }
    }
}