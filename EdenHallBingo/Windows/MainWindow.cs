using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Dalamud.IoC;
using System.IO;
using EdenHallBingo.Helpers;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using System.Text.RegularExpressions;


namespace EdenHallBingo.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    private string LogoPath;
    private static string versionID = "0.0.0.13";
    private List<TabData> tabs;
    ChatManager chatManager = new ChatManager();
    public MainWindow(Plugin plugin, string logoPath)
        : base($"Eden Hall Bingo v{versionID}##idtag", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            // MinimumSize = new Vector2(375, 330),
            // MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Plugin = plugin;
        Configuration = plugin.Configuration;
        LogoPath = logoPath;
        tabs = Configuration.tabs;
        Plugin.Chat.ChatMessage += OnChatMessage;
    }

    public List<string> codes = new List<string>();
    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        string messageText = message.TextValue;
        if (type == XivChatType.TellIncoming)
        {
            // Strict regex pattern for the exact message format
            Regex regex1 = new Regex(
                @"^Thanks for playing Bingo! I am sending you \d+ codes to start your game! Your codes are: ([A-Za-z0-9/+]+(?:, [A-Za-z0-9/+]+)*)$",
                RegexOptions.IgnoreCase);
            Regex regex2 = new Regex(
                @"^Thanks for playing Bingo! Here are your \d+ extra codes! Your codes are: ([A-Za-z0-9/+]+(?:, [A-Za-z0-9/+]+)*)$",
                RegexOptions.IgnoreCase);

            Match match1 = regex1.Match(messageText);
            Match match2 = regex2.Match(messageText);

            if (match1.Success)
            {
                // Extract the codes and split them into a list
                codes = match1.Groups[1].Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                tabs.Clear();
                isHandled = true;
            }
            if (match2.Success)
            {
                codes = match2.Groups[1].Value.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
                isHandled = true;
            }
        }
    }

    public void Dispose() { }
    private string inputCode = "";
    private Configuration Configuration;
    private bool usedCode = false;
    public override void Draw()
    {
        if (codes.Count>0)
        {
            foreach (string code in codes)
            {
                AutoAddNewTab(code);
            }
            codes.Clear();
            chatManager.SendMessage("/echo Thanks for playing Bingo! Your new boards have been loaded! Please use /bingo to see the boards! <se.1>");
        }
        
        if (ImGui.BeginTabBar("MainTabBar"))
        {
            // "Create New" tab - First tab for input
            if (ImGui.BeginTabItem("Home"))
            {
                if (Configuration.AdminCode == "Cheesecakes!") 
                {
                    if (ImGui.Button("Open Admin Window"))
                    {
                        Plugin.ToggleAdminUI();
                    }
                }
                ImGui.InputTextWithHint("##CodeInput", "Input code received from game master:", ref inputCode, 100, ImGuiInputTextFlags.EnterReturnsTrue);

                if (ImGui.IsItemDeactivatedAfterEdit()) // Enter key handling
                {
                    AddNewTab();
                }

                ImGui.SameLine();
                if (ImGui.Button("Open Board"))
                {
                    AddNewTab();
                }

                if (usedCode)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.0f, 0.0f, 1.0f)); // Set text color to red
                    ImGui.Text("Code already used!");
                    ImGui.PopStyleColor(); // Restore default text color
                }

                ImGui.Spacing();

                // Delete All Tabs Button
                if (tabs.Count > 0) 
                {
                    if (ImGui.Button("Close All Boards")) 
                    {
                        tabs.Clear(); // Keep only "Create New"
                        usedCode = false;

                        Configuration.tabs = tabs;
                        Configuration.Save();
                    }
                }
                else 
                {
                    ImGui.BeginDisabled();
                    ImGui.Button("Close All Boards");
                    ImGui.EndDisabled();
                }
                
                // // var test = PluginInterface.AssemblyLocation.Directory?.FullName!;
                // // logoPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "logo.png");
                // // Plugin.Log.Debug(logoPath);
                // var logoImage = Plugin.TextureProvider.GetFromFile(LogoPath).GetWrapOrDefault();
                // if (logoImage != null)
                // {
                //     ImGui.Image(logoImage.ImGuiHandle, new Vector2(logoImage.Width, logoImage.Height));
                // }
                // else
                // {
                //     ImGui.TextUnformatted("Image not found.");
                // }

                ImGui.EndTabItem();
            }

            // Draw dynamically created tabs
            for (int i = 0; i < tabs.Count; i++)
            {
                if (ImGui.BeginTabItem($"Board {i+1}"))
                {
                    DrawBingoBoard(tabs[i]);
                    ImGui.EndTabItem();
                }
            }

            ImGui.EndTabBar();
        }
    }

    private void AddNewTab()
    {
        if (!string.IsNullOrWhiteSpace(inputCode))
        {
            // Check if the code already exists
            if (tabs.Any(tab => tab.Title == inputCode))
            {
                inputCode = ""; // Clear input
                usedCode = true;
            }
            else
            {
                var boardTab = new TabData(inputCode);
                boardTab.AddBoard(inputCode);
                tabs.Add(boardTab); // Create new tab
                inputCode = ""; // Clear input after adding
                usedCode = false;
                Configuration.tabs = tabs;
                Configuration.Save();
            }
        }
    }
    private void AutoAddNewTab(string inputCode)
    {
        if (!string.IsNullOrWhiteSpace(inputCode))
        {
            // Check if the code already exists
            if (tabs.Any(tab => tab.Title == inputCode))
            {
                usedCode = true;
            }
            else
            {
                var boardTab = new TabData(inputCode);
                boardTab.AddBoard(inputCode);
                tabs.Add(boardTab); // Create new tab
                inputCode = ""; // Clear input after adding
                usedCode = false;
                Configuration.tabs = tabs;
                Configuration.Save();
            }
        }
    }

    private void DrawBingoBoard(TabData tab)
    {
        // Display "BINGO" header
        string[] bingoLetters = { "B", "I", "N", "G", "O" };

        // Centering the BINGO letters over the columns
        for (int i = 0; i < 5; i++)
        {
            ImGui.SetCursorPosX(30 + i * 57); // Adjust X position for each letter
            ImGui.Text(bingoLetters[i]);
            ImGui.SameLine();
        }

        ImGui.Spacing(); // Space between header and board

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                int index = row * 5 + col;
                var board = tab.Boards[0];
                bool isMarked = board.MarkedNumbers.Contains(index);

                // Get the number for this button
                string buttonText = (board.Board[index] == 0) ? "FREE" : $"{board.Board[index]}";

                // Change button color if marked
                if (isMarked)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.8f, 0.2f, 1.0f)); // Green
                }

                // Create button for each bingo number
                if (ImGui.Button(buttonText, new Vector2(50, 50)))
                {
                    if (isMarked)
                        board.MarkedNumbers.Remove(index);
                    else
                        board.MarkedNumbers.Add(index);
                }

                // Restore default button color if it was changed
                if (isMarked)
                {
                    ImGui.PopStyleColor();
                }

                if (col < 4) ImGui.SameLine();
            }
        }
    }

}
