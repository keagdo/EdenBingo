using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace EdenHallBingo.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base("Settings###ConfigWindow")
    {
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var code = Configuration.AdminCode;
        if (ImGui.InputText("Enter Admin Code", ref code, 128))
        {
            Configuration.AdminCode = code;
            Configuration.Save();
        }
        if (code == "Cheesecakes!")
        {
            ImGui.TextUnformatted("Admin Mode Activated!");
        }
    }
}
