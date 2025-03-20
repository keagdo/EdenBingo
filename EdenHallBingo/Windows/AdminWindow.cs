using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace EdenHallBingo.Windows;

public class AdminWindow : Window, IDisposable
{
    public void Dispose() { }
    private string playerName = "";
    private int boardCount = 1;
    private List<string> generatedCodes = new();
    private List<TabData> tabs = new();

    private List<int> drawnNumbers = new List<int> { 0 }; // List to store drawn numbers
    private string manualInput = ""; // Input field for manual number entry
    private Random rng = new Random();

    public AdminWindow(Plugin plugin): base("Eden Hall Bingo Admin Tab##idtag", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            // MinimumSize = new Vector2(375, 330),
            // MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("MainTabBar"))
        {
            if (ImGui.BeginTabItem("Add Player"))
            {
                ImGui.Text("Admin Panel");

                // Input for player's name
                ImGui.InputText("Player Name", ref playerName, 100);

                // Input for number of boards (minimum 1)
                ImGui.InputInt("Boards to Buy", ref boardCount);
                if (boardCount < 1) boardCount = 1;

                // Button to generate boards
                if (ImGui.Button("Generate Boards"))
                {
                    GeneratePlayerBoards();
                }

                if (ImGui.Button("Delete Player")) 
                {
                    DeletePlayer();
                }

                if (ImGui.GetIO().KeyShift) 
                {
                    if (ImGui.Button("Delete All Players (Hold Shift)")) 
                    {
                        tabs.Clear();
                    }
                }
                else 
                {
                    ImGui.BeginDisabled();
                    ImGui.Button("Delete All Players (Hold Shift)");
                    ImGui.EndDisabled();
                }

                // Display generated codes
                if (generatedCodes.Count > 0)
                {
                    ImGui.Text("Generated Codes:");
                    
                    foreach (var code in generatedCodes)
                    {
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), code); // Green text for codes
                    }

                    // Create a formatted string of codes
                    string formattedCodes = generatedCodes.Count == 1
                        ? "Your code is: " + generatedCodes[0]
                        : "Your codes are: " + string.Join(", ", generatedCodes);

                    // Copy to clipboard button
                    if (ImGui.Button("Copy Codes to Clipboard"))
                    {
                        ImGui.SetClipboardText(formattedCodes);
                    }
                }

                ImGui.EndTabItem();
            }
            // bool anyPlayerHasWinner = tabs.Any(tab => tab.Boards.Any(board => board.isWinner)); // Check if any player has a winning board

            // if (anyPlayerHasWinner)
            // {
            //     ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.8f, 0.2f, 0.2f, 1.0f)); // Red highlight
            //     ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(1.0f, 0.4f, 0.4f, 1.0f)); // Lighter red on hover
            //     ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(1.0f, 0.6f, 0.6f, 1.0f)); // Bright red when active
            // }

            if (ImGui.BeginTabItem("Players"))
            {
                // if (anyPlayerHasWinner)
                // {
                //     ImGui.PopStyleColor(3); // Restore colors after "Players" tab is drawn
                // }

                if (ImGui.BeginTabBar("PlayerTabBar"))
                {
                    for (int i = 0; i < tabs.Count; i++)
                    {
                        bool hasWinner = tabs[i].Boards.Any(board => board.isWinner); // Check if this player has a winning board

                        // if (hasWinner)
                        // {
                        //     ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.8f, 0.2f, 0.2f, 1.0f)); // Red highlight
                        //     ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(1.0f, 0.4f, 0.4f, 1.0f)); // Lighter red on hover
                        //     ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(1.0f, 0.6f, 0.6f, 1.0f)); // Bright red when active
                        // }

                        if (ImGui.BeginTabItem(tabs[i].Title))
                        {
                            DrawBingoBoard(tabs[i]);
                            ImGui.EndTabItem();
                        }

                        // if (hasWinner)
                        // {
                        //     ImGui.PopStyleColor(3); // Restore colors after the player's tab is drawn
                        // }
                    }
                    ImGui.EndTabBar();
                }
                ImGui.EndTabItem();
            }

            // if (anyPlayerHasWinner)
            // {
            //     ImGui.PopStyleColor(3); // Restore colors after Players tab is finished
            // }

