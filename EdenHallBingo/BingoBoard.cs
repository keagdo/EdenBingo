using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

        public static int GetStableHash(string input)
    {
        using (var md5 = MD5.Create()) // Can use other stable hashes too
        {
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToInt32(hash, 0); // Convert first 4 bytes to int
        }
    }

    private void GenerateBingoBoard()
    {
        Random rng = new Random(GetStableHash(Code)); // Seed with code for consistency
        
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
