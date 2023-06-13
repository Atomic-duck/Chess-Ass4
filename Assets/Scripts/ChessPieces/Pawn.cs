using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tile_count_x, int tile_count_y)
    {
        List<Vector2Int> res = new List<Vector2Int>();
        int direction = (team == 0) ? 1 : -1;

        if(currentY + direction >= 0 && currentY + direction <= tile_count_y-1)
        {
            // one in front
            if (board[currentX, currentY + direction] == null)
                res.Add(new Vector2Int(currentX, currentY + direction));

            // two in front
            if (board[currentX, currentY + direction] == null)
            {
                //white
                if (team == 0 && currentY == 1 && board[currentX, currentY + direction * 2] == null)
                    res.Add((new Vector2Int(currentX, currentY + direction * 2)));
                //black
                if (team == 1 && currentY == 6 && board[currentX, currentY + direction * 2] == null)
                    res.Add((new Vector2Int(currentX, currentY + direction * 2)));
            }

            // kill move
            if (currentX != tile_count_x - 1 && board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
                res.Add((new Vector2Int(currentX + 1, currentY + direction)));
            if (currentX != 0 && board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
                res.Add((new Vector2Int(currentX - 1, currentY + direction)));
        }

        return res;
    }

    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        // promotion
        if ((team == 0 && currentY == 6) || (team == 1 && currentY == 1))
            return SpecialMove.Promotion;
        // en passant
        if(moveList.Count > 0)
        {
            int direction = (team == 0) ? 1 : -1;
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            if(board[lastMove[1].x, lastMove[1].y].type == ChessPieceType.Pawn && board[lastMove[1].x, lastMove[1].y].team != team)
            {
                if(Mathf.Abs(lastMove[0].y - lastMove[1].y) == 2 && lastMove[1].y == currentY)
                {
                    if(lastMove[1].x == currentX - 1)
                    {
                        availableMoves.Add(new Vector2Int(currentX - 1, currentY + direction));
                        return SpecialMove.EnPassant;
                    }
                    else if(lastMove[1].x == currentX + 1)
                    {
                        availableMoves.Add(new Vector2Int(currentX + 1, currentY + direction));
                        return SpecialMove.EnPassant;
                    }
                }
            }

            return SpecialMove.None;
        }

        return SpecialMove.None;
    }
}