            if (ImGui.BeginTabItem("Draw Numbers"))
            {
                ImGui.BeginChild("DrawNumbersScroll", new Vector2(0, 300), true, ImGuiWindowFlags.HorizontalScrollbar);
                ImGui.Text("Bingo Number Draw");

                // Button to draw a random number
                if (ImGui.Button("Draw Random Number"))
                {
                    DrawRandomNumber();
                }

                // Manual input for admin
                ImGui.SetNextItemWidth(50); // Restrict input field width
                ImGui.InputText("Manual Entry", ref manualInput, 3);
                ImGui.SameLine();
                if (ImGui.Button("Add Number"))
                {
                    AddManualNumber();
                }

                if (ImGui.Button("Delete Numbers"))
                {
                    drawnNumbers.Clear();
                    drawnNumbers.Add(0);
                }

                if (drawnNumbers.Count > 1) // Ensure at least one valid number is drawn
                {
                    int lastNumber = drawnNumbers.Last(); // Get the most recent drawn number
                    string columnLetter = lastNumber switch
                    {
                        >= 1 and <= 15 => "B",
                        >= 16 and <= 30 => "I",
                        >= 31 and <= 45 => "N",
                        >= 46 and <= 60 => "G",
                        >= 61 and <= 75 => "O",
                        _ => "?"
                    };

                    ImGui.Text("Most recent drawing: ");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{columnLetter} {lastNumber}"); // Red color

                    string shoutMessage = $"/shout Bingo Drawing #{drawnNumbers.Count-1} is {columnLetter}{lastNumber}";
                    // Button to copy to clipboard
                    if (ImGui.Button($"Copy Shout Message: {shoutMessage}"))
                    {
                        ImGui.SetClipboardText(shoutMessage);
                    }
                }

                // Display drawn numbers
                ImGui.Text("Drawn Numbers:");
                // Define B-I-N-G-O groups
                List<int> columnB = new List<int>();
                List<int> columnI = new List<int>();
                List<int> columnN = new List<int>();
                List<int> columnG = new List<int>();
                List<int> columnO = new List<int>();

                // Sort drawn numbers into their respective columns
                foreach (var number in drawnNumbers)
                {
                    if (number >= 1 && number <= 15) columnB.Add(number);
                    else if (number >= 16 && number <= 30) columnI.Add(number);
                    else if (number >= 31 && number <= 45) columnN.Add(number);
                    else if (number >= 46 && number <= 60) columnG.Add(number);
                    else if (number >= 61 && number <= 75) columnO.Add(number);
                }
                // Sort each column
                columnB.Sort();
                columnI.Sort();
                columnN.Sort();
                columnG.Sort();
                columnO.Sort();

                // Display B-I-N-G-O headers
                string[] headers = { "B", "I", "N", "G", "O" };
                float[] columnOffsets = { 30, 90, 150, 210, 270 }; // Adjusted column positions

                for (int i = 0; i < 5; i++)
                {
                    ImGui.SetCursorPosX(columnOffsets[i]); 
                    ImGui.Text(headers[i]);
                    ImGui.SameLine();
                }

                ImGui.Spacing(); // Space between headers and numbers
                ImGui.NewLine();
                // Find the max column length to align properly
                int maxCount = new[] { columnB.Count, columnI.Count, columnN.Count, columnG.Count, columnO.Count }.Max();

                for (int i = 0; i < maxCount; i++)
                {
                    if (i < columnB.Count) {ImGui.SameLine(); ImGui.SetCursorPosX(columnOffsets[0]); ImGui.TextColored(new Vector4(1, 1, 0, 1), columnB[i].ToString()); }
                    if (i < columnI.Count) {ImGui.SameLine(); ImGui.SetCursorPosX(columnOffsets[1]); ImGui.TextColored(new Vector4(1, 1, 0, 1), columnI[i].ToString()); }
                    if (i < columnN.Count) {ImGui.SameLine(); ImGui.SetCursorPosX(columnOffsets[2]); ImGui.TextColored(new Vector4(1, 1, 0, 1), columnN[i].ToString()); }
                    if (i < columnG.Count) {ImGui.SameLine(); ImGui.SetCursorPosX(columnOffsets[3]); ImGui.TextColored(new Vector4(1, 1, 0, 1), columnG[i].ToString()); }
                    if (i < columnO.Count) {ImGui.SameLine(); ImGui.SetCursorPosX(columnOffsets[4]); ImGui.TextColored(new Vector4(1, 1, 0, 1), columnO[i].ToString()); }

                    ImGui.NewLine(); // Move to the next row
                }
                ImGui.EndChild();
            }
            ImGui.EndTabBar();
        }
    }

    private void DeletePlayer()
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        // Find the player's tab
        TabData existingTab = tabs.FirstOrDefault(tab => tab.Title == playerName);

        if (existingTab != null)
        {
            tabs.Remove(existingTab); // Remove the tab
        }
    }

    private void GeneratePlayerBoards()
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        generatedCodes.Clear();

        // Check if the player already has a tab
        TabData existingTab = tabs.FirstOrDefault(tab => tab.Title == playerName);

        if (existingTab != null)
        {
            // Player exists, add new boards to the existing tab
            int currentBoardCount = existingTab.Boards.Count;
            for (int i = 0; i < boardCount; i++)
            {
                string code = GenerateAdminCode(playerName, currentBoardCount + i);
                generatedCodes.Add(code);
                existingTab.Boards.Add(new BingoBoard(code)); // Ensure BingoBoard constructor is valid
            }
        }
        else
        {
            // Player does not exist, create a new tab
            TabData newTab = new(playerName);
            for (int i = 0; i < boardCount; i++)
            {
                string code = GenerateAdminCode(playerName, i);
                generatedCodes.Add(code);
                newTab.Boards.Add(new BingoBoard(code));
            }
            tabs.Add(newTab);
        }

        playerName = ""; // Reset input
        boardCount = 1;
    }

    private string GenerateAdminCode(string name, int boardIndex)
    {
        Random rng = new Random();
        var randomSalt = rng.Next(1000, 9999).ToString(); // Generate a 4-digit random number

        // Combine name, board index, random salt, and secret key
        string rawData = $"{name}-{boardIndex}-{randomSalt}-SECRET_ADMIN_KEY";

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            string hashedPart = Convert.ToBase64String(hashBytes).Substring(0, 8); // Shorten hash
            return $"{hashedPart}";
        }
    }

    private void DrawBingoBoard(TabData tab)
    {
        if (ImGui.BeginTabBar("BingoBoards"))
        {
            for (int i = 0; i < tab.Boards.Count; i++)
            {
                string tabLabel = $"Board {i + 1}";
                // bool isWinner = tab.Boards[i].isWinner;

                // if (isWinner)
                // {
                //     ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.8f, 0.2f, 0.2f, 1.0f)); // Red for winners
                //     ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(1.0f, 0.4f, 0.4f, 1.0f)); // Lighter red on hover
                //     ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(1.0f, 0.6f, 0.6f, 1.0f)); // Bright red when active
                //     tab.hasWinner = true;
                // }

                if (ImGui.BeginTabItem(tabLabel))
                {
                    DrawSingleBingoBoard(tab.Boards[i]);
                    ImGui.EndTabItem();
                }

                // if (isWinner)
                // {
                //     ImGui.PopStyleColor(3); // Restore colors
                // }
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawSingleBingoBoard(BingoBoard board)
    {
        string[] bingoLetters = { "B", "I", "N", "G", "O" };

        // Display "BINGO" header
        for (int i = 0; i < 5; i++)
        {
            ImGui.SetCursorPosX(30 + i * 57);
            ImGui.Text(bingoLetters[i]);
            ImGui.SameLine();
        }

        ImGui.Spacing(); // Space between header and board

        bool[,] markedGrid = new bool[5, 5]; // Track marked positions

        for (int row = 0; row < 5; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                int index = row * 5 + col;
                int number = board.Board[index];

                // Automatically mark if in drawnNumbers (excluding FREE space)
                if (number == 0 || drawnNumbers.Contains(number))
                {
                    board.MarkedNumbers.Add(index);
                }

                bool isMarked = board.MarkedNumbers.Contains(index);
                markedGrid[row, col] = isMarked;

                // Get the number for this button
                string buttonText = (number == 0) ? "FREE" : $"{number}";

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

        // Check for a winning board
        if (CheckBingoWin(markedGrid))
        {
            board.isWinner = true;
        }
    }

    private bool CheckBingoWin(bool[,] markedGrid)
    {
        // Check rows and columns
        for (int i = 0; i < 5; i++)
        {
            if (Enumerable.Range(0, 5).All(j => markedGrid[i, j])) return true; // Row win
            if (Enumerable.Range(0, 5).All(j => markedGrid[j, i])) return true; // Column win
        }

        // Check diagonals
        if (Enumerable.Range(0, 5).All(i => markedGrid[i, i])) return true; // Main diagonal
        if (Enumerable.Range(0, 5).All(i => markedGrid[i, 4 - i])) return true; // Anti-diagonal

        return false;
    }

    private void DrawRandomNumber()
    {
        List<int> possibleNumbers = Enumerable.Range(1, 75).Except(drawnNumbers).ToList();

        if (possibleNumbers.Count > 0)
        {
            int newNumber = possibleNumbers[rng.Next(possibleNumbers.Count)];
            drawnNumbers.Add(newNumber);
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "All numbers have been drawn!"); // Red warning
        }
    }

    private void AddManualNumber()
    {
        if (int.TryParse(manualInput, out int number) && number >= 1 && number <= 75 && !drawnNumbers.Contains(number))
        {
            drawnNumbers.Add(number);
            manualInput = ""; // Clear input after adding
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "Invalid or duplicate number!"); // Red warning
        }
    }
}