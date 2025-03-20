using System;
using System.Collections.Generic;
using System.Linq;

public class TabData
{
    public string Title { get; private set; }
    public List<BingoBoard> Boards { get; private set; } = new();
    public bool hasWinner = false;
    public TabData(string title)
    {
        Title = title;
    }

    public void AddBoard(string code)
    {
        Boards.Add(new BingoBoard(code));
    }
}