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

        bool chats = Configuration.SendChats;
        if (ImGui.Button($"Send Chats on Draw Numbers: {chats}"))
        {
            chats = !chats;
            Configuration.SendChats = chats;
            Configuration.Save();
        }

        var channel = Configuration.Channel;
        if (ImGui.InputText("Enter channel to send tells in!", ref channel, 128))
        {
            Configuration.Channel = channel;
            Configuration.Save();
        }
        if (GetDeterministicHashCode(code) == -1226680257)
        {
            ImGui.TextUnformatted("Admin Mode Activated!");
        }
    }
    private int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < str.Length; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1)
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}
