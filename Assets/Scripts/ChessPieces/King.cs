using System.Collections.Generic;
using UnityEngine;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tile_count_x, int tile_count_y)
    {
        List<Vector2Int> res = new List<Vector2Int>();

        // right
        if(currentX + 1 < tile_count_x)
        {
            // right
            if(board[currentX + 1, currentY] == null || board[currentX + 1, currentY].team != team)
                res.Add(new Vector2Int(currentX + 1, currentY));
            // top right
            if(currentY + 1 < tile_count_y && (board[currentX + 1, currentY+1] == null || board[currentX + 1, currentY+1].team != team))
                res.Add(new Vector2Int(currentX + 1, currentY + 1));
            // bottom right
            if (currentY - 1 >= 0 && (board[currentX + 1, currentY - 1] == null || board[currentX + 1, currentY - 1].team != team))
                res.Add(new Vector2Int(currentX + 1, currentY - 1));
        }

        // left
        if (currentX - 1 >= 0)
        {
            // right
            if (board[currentX - 1, currentY] == null || board[currentX - 1, currentY].team != team)
                res.Add(new Vector2Int(currentX - 1, currentY));
            // top right
            if (currentY + 1 < tile_count_y && (board[currentX - 1, currentY + 1] == null || board[currentX - 1, currentY + 1].team != team))
                res.Add(new Vector2Int(currentX - 1, currentY + 1));
            // bottom right
            if (currentY - 1 >= 0 && (board[currentX - 1, currentY - 1] == null || board[currentX - 1, currentY - 1].team != team))
                res.Add(new Vector2Int(currentX - 1, currentY - 1));
        }

        // forward
        if(currentY + 1 < tile_count_y && (board[currentX, currentY + 1] == null || board[currentX, currentY + 1].team != team))
            res.Add(new Vector2Int(currentX, currentY + 1));
        // backward
        if (currentY - 1 >=0 && (board[currentX, currentY - 1] == null || board[currentX, currentY - 1].team != team))
            res.Add(new Vector2Int(currentX, currentY - 1));

        return res;
    }
    public override SpecialMove GetSpecialMoves(ref ChessPiece[,] board, ref List<Vector2Int[]> moveList, ref List<Vector2Int> availableMoves)
    {
        SpecialMove res = SpecialMove.None;

        var kingMove = moveList.Find(m => m[0].x == 4 && m[0].y == ((team == 0) ? 0 : 7));
        var leftRock = moveList.Find(m => m[0].x == 0 && m[0].y == ((team == 0) ? 0 : 7));
        var rightRock = moveList.Find(m => m[0].x == 7 && m[0].y == ((team == 0) ? 0 : 7));
        
        if (kingMove == null && currentX == 4)
        {
            // white team
            if (team == 0)
            {
                //left rock
                if (leftRock == null && board[0, 0].type == ChessPieceType.Rock && board[0, 0].team == 0)
                    if (board[3, 0] == null && board[2, 0] == null && board[1, 0] == null)
                    {
                        availableMoves.Add(new Vector2Int(2, 0));
                        res = SpecialMove.Castling;
                    }
                //right rock
                if (rightRock == null && board[7, 0].type == ChessPieceType.Rock && board[7, 0].team == 0)
                    if (board[5, 0] == null && board[6, 0] == null)
                    {
                        availableMoves.Add(new Vector2Int(6, 0));
                        res = SpecialMove.Castling;
                    }
            }
            // black team
            else
            {
                //left rock
                if (leftRock == null && board[0, 7].type == ChessPieceType.Rock && board[0, 7].team == 1)
                    if (board[3, 7] == null && board[2, 7] == null && board[1, 7] == null)
                    {
                        availableMoves.Add(new Vector2Int(2, 7));
                        res = SpecialMove.Castling;
                    }
                //right rock
                if (rightRock == null && board[7, 7].type == ChessPieceType.Rock && board[7, 7].team == 1)
                    if (board[5, 7] == null && board[6, 7] == null)
                    {
                        availableMoves.Add(new Vector2Int(6, 7));
                        res = SpecialMove.Castling;
                    }
            }
        }

        return res;
    }
}
