using System.Collections.Generic;
using UnityEngine;

public class Knight : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tile_count_x, int tile_count_y)
    {
        List<Vector2Int> res = new List<Vector2Int>();

        // top right
        int x = currentX + 1;
        int y = currentY + 2;
        if(x < tile_count_x && y < tile_count_y && (board[x,y] == null || board[x,y].team != team))
            res.Add(new Vector2Int(x,y));

        x = currentX + 2;
        y = currentY + 1;
        if(x < tile_count_x && y < tile_count_y && (board[x, y] == null || board[x, y].team != team))
            res.Add(new Vector2Int(x, y));

        // top left
        x = currentX - 1;
        y = currentY + 2;
        if (x >= 0 && y < tile_count_y && (board[x, y] == null || board[x, y].team != team))
            res.Add(new Vector2Int(x, y));

        x = currentX - 2;
        y = currentY + 1;
        if (x >= 0 && y < tile_count_y && (board[x, y] == null || board[x, y].team != team))
            res.Add(new Vector2Int(x, y));

        // bottom right
        x = currentX + 1;
        y = currentY - 2;
        if (x < tile_count_x && y >= 0 && (board[x, y] == null || board[x, y].team != team))
            res.Add(new Vector2Int(x, y));

        x = currentX + 2;
        y = currentY - 1;
        if (x < tile_count_x && y >= 0 && (board[x, y] == null || board[x, y].team != team))
            res.Add(new Vector2Int(x, y));

        // bottom left
        x = currentX - 1;
        y = currentY - 2;
        if (x >= 0 && y >= 0 && (board[x, y] == null || board[x, y].team != team))
            res.Add(new Vector2Int(x, y));

        x = currentX - 2;
        y = currentY - 1;
        if (x >= 0 && y >= 0 && (board[x, y] == null || board[x, y].team != team))
            res.Add(new Vector2Int(x, y));

        return res;
    }
}
