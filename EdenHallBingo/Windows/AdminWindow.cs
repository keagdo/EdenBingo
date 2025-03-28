using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using EdenHallBingo.Helpers;
using ImGuiNET;

namespace EdenHallBingo.Windows;

public class AdminWindow : Window, IDisposable
{
    public void Dispose() { }
    private string playerName = "";
    private int boardCount = 1;
    private List<string> generatedCodes = new();
    private List<TabData> tabs;
    public Plugin P;
    private List<int> drawnNumbers = new List<int> { 0 }; // List to store drawn numbers
    private string manualInput = ""; // Input field for manual number entry
    private Random rng = new Random();
    // ChatManager chatManager = new ChatManager();
    private int selectedIndex = 0;
    private Configuration Configuration;
    ChatManager chatManager = new ChatManager();
    public AdminWindow(Plugin plugin): base("Eden Hall Bingo Admin Tab##idtag", ImGuiWindowFlags.HorizontalScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            // MinimumSize = new Vector2(375, 330),
            // MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        Configuration = plugin.Configuration;
        tabs = Configuration.adminTabs;
        P = plugin;
    }

    public override void Draw()
    {
        ProcessChatQueue();
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
                        Configuration.adminTabs = tabs;
                        Configuration.Save();
                    }
                }
                else 
                {
                    ImGui.BeginDisabled();
                    ImGui.Button("Delete All Players (Hold Shift)");
                    ImGui.EndDisabled();
                }

                if (ImGui.Button("Export Player Codes to Clipboard")) 
                {
                    ExportCodes();
                }
                if (ImGui.Button("Export Draws to Clipboard")) 
                {
                    ExportNumbers();
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

            if (ImGui.BeginTabItem("Players"))
            {

                if (ImGui.BeginTabBar("PlayerTabBar"))
                {
                    for (int i = 0; i < tabs.Count; i++)
                    {
                        // bool hasWinner = tabs[i].Boards.Any(board => board.isWinner); // Check if this player has a winning board

                        if (ImGui.BeginTabItem(tabs[i].Title))
                        {
                            DrawBingoBoard(tabs[i]);
                            ImGui.EndTabItem();
                        }

                    }
                    ImGui.EndTabBar();
                }
                ImGui.EndTabItem();
            }


            if (ImGui.BeginTabItem("Draw Numbers"))
            {
                ImGui.Text("Bingo Number Draw");
                // Button to draw a random number
                if (ImGui.Button("Draw Random Number"))
                {
                    DrawRandomNumber();
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
                    string shoutMessage = $"{Configuration.Channel} Bingo Drawing #{drawnNumbers.Count-1} is {columnLetter}{lastNumber}";
                    ImGui.SetClipboardText(shoutMessage);
                    if (Configuration.SendChats)
                    {
                        Chat(shoutMessage);
                    }
                    selectedIndex = drawnNumbers.Count - 1;
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
                    selectedIndex = Math.Clamp(selectedIndex, 0, drawnNumbers.Count - 1);

                    // Navigation buttons
                    int selectedNumber = drawnNumbers[selectedIndex];
                    // Determine column letter based on selected number
                    string columnLetter = selectedNumber switch
                    {
                        >= 1 and <= 15 => "B",
                        >= 16 and <= 30 => "I",
                        >= 31 and <= 45 => "N",
                        >= 46 and <= 60 => "G",
                        >= 61 and <= 75 => "O",
                        _ => "?"
                    };

                    ImGui.Text($"Selected drawing #{selectedIndex}: ");
                    ImGui.SameLine();
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{columnLetter} {selectedNumber}"); // Red color
                    ImGui.SameLine();
                    if (ImGui.Button("Previous"))
                    {
                        if (selectedIndex > 1) selectedIndex--;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Next"))
                    {
                        if (selectedIndex < drawnNumbers.Count - 1) selectedIndex++;
                    }

                    string shoutMessage = $"{Configuration.Channel} Bingo Drawing #{selectedIndex} is {columnLetter}{selectedNumber}";
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
            }
            ImGui.EndTabBar();
        }
    }

    private void ExportNumbers()
    {
        string numbers = string.Join(", ", drawnNumbers);
        ImGui.SetClipboardText(numbers);
    }

    private void ExportCodes()
    {
        StringBuilder codes = new StringBuilder();

        foreach (var tab in tabs)
        {
            codes.AppendLine(tab.Title); // Add the tab title

            foreach (var board in tab.Boards)
            {
                codes.AppendLine(board.Code); // Add the board code under the title
            }

            codes.AppendLine(); // Add a blank line for separation
        }

        ImGui.SetClipboardText(codes.ToString());
    }

    private void DeletePlayer()
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        // Find the player's tab
        TabData? existingTab = tabs.FirstOrDefault(tab => tab.Title == playerName);

        if (existingTab != null)
        {
            tabs.Remove(existingTab); // Remove the tab
            Configuration.adminTabs = tabs;
            Configuration.Save();
        }
    }

    private void GeneratePlayerBoards()
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        generatedCodes.Clear();

        // Check if the player already has a tab
        TabData? existingTab = tabs.FirstOrDefault(tab => tab.Title == playerName);

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
            if (Configuration.SendChats)
            {
                Chat($"/tell <t> Thanks for playing Bingo! Here are your {generatedCodes.Count} extra codes! Your codes are: {string.Join(", ", generatedCodes)}");
                Chat("/tell <t> Thanks for playing Bingo! Your new boards have been loaded! Please use /bingo to see the boards!");
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
            if (Configuration.SendChats)
            {
                Chat($"/tell <t> Thanks for playing Bingo! I am sending you {generatedCodes.Count} codes to start your game! Your codes are: {string.Join(", ", generatedCodes)}");
                Chat("/tell <t> Thanks for playing Bingo! Your new boards have been loaded! Please use /bingo to see the boards!");
            }
        }
        Configuration.adminTabs = tabs;
        Configuration.Save();

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

                if (ImGui.BeginTabItem(tabLabel))
                {
                    DrawSingleBingoBoard(tab.Boards[i]);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawSingleBingoBoard(BingoBoard board)
    {
        ImGui.Text($"Board Code: {board.Code}");
        ImGui.SameLine();
        if (ImGui.Button("Copy Code Clipboard"))
        {
            ImGui.SetClipboardText(board.Code);
        }
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
    private Queue<string> chatQueue = new Queue<string>();
    private float chatCooldown = 2.00f; // Adjust delay as needed (in seconds)
    private DateTime lastChatTime = DateTime.MinValue;

    public void Chat(string message)
    {
        chatQueue.Enqueue(message);
    }

    public async Task AwaitChatQueue()
    {
        while (chatQueue.Count > 0)
        {
            await Task.Delay(500);
        }
    }
    private void ProcessChatQueue()
    {
        if (chatQueue.Count > 0 && (DateTime.Now - lastChatTime).TotalSeconds >= chatCooldown)
        {
            string message = chatQueue.Dequeue();
            chatManager.SendMessage(message);
            lastChatTime = DateTime.Now; // Update the last chat time
        }
    }
}