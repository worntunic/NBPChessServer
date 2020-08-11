using ServiceStack.Redis;
using System;
using System.Collections.Generic;

namespace RedisData
{
    public class ChessGame
    {
        public enum GameState
        {
            WhiteMove, BlackMove, WhiteWin, BlackWin, Tie
        }
        readonly RedisClient redis = new RedisClient(Config.SingleHost);
        //Keys
        private const string latestGameIDKey = "nbpc:ai:game";
        private const string keyPrefix = "nbpc:game:";
        private const string movesSuffix = ":moves";
        private const string gameDataSuffix = ":data";
        private const string wPlayerKey = "wplayer", bPlayerKey = "bplayer";
        private const string wTimeLeftLastMoveKey = "wtimeleft", bTimeLeftLastMoveKey = "btimeleft";
        private const string wLastMoveDateKey = "wlastmovedate", bLastMoveDateKey = "blastmovedate";
        private const string gameStateKey = "state";
        private const string gameTimeKey = "gametime";
        private const string startDateKey = "startdate";
        //Generated Keys
        private string gameKey, movesKey, gameDataKey;
        //Loaded flags
        private bool gameDataLoaded = false, movesLoaded = false;

        public int ID { get; private set; }
        private List<AlgebraicMove> moves;

        private Player whitePlayer, blackPlayer;
        private Dictionary<string, string> gameData;


        public ChessGame()
        {

        }

        public ChessGame(int gameID)
        {
            ID = gameID;
            GenerateKeys();
            LoadGame();
        }
        private void GenerateKeys()
        {
            gameKey = keyPrefix + ID;
            movesKey = gameKey + movesSuffix;
            gameDataKey = gameKey + gameDataKey;
        }

        public void CreateGame(int whitePlayerID, int blackPlayerID, int gameTimeInSeconds)
        {
            ID = (int)redis.Incr(latestGameIDKey);
            GenerateKeys();

            SetInitialData(whitePlayerID, blackPlayerID, gameTimeInSeconds);

            GetWhitePlayer().AddGame(ID);
            GetBlackPlayer().AddGame(ID);
        }

        private void SetInitialData(int whitePlayerID, int blackPlayerID, int gameTimeInSeconds)
        {
            GameState gameState = GameState.WhiteMove;

            Dictionary<string, string> dbData = new Dictionary<string, string>()
            {
                { wPlayerKey, whitePlayerID.ToString() },
                { bPlayerKey, blackPlayerID.ToString() },
                { gameStateKey, ((int)gameState).ToString() },
                { gameTimeKey, gameTimeInSeconds.ToString() },
                { startDateKey, DateTime.UtcNow.ToString() },
                { wLastMoveDateKey, DateTime.UtcNow.ToString() },
                { bLastMoveDateKey, DateTime.UtcNow.ToString() },
                { wTimeLeftLastMoveKey, gameTimeInSeconds.ToString() },
                { bTimeLeftLastMoveKey, gameTimeInSeconds.ToString() }
            };
            redis.SetRangeInHash(gameDataKey, dbData);
        }
        public void ReloadGameData()
        {
            gameDataLoaded = false;
            LoadGameData();
        }
        private void LoadGameData()
        {
            if (!gameDataLoaded)
            {
                gameData = redis.GetAllEntriesFromHash(gameDataKey);
                whitePlayer = new Player(int.Parse(gameData[wPlayerKey]));
                blackPlayer = new Player(int.Parse(gameData[bPlayerKey]));
                gameDataLoaded = true;
            }
        }
        public void ReloadMoves()
        {
            movesLoaded = false;
            LoadMoves();
        }
        private void LoadMoves()
        {
            if (!movesLoaded)
            {
                List<string> moveList = redis.GetAllItemsFromList(movesKey);
                moves = new List<AlgebraicMove>();
                for (int i = 0; i < moveList.Count; i++)
                {
                    AlgebraicMove move = new AlgebraicMove();
                    move.move = moveList[i];
                    moves.Add(move);
                }
                movesLoaded = true;
            }
        }

        private void LoadGame()
        {
            LoadGameData();
            LoadMoves();
        }

        public void PlayMove(int playerID, string move, int stateAfterMove)
        {
            GameState gameState = GetGameState();
            if (gameState == GameState.WhiteMove && playerID == GetWhitePlayer().ID
                || gameState == GameState.BlackMove && playerID == GetBlackPlayer().ID)
            {
                RegisterMove(move, stateAfterMove);
            } else
            {
                throw new Exception($"Player {playerID} isn't on the move.");
            }
        }

        private void RegisterMove(string move, int stateAfterMove)
        {
            AlgebraicMove algMove = new AlgebraicMove();
            algMove.move = move;
            //TODO: If it's a game ending move
            //Turn to another player
            //Enter the move first
            redis.AddItemToList(movesKey, algMove.move);
            if (movesLoaded)
            {
                moves.Add(algMove);
            }
            UpdateTimeAfterMove();
            SetDataEntry(gameStateKey, stateAfterMove.ToString());

            GameState newGameState = (GameState)stateAfterMove;
            if (newGameState == GameState.Tie || newGameState == GameState.WhiteWin || newGameState == GameState.BlackWin)
            {
                FinishGame();
            }

        }

