using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Chess.Game {
	public class HumanPlayer : Player {
		Board board;
		public HumanPlayer (Board board) {
			this.board = board;
		}

		public override void NotifyTurnToMove () {

		}

		public override void Update () {
			
		}

		public override void TryMakeMove (Coord startSquare, Coord targetSquare) {
			int startIndex = BoardRepresentation.IndexFromCoord (startSquare);
			int targetIndex = BoardRepresentation.IndexFromCoord (targetSquare);
			bool moveIsLegal = false;
			Move chosenMove = new Move ();

			MoveGenerator moveGenerator = new MoveGenerator ();
			bool wantsKnightPromotion = Input.GetKey (KeyCode.LeftAlt);

			var legalMoves = moveGenerator.GenerateMoves (board);
			for (int i = 0; i < legalMoves.Count; i++) {
				var legalMove = legalMoves[i];

				if (legalMove.StartSquare == startIndex && legalMove.TargetSquare == targetIndex) {
					if (legalMove.IsPromotion) {
						if (legalMove.MoveFlag == Move.Flag.PromoteToQueen && wantsKnightPromotion) {
							continue;
						}
						if (legalMove.MoveFlag != Move.Flag.PromoteToQueen && !wantsKnightPromotion) {
							continue;
						}
					}
					moveIsLegal = true;
					chosenMove = legalMove;
					//	Debug.Log (legalMove.PromotionPieceType);
					break;
				}
			}

			Debug.Log(moveIsLegal);

			if (moveIsLegal) {
				ChoseMove (chosenMove);
			}
		}
	}
}