using System.Collections.Generic;
using UnityEngine;
using Unity.Networking.Transport;
using UnityEngine.UI;
using Chess.Game;
using Chess;

public enum SpecialMove
{
    None = 0,
    EnPassant,
    Castling,
    Promotion
}

public class Chessboard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffset = 1.5f;
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private GameObject conntinueButton;
    [SerializeField] private Transform rematchIndicator;
    [SerializeField] private Button rematchButton;
    [SerializeField] private GameObject Notify;
    [SerializeField] private TMPro.TMP_Dropdown level_dropdown;

    [Header("Prefabs and Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // Logic
    public const int whiteTeam = 0, blackTeam = 1;
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<ChessPiece> deadWhites = new List<ChessPiece>();
    private List<ChessPiece> deadBlacks = new List<ChessPiece>();
    private List<Vector2Int[]> moveList = new List<Vector2Int[]>();
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;
    public bool isWhiteTurn;
    private SpecialMove specialMove;
    private bool moved = false;
    private bool finish = false;

    // AI
    GameManager gameManager;
    bool isMoved;
    Coord startSquare, targetSquare;
    bool hmIsWhite;


    // Multi logic
    private int playerCount = -1;
    private int currentTeam = -1;
    private bool localGame = true;
    private bool[] playerRematch = new bool[2];

    private void Start()
    {
        isWhiteTurn = true;
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();

        gameManager = new GameManager(GameManager.PlayerType.Human, GameManager.PlayerType.AI);
        gameManager.onMoveMade += GameManager_onMoveMade;
        hmIsWhite = true;

        RegisterEvents();
    }

    private void GameManager_onMoveMade(Move move)
    {
        int moveFrom = move.StartSquare;
        int startX = moveFrom % 8;
        int startY = moveFrom / 8;
        int moveTo = move.TargetSquare;
        int endX = moveTo % 8;
        int endY = moveTo / 8;

        ChessPiece target = chessPieces[startX, startY];
        availableMoves = target.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
        specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

        MoveTo(startX, startY, endX, endY);
    }

    private void Update()
    {
        if (localGame)
        {
            if (gameManager.playerToMove is HumanPlayer)
            {
                PlayerPlay();
                if (isMoved)
                {
                    if(hmIsWhite)
                        gameManager.whitePlayer.TryMakeMove(startSquare, targetSquare);
                    else
                        gameManager.blackPlayer.TryMakeMove(startSquare, targetSquare);
                    isMoved = false;
                }
            }
            else
            {
                gameManager.Update();
                Debug.Log("Aaaa");
            }
   
            if(gameManager.gameResult == GameManager.Result.FiftyMoveRule 
                || gameManager.gameResult == GameManager.Result.InsufficientMaterial
                || gameManager.gameResult == GameManager.Result.Stalemate
                || gameManager.gameResult == GameManager.Result.Repetition)
            {
                CheckMate(2);
            }
        }
        else
        {
            PlayerPlay();
        }
        
    }

    /*private void AIPlay()
    {
        if (true)
        {
            PieceInfo[,] simulation = new PieceInfo[TILE_COUNT_X, TILE_COUNT_Y];
            List<Vector2Int> moveListhis = new List<Vector2Int>();
            for (int x = 0; x < TILE_COUNT_X; x++)
            {
                for(int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (chessPieces[x, y] != null)
                    {
                        simulation[x, y] = BoardUtility.SpawnPiece(chessPieces[x, y].type, chessPieces[x, y].team);
                        simulation[x, y].currentX = x;
                        simulation[x, y].currentY = y;
                    }
                }
            }

            search.StartSearch(simulation, moveList);
        }
    }*/
    private void PlayerPlay()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 1000, LayerMask.GetMask("Tile", "Hover", "Highlight")))
        {
            // get indexes of the hitted tile
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            // if hovering a tile after not hovering any tile
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Hover");
            }
            // if already hovering a tile, change the previous one
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Hover");
            }

            // if press down mouse
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    // is our turn??
                    if ((chessPieces[hitPosition.x, hitPosition.y].team == 0 && isWhiteTurn && (currentTeam == 0 || localGame)) || (chessPieces[hitPosition.x, hitPosition.y].team == 1 && !isWhiteTurn && (currentTeam == 1 || localGame)))
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];

                        // get list where i can go and highlight it
                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                        // get a list of special moves
                        specialMove = currentlyDragging.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

                        PreventCheck(currentlyDragging, ref availableMoves);
                        HighlightTiles();
                    }
                }
            }

            // if release mouse
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int prevPos = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                if (ContainValidMove(ref availableMoves, new Vector2Int(hitPosition.x, hitPosition.y)))
                {
                    MoveTo(prevPos.x, prevPos.y, hitPosition.x, hitPosition.y);
                    isMoved = true;
                    
                    startSquare = new Coord(prevPos.x, prevPos.y);
                    targetSquare = new Coord(hitPosition.x, hitPosition.y);
                    moved = true;
                    // net implement
                    NetMakeMove mm = new NetMakeMove();
                    mm.originalX = prevPos.x;
                    mm.originalY = prevPos.y;
                    mm.destinationX = hitPosition.x;
                    mm.destinationY = hitPosition.y;
                    mm.teamId = currentTeam;
                    Client.Instance.SendToServer(mm);
                }
                else
                {
                    currentlyDragging.SetPosition(GetTileCenter(prevPos.x, prevPos.y));
                }

                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = (ContainValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTiles();
            }
        }

        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }
    }
    // generate board
    private void GenerateAllTiles(float tileSize, int tile_count_x, int tile_count_y)
    {
        yOffset += transform.position.y;
        bounds = new Vector3(tile_count_x / 2 * tileSize, 0, tile_count_x / 2 * tileSize) + boardCenter;
        tiles = new GameObject[tile_count_x, tile_count_y];
        for (int x = 0; x < tile_count_x; x++)
        {
            for (int y = 0; y < tile_count_y; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
   {
      GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
      tileObject.transform.parent = transform;

      Mesh mesh = new Mesh();
      tileObject.AddComponent<MeshFilter>().mesh = mesh;
      tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x*tileSize, yOffset, y*tileSize) - bounds;
        vertices[1] = new Vector3(x*tileSize, yOffset, (y+1)*tileSize) - bounds;
        vertices[2] = new Vector3((x+1)*tileSize, yOffset, y*tileSize) - bounds;
        vertices[3] = new Vector3((x+1)*tileSize, yOffset, (y+1)*tileSize) - bounds;

        int[] tris = new int[] { 0, 1, 2, 1,3,2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

      return tileObject;
   }

    // spawning pieces
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        // white team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rock, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rock, whiteTeam);
        for(int i=0; i<TILE_COUNT_X; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        // black team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rock, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rock, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece piece = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();   // cause we are using prefab
        piece.team = team;
        piece.type = type;
        Material[] materials = { piece.GetComponent<MeshRenderer>().material, teamMaterials[team] };
        piece.GetComponent<MeshRenderer>().materials = materials;

        return piece;
    }

    // position
    private void PositionAllPieces()
    {
        for(int x = 0; x < TILE_COUNT_X; x++)
            for(int y = 0; y < TILE_COUNT_Y; y++)
                if(chessPieces[x,y] != null)
                    PositionSinglePiece(x,y, true);
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x,y].currentX = x;
        chessPieces[x,y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x*tileSize + tileSize/2, yOffset, y*tileSize + tileSize/2) - bounds;
    }

    // check mate
    private void CheckMate(int team)
    {
        DisplayVictory(team);
        finish = true;
    }

    private void DisplayVictory(int win_team)
    {
        victoryScreen.SetActive(true);
        victoryScreen.transform.GetChild(win_team).gameObject.SetActive(true);
    }

    public void OnRematchButton()
    {
        if (localGame)
        {
            NetRematch wrm = new NetRematch();
            wrm.teamId = 0;
            wrm.wantRematch = 1;
            Client.Instance.SendToServer(wrm);

            NetRematch brm = new NetRematch();
            brm.teamId = 1;
            brm.wantRematch = 1;
            Client.Instance.SendToServer(brm);

            moved = false;
            finish = false;
        }
        else
        {
            NetRematch rm = new NetRematch();
            rm.teamId = currentTeam;
            rm.wantRematch = 1;
            Client.Instance.SendToServer(rm);

            rematchIndicator.transform.GetChild(2).gameObject.SetActive(true);
        }
    }
    private void ResetUI()
    {
        //UI
        rematchButton.interactable = true;
        rematchIndicator.transform.GetChild(0).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(1).gameObject.SetActive(false);
        rematchIndicator.transform.GetChild(2).gameObject.SetActive(false);

        victoryScreen.transform.GetChild(0).gameObject.SetActive(false);
        victoryScreen.transform.GetChild(1).gameObject.SetActive(false);
        victoryScreen.SetActive(false);
    }
    private void ResetChessBoard()
    {
        gameManager.NewGame(hmIsWhite);
        gameManager.aiSettings.depth = level_dropdown.value + 5;

        // fields reset
        currentlyDragging = null;
        availableMoves.Clear();
        moveList.Clear();
        playerRematch[0] = playerRematch[1] = false;

        // clean up
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                    Destroy(chessPieces[x, y].gameObject);

                chessPieces[x, y] = null;
            }

        for (int i = 0; i < deadWhites.Count; i++)
            Destroy(deadWhites[i].gameObject);
        for (int i = 0; i < deadBlacks.Count; i++)
            Destroy(deadBlacks[i].gameObject);
        deadWhites.Clear();
        deadBlacks.Clear();

        SpawnAllPieces();
        PositionAllPieces();
        isWhiteTurn = true;
    }
    public void GameReset()
    {
        ResetUI();
        ResetChessBoard();
    }
    public void OnMenuButton()
    {
        if (!localGame && Client.Instance.IsActive())
        {
            NetRematch rm = new NetRematch();
            rm.teamId = currentTeam;
            rm.wantRematch = 0;
            Client.Instance.SendToServer(rm);
            GameReset();
        }
        else
        {
            ResetUI();
            conntinueButton.SetActive(moved && !finish);
        }

        Notify.SetActive(false);
        GameUI.Instance.OnLeaveFromGameMenu();
        Invoke("ShutdownRelay", 0.3f);
        // reset some vuales
        playerCount = -1;
        currentTeam = -1;
    }
    
    public void HandleInputData(int val)
    {
        Debug.Log(val);
    }
    public void OnWhiteNewGameButton()
    {
        hmIsWhite = true;
        moved = false;
        finish = false;
        GameReset();
        currentTeam = 0;
        GameUI.Instance.OnLocalGameButton();
    }

    public void OnBlackNewGameButton()
    {
        hmIsWhite = false;
        moved = false;
        finish = false;
        GameReset();
        currentTeam = 1;
        GameUI.Instance.OnLocalGameButton();
    }

    // special move
    private void ProcessSpecialMove() // if special move performed, process it
    {
        if(specialMove == SpecialMove.EnPassant)
        {
            var newMove = moveList[moveList.Count - 1];
            ChessPiece myPawn = chessPieces[newMove[1].x, newMove[1].y];
            var targetPawnPos = moveList[moveList.Count - 2];
            ChessPiece enemyPawn = chessPieces[targetPawnPos[1].x, targetPawnPos[1].y];

            if(myPawn.currentX == enemyPawn.currentX)
            {
                if(myPawn.currentY == enemyPawn.currentY - 1 || myPawn.currentY == enemyPawn.currentY + 1)
                {
                    if(enemyPawn.team == 0)
                    {
                        deadWhites.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                            - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.forward * deathSpacing) * deadWhites.Count);
                    }
                    else
                    {
                        deadBlacks.Add(enemyPawn);
                        enemyPawn.SetScale(Vector3.one * deathSize);
                        enemyPawn.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                            - bounds
                            + new Vector3(tileSize / 2, 0, tileSize / 2)
                            + (Vector3.back * deathSpacing) * deadBlacks.Count);
                    }
                    chessPieces[enemyPawn.currentX, enemyPawn.currentY] = null;
                }
            }
        }
        else if(specialMove == SpecialMove.Castling)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];

            //left Rock
            if(lastMove[1].x == 2)
            {
                if(lastMove[1].y == 0) // white side
                {
                    ChessPiece rock = chessPieces[0, 0];
                    chessPieces[3,0] = rock;
                    PositionSinglePiece(3, 0);
                    chessPieces[0, 0] = null;
                }
                else if(lastMove[1].y == 7) // black side
                {
                    ChessPiece rock = chessPieces[0, 7];
                    chessPieces[3, 7] = rock;
                    PositionSinglePiece(3, 7);
                    chessPieces[0, 7] = null;
                }
            }
            //right rock
            else if(lastMove[1].x == 6)
            {
                if (lastMove[1].y == 0) // white side
                {
                    ChessPiece rock = chessPieces[7, 0];
                    chessPieces[5, 0] = rock;
                    PositionSinglePiece(5, 0);
                    chessPieces[7, 0] = null;
                }
                else if (lastMove[1].y == 7) // black side
                {
                    ChessPiece rock = chessPieces[7, 7];
                    chessPieces[5, 7] = rock;
                    PositionSinglePiece(5, 7);
                    chessPieces[7, 7] = null;
                }
            }
        }
        else if(specialMove == SpecialMove.Promotion)
        {
            Vector2Int[] lastMove = moveList[moveList.Count - 1];
            ChessPiece targetPawn = chessPieces[lastMove[1].x, lastMove[1].y];

            if(targetPawn.type == ChessPieceType.Pawn)
            {
                if(targetPawn.team == 0 && lastMove[1].y == 7)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 0);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
                else if (targetPawn.team == 1 && lastMove[1].y == 0)
                {
                    ChessPiece newQueen = SpawnSinglePiece(ChessPieceType.Queen, 1);
                    newQueen.transform.position = chessPieces[lastMove[1].x, lastMove[1].y].transform.position;
                    Destroy(chessPieces[lastMove[1].x, lastMove[1].y].gameObject);
                    chessPieces[lastMove[1].x, lastMove[1].y] = newQueen;
                    PositionSinglePiece(lastMove[1].x, lastMove[1].y);
                }
            }
        }
    }
    private int CheckForCheckMate()
    {
        var lastMove = moveList[moveList.Count - 1];
        int targetTeam = (chessPieces[lastMove[1].x, lastMove[1].y].team == 0)? 1:0;

        List<ChessPiece> attackingPieces = new List<ChessPiece>();
        List<ChessPiece> defendingPieces = new List<ChessPiece>();
        ChessPiece targetKing = null;
        for(int x = 0; x <TILE_COUNT_X; x++)
            for(int y = 0; y <TILE_COUNT_Y; y++)
                if(chessPieces[x,y] != null)
                {
                    if(chessPieces[x,y].team == targetTeam)
                    {
                        defendingPieces.Add(chessPieces[x,y]);
                        if(chessPieces[x,y].type == ChessPieceType.King)
                            targetKing = chessPieces[x,y];
                    }
                    else
                    {
                        attackingPieces.Add(chessPieces[x,y]);
                    }
                }
        // is the king attacked right now?
        List<Vector2Int> currentAvailableMoves = new List<Vector2Int>();
        for(int i = 0; i< attackingPieces.Count; i++)
        {
            var pieceMoves = attackingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            for (int b = 0; b < pieceMoves.Count; b++)
                currentAvailableMoves.Add(pieceMoves[b]);
        }

        // are we in check right now?
        if (ContainValidMove(ref currentAvailableMoves, new Vector2Int(targetKing.currentX, targetKing.currentY))){
            for(int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(ref chessPieces, defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count != 0)
                    return 0;
            }

            return 1;
        }
        else
        {
            for (int i = 0; i < defendingPieces.Count; i++)
            {
                List<Vector2Int> defendingMoves = defendingPieces[i].GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                SimulateMoveForSinglePiece(ref chessPieces, defendingPieces[i], ref defendingMoves, targetKing);

                if (defendingMoves.Count != 0)
                    return 0;
            }

            return 2;
        }
    }
    // operations
    private void PreventCheck(ChessPiece cp, ref List<Vector2Int> moves) // delete all move lead to checked
    {
        ChessPiece targetKing = null;
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null && chessPieces[x, y].type == ChessPieceType.King)
                    if (chessPieces[x, y].team == cp.team)
                        targetKing = chessPieces[x, y];

        // delete move that are putting in check
        SimulateMoveForSinglePiece(ref chessPieces, cp, ref moves, targetKing);
    }
    private bool ContainValidMove(ref List<Vector2Int> moves, Vector2Int pos)   // check is pos in moves list
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].x == pos.x && moves[i].y == pos.y)
                return true;

        return false;
    }
    private void SimulateMoveForSinglePiece(ref ChessPiece[,] board, ChessPiece cp, ref List<Vector2Int> moves, ChessPiece targetKing)   // remove all move lead to checked
    {
        // save current values, to reset after function call
        int actualX = cp.currentX;
        int actualY = cp.currentY;
        List<Vector2Int> movesToRemove = new List<Vector2Int>();

        // going through all moves
        for (int i = 0; i < moves.Count; i++)
        {
            int simX = moves[i].x;
            int simY = moves[i].y;

            Vector2Int kingPosThisSim = new Vector2Int(targetKing.currentX, targetKing.currentY);
            // did we simulate the king's move
            if (cp.type == ChessPieceType.King)
                kingPosThisSim = new Vector2Int(simX, simY);

            // copy the [,] and not a reference
            ChessPiece[,] simulation = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
            List<ChessPiece> simAttackKingPiece = new List<ChessPiece>();
            for (int x = 0; x < TILE_COUNT_X; x++)
                for (int y = 0; y < TILE_COUNT_Y; y++)
                {
                    if (board[x, y] != null)
                    {
                        simulation[x, y] = board[x, y];
                        if (simulation[x, y].team != cp.team)
                            simAttackKingPiece.Add(simulation[x, y]);
                    }
                }

            // simulate that move
            simulation[actualX, actualY] = null;
            cp.currentX = simX;
            cp.currentY = simY;
            simulation[simX, simY] = cp;

            // did one of the pieces got taken down during simulation
            var deadPiece = simAttackKingPiece.Find(c => c.currentX == simX && c.currentY == simY);
            if (deadPiece != null)
                simAttackKingPiece.Remove(deadPiece);

            // get all the simulated attacking pieces moves
            List<Vector2Int> simMoves = new List<Vector2Int>();
            for (int a = 0; a < simAttackKingPiece.Count; a++)
            {
                var pieceMoves = simAttackKingPiece[a].GetAvailableMoves(ref simulation, TILE_COUNT_X, TILE_COUNT_Y);
                for (int b = 0; b < pieceMoves.Count; b++)
                    simMoves.Add(pieceMoves[b]);
            }

            // is the king in trouble? if so, remove the move
            if (ContainValidMove(ref simMoves, kingPosThisSim))
            {
                movesToRemove.Add(moves[i]);
            }

            // restore the actual cp data
            cp.currentX = actualX;
            cp.currentY = actualY;
        }
        // remove from current available move list
        for (int i = 0; i < movesToRemove.Count; i++)
            moves.Remove(movesToRemove[i]);
    }
    private void MoveTo(int originalX, int originalY, int x, int y)
    {
        ChessPiece cp = chessPieces[originalX, originalY];
        Vector2Int prevPos = new Vector2Int(originalX, originalY);

        // is there another piece on the target position?
        if(chessPieces[x,y] != null)
        {
            ChessPiece ocp = chessPieces[x,y];
            if(cp.team == ocp.team)
            {
                return;
            }

            if(ocp.team == 0)
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(1);

                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8*tileSize, yOffset, -1 * tileSize) 
                    - bounds 
                    + new Vector3(tileSize/2, 0, tileSize/2) 
                    + (Vector3.forward * deathSpacing)*deadWhites.Count);
            }
            else
            {
                if (ocp.type == ChessPieceType.King)
                    CheckMate(0);

                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[prevPos.x, prevPos.y] = null;
        PositionSinglePiece(x, y);
        isWhiteTurn = !isWhiteTurn;

        // addition stuff
        moveList.Add(new Vector2Int[] { prevPos, new Vector2Int(x, y) });
        ProcessSpecialMove();
        if (CheckForCheckMate() == 1)
            CheckMate(cp.team);

        RemoveHighlightTiles();
    }
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");

    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");

        availableMoves.Clear();

    }
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for(int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x,y] == hitInfo)
                    return new Vector2Int(x,y);

        return -Vector2Int.one;
    }

    #region
    private void RegisterEvents()
    {
        NetUtility.S_WELCOME += OnWelcomeServer;
        NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
        NetUtility.S_REMATCH += OnRematchServer;

        NetUtility.C_WELCOME += OnWelcomeClient;
        NetUtility.C_START_GAME += OnStartGameClient;
        NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
        NetUtility.C_REMATCH += OnRematchClient;

        GameUI.Instance.SetLocalGame += OnSetLocalGame;
        Client.Instance.connectionDropped += ConnectionDropped;
    }
    private void UnregisterEvents()
    {
        NetUtility.S_WELCOME -= OnWelcomeServer;
        NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
        NetUtility.S_REMATCH -= OnRematchServer;

        NetUtility.C_WELCOME -= OnWelcomeClient;
        NetUtility.C_START_GAME -= OnStartGameClient;
        NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
        NetUtility.C_REMATCH -= OnRematchClient;

        GameUI.Instance.SetLocalGame -= OnSetLocalGame;
    }
    // AI
    /*void onSearchComplete(Move move)
    {
        ChessPiece target = chessPieces[move.start.x, move.start.y];
        availableMoves = target.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
        specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

        MoveTo(move.start.x, move.start.y, move.end.x, move.end.y);
    }*/
    // server
    private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
    {
        // client has connected, assign a team and return a message back to him
        NetWelcome nw = msg as NetWelcome;

        // assign a team
        nw.AssignedTeam = ++playerCount;

        // return back to client
        Server.Instance.SendToClient(cnn, nw);

        // if full, start the game
        if(playerCount == 1)
        {
            Server.Instance.Broadcast(new NetStartGame());
        }
    }
    private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
    {
        // receive message, broadcast it
        NetMakeMove mm = msg as NetMakeMove;

        // do some validate check

        // receive and broadcast it back
        Server.Instance.Broadcast(mm);
    }
    private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
    {
        Server.Instance.Broadcast(msg);
    }

    // client
    private void OnWelcomeClient(NetMessage msg)
    {
        // receive the connection message
        NetWelcome nw = msg as NetWelcome;

        // assign the team
        if(!localGame)
            currentTeam = nw.AssignedTeam;

        if (localGame)
            Server.Instance.Broadcast(new NetStartGame());
    }

    private void OnStartGameClient(NetMessage msg)
    {
        // change camera
        GameUI.Instance.ChangeCamera((currentTeam == 0)? CameraAngle.whiteTeam : CameraAngle.blackTeam);
    }
    private void OnMakeMoveClient(NetMessage msg)
    {
        NetMakeMove mm = msg as NetMakeMove;
        if(mm.teamId != currentTeam)
        {
            ChessPiece target = chessPieces[mm.originalX, mm.originalY];
            availableMoves = target.GetAvailableMoves(ref chessPieces, TILE_COUNT_X, TILE_COUNT_Y);
            specialMove = target.GetSpecialMoves(ref chessPieces, ref moveList, ref availableMoves);

            MoveTo(mm.originalX, mm.originalY, mm.destinationX, mm.destinationY);
        }
    }

    private void OnRematchClient(NetMessage msg)
    {
        NetRematch rm = msg as NetRematch;
        playerRematch[rm.teamId] = rm.wantRematch == 1;

        // active the piece of UI
        if(rm.teamId != currentTeam && currentTeam != -1 && !localGame)
        {
            rematchIndicator.transform.GetChild((rm.wantRematch == 1)? 0:1).gameObject.SetActive(true);
            rematchIndicator.transform.GetChild(2).gameObject.SetActive(false);
            if (rm.wantRematch != 1)
            {
                rematchButton.interactable = false;
            }
        }

        // if both want rematch
        if (playerRematch[0] && playerRematch[1])
            GameReset();
    }
    private void ShutdownRelay()
    {
        Client.Instance.Shutdown();
        Server.Instance.Shutdown();
    }
    private void OnSetLocalGame(bool v)
    {
        if(!v)
            currentTeam = -1;
        playerCount = -1;
        localGame = v;
    }
    private void ConnectionDropped()
    {
        Notify.SetActive(true);
    }
    #endregion
}
