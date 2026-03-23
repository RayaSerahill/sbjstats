using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;

namespace sbjStats.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
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

        apiKey = plugin.Configuration.ApiKey;
        enableUpload = plugin.Configuration.EnableUpload;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        if (ImGui.InputText("API Key", ref apiKey, 512))
            UpdateConfig(() => plugin.Configuration.ApiKey = apiKey.Trim());
        if (ImGui.Checkbox("Enable Live Upload", ref enableUpload))
            UpdateConfig(() => plugin.Configuration.EnableUpload = enableUpload);

        if (ImGui.BeginTabBar("SimpleGambaTabs"))
        {
            if (ImGui.BeginTabItem("SimpleBlackjack"))
            {
                ImGui.TextWrapped("If you have existing stats that you want to upload, you can do so by clicking the button below. This will upload all stats that have been recorded so far while avoiding duplicates that are already uploaded.");
                if (ImGui.Button("Upload existing stats"))
                {
                    if (String.IsNullOrEmpty(plugin.Configuration.ApiKey))
                    {
                        plugin.ShowToast("Please enter a valid API key.", NotificationType.Error);
                        return;
                    }
                    plugin.UploadExistingStatsSbjAsync();
                }
            }

            if (ImGui.BeginTabItem("SimpleScratch"))
            {
                
            }
        }
        

    }
    
    private void UpdateConfig(Action applyChanges)
    {
        applyChanges();
        plugin.Configuration.Save();
    }
}