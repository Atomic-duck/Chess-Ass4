namespace Chess {
	using System.Collections.Generic;
	using System.Threading;
	using UnityEngine;
	using static System.Math;

	public class Search {

		const int transpositionTableSize = 64000;
		const int immediateMateScore = 100000;
		const int positiveInfinity = 9999999;
		const int negativeInfinity = -positiveInfinity;

		public event System.Action<Move> onSearchComplete;

		TranspositionTable tt;
		MoveGenerator moveGenerator;

		Move bestMoveThisIteration;
		int bestEvalThisIteration;
		Move bestMove;
		int bestEval;
		int currentIterativeSearchDepth;
		bool abortSearch;

		Move invalidMove;
		MoveOrdering moveOrdering;
		AISettings settings;
		Board board;
		Evaluation evaluation;

		public Search (Board board, AISettings settings) {
			this.board = board;
			this.settings = settings;
			evaluation = new Evaluation ();
			moveGenerator = new MoveGenerator ();
			tt = new TranspositionTable (board, transpositionTableSize);
			moveOrdering = new MoveOrdering (moveGenerator, tt);
			invalidMove = Move.InvalidMove;
			int s = TranspositionTable.Entry.GetSize ();
			//Debug.Log ("TT entry: " + s + " bytes. Total size: " + ((s * transpositionTableSize) / 1000f) + " mb.");
		}

		public void StartSearch () {

			// Initialize search settings
			bestEvalThisIteration = bestEval = 0;
			bestMoveThisIteration = bestMove = Move.InvalidMove;
			tt.enabled = settings.useTranspositionTable;

			// Clearing the transposition table before each search seems to help
			// This makes no sense to me, I presume there is a bug somewhere but haven't been able to track it down yet
			if (settings.clearTTEachMove) {
				tt.Clear ();
			}

			moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
			currentIterativeSearchDepth = 0;
			abortSearch = false;

			// Iterative deepening. This means doing a full search with a depth of 1, then with a depth of 2, and so on.
			// This allows the search to be aborted at any time, while still yielding a useful result from the last search.
			if (settings.useIterativeDeepening) {
				int targetDepth = (settings.useFixedDepthSearch) ? settings.depth : int.MaxValue;

				for (int searchDepth = 1; searchDepth <= targetDepth; searchDepth++) {
					SearchMoves (searchDepth, 0, negativeInfinity, positiveInfinity);
					if (abortSearch) {
						break;
					} else {
						currentIterativeSearchDepth = searchDepth;
						bestMove = bestMoveThisIteration;
						bestEval = bestEvalThisIteration;

						// Exit search if found a mate
						if (IsMateScore (bestEval) && !settings.endlessSearchMode) {
							break;
						}
					}
				}
			} else {
				SearchMoves (settings.depth, 0, negativeInfinity, positiveInfinity);
				bestMove = bestMoveThisIteration;
				bestEval = bestEvalThisIteration;
			}

			onSearchComplete?.Invoke (bestMove);
		}

		public (Move move, int eval) GetSearchResult () {
			return (bestMove, bestEval);
		}

		public void EndSearch () {
			abortSearch = true;
		}

		int SearchMoves (int depth, int plyFromRoot, int alpha, int beta) {
			if (abortSearch) {
				return 0;
			}

			if (plyFromRoot > 0) {
				// Detect draw by repetition.
				// Returns a draw score even if this position has only appeared once in the game history (for simplicity).
				if (board.RepetitionPositionHistory.Contains (board.ZobristKey)) {
					return 0;
				}

				// Skip this position if a mating sequence has already been found earlier in
				// the search, which would be shorter than any mate we could find from here.
				// This is done by observing that alpha can't possibly be worse (and likewise
				// beta can't  possibly be better) than being mated in the current position.
				alpha = Max (alpha, -immediateMateScore + plyFromRoot);
				beta = Min (beta, immediateMateScore - plyFromRoot);
				if (alpha >= beta) {
					return alpha;
				}
			}

			// Try looking up the current position in the transposition table.
			// If the same position has already been searched to at least an equal depth
			// to the search we're doing now,we can just use the recorded evaluation.
			int ttVal = tt.LookupEvaluation (depth, plyFromRoot, alpha, beta);
			if (ttVal != TranspositionTable.lookupFailed) {
				
				if (plyFromRoot == 0) {
					bestMoveThisIteration = tt.GetStoredMove ();
					bestEvalThisIteration = tt.entries[tt.Index].value;
					//Debug.Log ("move retrieved " + bestMoveThisIteration.Name + " Node type: " + tt.entries[tt.Index].nodeType + " depth: " + tt.entries[tt.Index].depth);
				}
				return ttVal;
			}

			if (depth == 0) {
				int evaluation = QuiescenceSearch (alpha, beta);
				return evaluation;
			}

			List<Move> moves = moveGenerator.GenerateMoves (board);
			moveOrdering.OrderMoves (board, moves, settings.useTranspositionTable);
			// Detect checkmate and stalemate when no legal moves are available
			if (moves.Count == 0) {
				if (moveGenerator.InCheck ()) {
					int mateScore = immediateMateScore - plyFromRoot;
					return -mateScore;
				} else {
					return 0;
				}
			}

			int evalType = TranspositionTable.UpperBound;
			Move bestMoveInThisPosition = invalidMove;

			for (int i = 0; i < moves.Count; i++) {
				board.MakeMove (moves[i], inSearch : true);
				int eval = -SearchMoves (depth - 1, plyFromRoot + 1, -beta, -alpha);
				board.UnmakeMove (moves[i], inSearch : true);
				

				// Move was *too* good, so opponent won't allow this position to be reached
				// (by choosing a different move earlier on). Skip remaining moves.
				if (eval >= beta) {
					tt.StoreEvaluation (depth, plyFromRoot, beta, TranspositionTable.LowerBound, moves[i]);
					
					return beta;
				}

				// Found a new best move in this position
				if (eval > alpha) {
					evalType = TranspositionTable.Exact;
					bestMoveInThisPosition = moves[i];

					alpha = eval;
					if (plyFromRoot == 0) {
						bestMoveThisIteration = moves[i];
						bestEvalThisIteration = eval;
					}
				}
			}

			tt.StoreEvaluation (depth, plyFromRoot, alpha, evalType, bestMoveInThisPosition);

			return alpha;

		}

		// Search capture moves until a 'quiet' position is reached.
		int QuiescenceSearch (int alpha, int beta) {
			// A player isn't forced to make a capture (typically), so see what the evaluation is without capturing anything.
			// This prevents situations where a player ony has bad captures available from being evaluated as bad,
			// when the player might have good non-capture moves available.
			int eval = evaluation.Evaluate (board);
	
			if (eval >= beta) {
				return beta;
			}
			if (eval > alpha) {
				alpha = eval;
			}

			var moves = moveGenerator.GenerateMoves (board, false);
			moveOrdering.OrderMoves (board, moves, false);
			for (int i = 0; i < moves.Count; i++) {
				board.MakeMove (moves[i], true);
				eval = -QuiescenceSearch (-beta, -alpha);
				board.UnmakeMove (moves[i], true);

				if (eval >= beta) {
					return beta;
				}
				if (eval > alpha) {
					alpha = eval;
				}
			}

			return alpha;
		}

		public static bool IsMateScore (int score) {
			const int maxMateDepth = 1000;
			return System.Math.Abs (score) > immediateMateScore - maxMateDepth;
		}

		public static int NumPlyToMateFromScore (int score) {
			return immediateMateScore - System.Math.Abs (score);

		}
	}
}