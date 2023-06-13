using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tile_count_x, int tile_count_y)
    {
        List<Vector2Int> res = new List<Vector2Int>();

        //top right
        for (int x = currentX + 1, y = currentY + 1; x < tile_count_x && y < tile_count_y; x++, y++)
        {
            if (board[x, y] == null)
                res.Add(new Vector2Int(x, y));
            else
            {
                if (board[x, y].team != team)
                    res.Add(new Vector2Int(x, y));

                break;
            }
        }

        //top left
        for (int x = currentX - 1, y = currentY + 1; x >= 0 && y < tile_count_y; x--, y++)
        {
            if (board[x, y] == null)
                res.Add(new Vector2Int(x, y));
            else
            {
                if (board[x, y].team != team)
                    res.Add(new Vector2Int(x, y));

                break;
            }
        }

        //bottom left
        for (int x = currentX - 1, y = currentY - 1; x >= 0 && y >= 0; x--, y--)
        {
            if (board[x, y] == null)
                res.Add(new Vector2Int(x, y));
            else
            {
                if (board[x, y].team != team)
                    res.Add(new Vector2Int(x, y));

                break;
            }
        }

        //bottom right
        for (int x = currentX + 1, y = currentY - 1; x < tile_count_x && y >= 0; x++, y--)
        {
            if (board[x, y] == null)
                res.Add(new Vector2Int(x, y));
            else
            {
                if (board[x, y].team != team)
                    res.Add(new Vector2Int(x, y));

                break;
            }
        }

        // Down
        for (int i = currentY - 1; i >= 0; i--)
        {
            if (board[currentX, i] == null)
                res.Add(new Vector2Int(currentX, i));
            else
            {
                if (board[currentX, i].team != team)
                    res.Add(new Vector2Int(currentX, i));

                break;
            }
        }

        // Up
        for (int i = currentY + 1; i < tile_count_y; i++)
        {
            if (board[currentX, i] == null)
                res.Add(new Vector2Int(currentX, i));
            else
            {
                if (board[currentX, i].team != team)
                    res.Add(new Vector2Int(currentX, i));

                break;
            }
        }

        // Left
        for (int i = currentX - 1; i >= 0; i--)
        {
            if (board[i, currentY] == null)
                res.Add(new Vector2Int(i, currentY));
            else
            {
                if (board[i, currentY].team != team)
                    res.Add(new Vector2Int(i, currentY));

                break;
            }
        }

        // Right
        for (int i = currentX + 1; i < tile_count_x; i++)
        {
            if (board[i, currentY] == null)
                res.Add(new Vector2Int(i, currentY));
            else
            {
                if (board[i, currentY].team != team)
                    res.Add(new Vector2Int(i, currentY));

                break;
            }
        }

        return res;
    }
}
