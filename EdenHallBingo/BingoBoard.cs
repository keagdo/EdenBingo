using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
public class BingoBoard
{
    public int[] Board { get; private set; } = new int[25];
    public HashSet<int> MarkedNumbers { get; private set; }
    public bool isWinner = false;
    public string Code { get; private set; }

    public BingoBoard(string code)
    {
        Code = code;
        MarkedNumbers = new HashSet<int>();
        GenerateBingoBoard();
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


    private void GenerateBingoBoard()
    {
        var seed =  GetDeterministicHashCode(Code);       
        Random rng = new Random(seed); // Seed with code for consistency
        
        Board = new int[25]; // 5x5 grid
        
        // Assign numbers per Bingo column
        int[][] numberRanges =
        {
            Enumerable.Range(1, 15).OrderBy(x => rng.Next()).Take(5).ToArray(),  // B (1-15)
            Enumerable.Range(16, 15).OrderBy(x => rng.Next()).Take(5).ToArray(), // I (16-30)
            Enumerable.Range(31, 15).OrderBy(x => rng.Next()).Take(5).ToArray(), // N (31-45)
            Enumerable.Range(46, 15).OrderBy(x => rng.Next()).Take(5).ToArray(), // G (46-60)
            Enumerable.Range(61, 15).OrderBy(x => rng.Next()).Take(5).ToArray()  // O (61-75)
        };

        for (int col = 0; col < 5; col++)
        {
            for (int row = 0; row < 5; row++)
            {
                int index = row * 5 + col; // Convert (row, col) to 1D index
                Board[index] = numberRanges[col][row]; // Assign number from column's range
            }
        }

        // Free space in the center
        Board[12] = 0; // Middle of a 5x5 grid (index 12)
    }
}
