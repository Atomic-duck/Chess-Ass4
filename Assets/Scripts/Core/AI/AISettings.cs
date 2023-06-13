namespace Chess {
	using System.Collections.Generic;
	using System.Collections;
	using UnityEngine;

	public class AISettings {

		public event System.Action requestAbortSearch;

		public int depth;
		public bool useIterativeDeepening;
		public bool useTranspositionTable;

		public bool useThreading;
		public bool useFixedDepthSearch;
		public int searchTimeMillis = 1000;
		public bool endlessSearchMode;
		public bool clearTTEachMove;

		public bool useBook;
		public int maxBookPly = 10;
		
		public MoveGenerator.PromotionMode promotionsToSearch;

		public void RequestAbortSearch () {
			requestAbortSearch?.Invoke ();
		}

		public AISettings() {
			depth = 3;
			useIterativeDeepening = true;
			useTranspositionTable = true;
			useThreading = true;
			useFixedDepthSearch = true;
			endlessSearchMode = false;
			clearTTEachMove = false;
			useBook = false;
			promotionsToSearch = MoveGenerator.PromotionMode.QueenOnly;
		}

	}
}