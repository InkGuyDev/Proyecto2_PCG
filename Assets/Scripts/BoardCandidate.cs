using System.Collections.Generic;

public class BoardCandidate
{
    public List<List<char>> board;
    public float fitness;
    public int width;
    public int height;

    public BoardCandidate(List<List<char>> sourceBoard)
    {
        width = sourceBoard.Count;
        height = width > 0 ? sourceBoard[0].Count : 0;
        board = new List<List<char>>(width);

        for (int x = 0; x < width; x++)
        {
            board.Add(new List<char>(sourceBoard[x]));
        }
        fitness = 0f;
    }

    public BoardCandidate Clone()
    {
        BoardCandidate copy = new BoardCandidate(this.board);
        copy.fitness = this.fitness;
        return copy;
    }
}