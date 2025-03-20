using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace EdenHallBingo;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string AdminCode { get; set;} = "BadCode";
    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