        private void FinishGame()
        {
            CalculateRanks();
            GetWhitePlayer().FinishGame(ID);
            GetBlackPlayer().FinishGame(ID);
        }

        private void CalculateRanks()
        {
            float realRankW = GetWhitePlayer().GetRank();
            float realRankB = GetBlackPlayer().GetRank();
            double expOutW, expOutB;
            int weighingFactorK = 15;
            double gameOutcomeW, gameOutcomeB;
            double newRankW, newRankB;
            //Get expected outcome for W and B
            expOutW = 1 / (1 + Math.Pow(10, ((realRankB - realRankW) / 400)));
            expOutB = 1 / (1 + Math.Pow(10, ((realRankW - realRankB) / 400)));
            //Round
            expOutW = Math.Round(expOutW, 2);
            expOutB = Math.Round(expOutB, 2);
            //Get game outcomes
            GameState gameState = GetGameState();
            if (gameState == GameState.WhiteWin)
            {
                gameOutcomeW = 1;
                gameOutcomeB = 0;
            } else if (gameState == GameState.BlackWin)
            {
                gameOutcomeW = 0;
                gameOutcomeB = 1;
            } else if (gameState == GameState.Tie)
            {
                gameOutcomeW = 0.5;
                gameOutcomeB = 0.5;
            } else
            {
                throw new Exception("Invalid Gamestate for rank calculation");
            }
            newRankW = realRankW + weighingFactorK * (gameOutcomeW - expOutW);
            newRankB = realRankB + weighingFactorK * (gameOutcomeB - expOutB);
            //Round
            newRankW = Math.Round(newRankW);
            newRankB = Math.Round(newRankB);

            GetWhitePlayer().SetRank((int)newRankW);
            GetBlackPlayer().SetRank((int)newRankB);
        }

        public List<AlgebraicMove> GetMoves()
        {
            LoadMoves();
            return moves;
        }
        public List<string> GetMovesAsString()
        {
            LoadMoves();
            List<string> moveStrings = new List<string>();
            for (int i = 0; i < moves.Count; i++)
            {
                moveStrings.Add(moves[i].move);
            }
            return moveStrings;
        }

        private void UpdateTimeAfterMove()
        {
            if (GetGameState() == GameState.BlackMove)
            {
                SetDataEntry(bTimeLeftLastMoveKey, GetBlackActualTimeLeft().ToString());
                SetDataEntry(bLastMoveDateKey, DateTime.UtcNow.ToString());
            }
            else
            {
                SetDataEntry(wTimeLeftLastMoveKey, GetWhiteActualTimeLeft().ToString());
                SetDataEntry(wLastMoveDateKey, DateTime.UtcNow.ToString());
            }
        }
        public GameState GetGameState()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            return (GameState)int.Parse(gameData[gameStateKey]);
        }
        public Player GetWhitePlayer()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            return whitePlayer;
        }
        public Player GetBlackPlayer()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            return blackPlayer;
        }
        public DateTime GetGameStartDate()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            return DateTimeOffset.Parse(gameData[startDateKey]).UtcDateTime;
        }
        public DateTime GetWhiteLastMove()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            return DateTime.Parse(gameData[wLastMoveDateKey]);
        }
        public DateTime GetBlackLastMove()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            return DateTime.Parse(gameData[bLastMoveDateKey]);
        }
        public int GetWhiteTimeLeftLastMove()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            return int.Parse(gameData[wTimeLeftLastMoveKey]);
        }
        public int GetBlackTimeLeftLastMove()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            return int.Parse(gameData[bTimeLeftLastMoveKey]);
        }
        public int GetWhiteActualTimeLeft()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            DateTime blackLastMove = GetBlackLastMove();
            int secondsLeftLastMove = GetWhiteTimeLeftLastMove();
            if (GetGameState() == GameState.WhiteMove)
            {
                secondsLeftLastMove -= (int)(DateTime.UtcNow - blackLastMove).TotalSeconds;
            }
            return secondsLeftLastMove;
        }
        public int GetBlackActualTimeLeft()
        {
            if (!gameDataLoaded)
            {
                LoadGameData();
            }
            DateTime whiteLastMove = GetWhiteLastMove();
            int secondsLeftLastMove = GetBlackTimeLeftLastMove();
            if (GetGameState() == GameState.BlackMove)
            {
                secondsLeftLastMove -= (int)(DateTime.UtcNow - whiteLastMove).TotalSeconds;
            }
            return secondsLeftLastMove;
        }
        private void SetDataEntry(string key, string value)
        {
            if (gameDataLoaded)
            {
                gameData[key] = value;
            }
            redis.SetEntryInHash(gameDataKey, key, value);
        }

        public bool AreMovesLoaded()
        {
            return movesLoaded;
        }

        public bool IsGameDataLoaded()
        {
            return gameDataLoaded;
        }
    }
}
