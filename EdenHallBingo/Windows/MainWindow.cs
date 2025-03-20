using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace EdenHallBingo.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin;
    public MainWindow(Plugin plugin)
        : base("Eden Hall Bingo##idtag", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            // MinimumSize = new Vector2(375, 330),
            // MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Plugin = plugin;
        Configuration = plugin.Configuration;
    }

    public void Dispose() { }
    private string inputCode = "";
    private List<TabData> tabs = new List<TabData> { new TabData("Create New") };
    private Configuration Configuration;
    private bool usedCode = false;
    public override void Draw()
    {
        if (ImGui.BeginTabBar("MainTabBar"))
        {
            // "Create New" tab - First tab for input
            if (ImGui.BeginTabItem("Create New"))
            {
                if (Configuration.AdminCode == "Cheesecakes!") 
                {
                    if (ImGui.Button("Open Admin Window"))
                    {
                        Plugin.ToggleAdminUI();
                    }
                }
                ImGui.InputText("##CodeInput", ref inputCode, 100, ImGuiInputTextFlags.EnterReturnsTrue);

                if (ImGui.IsItemDeactivatedAfterEdit()) // Enter key handling
                {
                    AddNewTab();
                }

                ImGui.SameLine();
                if (ImGui.Button("Add Tab"))
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
                if (tabs.Count > 1 && ImGui.Button("Delete All Tabs"))
                {
                    tabs.RemoveRange(1, tabs.Count - 1); // Keep only "Create New"
                    usedCode = false;
                }

                ImGui.EndTabItem();
            }

            // Draw dynamically created tabs
            for (int i = 1; i < tabs.Count; i++)
            {
                if (ImGui.BeginTabItem($"Board {i}"))
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
