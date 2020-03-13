using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisData
{
    public class ChessGameManager
    {
        readonly RedisClient redis = new RedisClient(Config.SingleHost);

        private const int WaitCycleTime = 2;
        private const int RankScopeExpandPerCycle = 50;
        public const int StartingRankScope = 20;
        private const int GameNotFoundID = -1;
        private const int GameTimeInSeconds = 24 * 60 * 60;

        private const string gameQueuePrefix = "nbpc:gamequeue:";
        private const string sortedRanksKey = gameQueuePrefix + "sortedranks";
        private const string playerInfoPrefix = gameQueuePrefix + "players:";
        private const string playerRankScopeKey = "rankscope", gameFoundIDKey = "gameID";

        private int currentRankScope;
        private Player player;
        private string playerInfoKey;

        public ChessGameManager(Player player)
        {
            currentRankScope = StartingRankScope;
            this.player = player;
            playerInfoKey = playerInfoPrefix + player.ID;
        }

        public ChessGameValidationResult FindGame()
        {
            if (redis.Exists(playerInfoKey) == 0)
            {
                CreateGameQueueEntry();
            } else
            {
                UpdateRankScope();
            }
            int gameAlreadyFoundID = int.Parse(redis.GetValueFromHash(playerInfoKey, gameFoundIDKey));
            if (gameAlreadyFoundID != -1)
            {
                DeleteEntriesForPlayer();
                ChessGame game = new ChessGame(gameAlreadyFoundID);
                return new ChessGameValidationResult(ChessGameValidationStatus.Found, game);
            }
            int oppID;
            if (FindClosestPlayerInRange(out oppID))
            {
                ChessGame game = CreateGame(oppID);
                DeleteEntriesForPlayer();
                return new ChessGameValidationResult(ChessGameValidationStatus.Found, game);
            } else
            {
                return new ChessGameValidationResult(ChessGameValidationStatus.Searching);
            }
        }

        private bool FindClosestPlayerInRange(out int playerID)
        {
            int minRank = player.GetRank() - currentRankScope;
            int maxRank = player.GetRank() + currentRankScope;
            IDictionary<string, double> playersInRange = redis.GetRangeWithScoresFromSortedSetByHighestScore(sortedRanksKey, minRank, maxRank);
            int closestMatch = -1;
            int closestMatchDelta = int.MaxValue;
            foreach (KeyValuePair<string, double> playerIDRank in playersInRange)
            {
                int opponentRank = (int)playerIDRank.Value;
                int opponentID = int.Parse(playerIDRank.Key);
                if (opponentID != player.ID)
                {
                    string opponentInfoKey = playerInfoPrefix + opponentID.ToString();
                    int opponentRankScope = int.Parse(redis.GetValueFromHash(opponentInfoKey, playerRankScopeKey));
                    int opponentGame = int.Parse(redis.GetValueFromHash(opponentInfoKey, gameFoundIDKey));
                    if (opponentGame == -1)
                    {
                        int delta = Math.Abs(player.GetRank() - opponentRank);
                        if (delta < opponentRankScope && delta < closestMatchDelta)
                        {
                            closestMatch = opponentID;
                            closestMatchDelta = delta;
                        }
                    }
                }
            }
            playerID = closestMatch;
            return closestMatch != -1;
        } 

        private void CreateGameQueueEntry()
        {
            redis.AddItemToSortedSet(sortedRanksKey, player.ID.ToString(), player.GetRank());
            redis.SetEntryInHash(playerInfoKey, playerRankScopeKey, currentRankScope.ToString());
            redis.SetEntryInHash(playerInfoKey, gameFoundIDKey, GameNotFoundID.ToString());
        }

        private ChessGame CreateGame(int oppID)
        {
            Random random = new Random();
            int whitePlayerID, blackPlayerID;
            string opponentInfoKey = playerInfoPrefix + oppID.ToString();

            if (random.Next() % 2 == 0)
            {
                whitePlayerID = player.ID;
                blackPlayerID = oppID;
            } else
            {
                whitePlayerID = oppID;
                blackPlayerID = player.ID;
            }

            ChessGame chessGame = new ChessGame();
            chessGame.CreateGame(whitePlayerID, blackPlayerID, GameTimeInSeconds);
            //Set Game ID for opponent
            redis.SetEntryInHash(opponentInfoKey, gameFoundIDKey, chessGame.ID.ToString());
            return chessGame;
        }

        private void DeleteEntriesForPlayer()
        {
            redis.RemoveItemFromSortedSet(sortedRanksKey, player.ID.ToString());
            redis.Remove(playerInfoKey);
        }

        private int GetNewRankScope()
        {
            return currentRankScope + RankScopeExpandPerCycle;
        }

        private void UpdateRankScope()
        {
            currentRankScope = int.Parse(redis.GetValueFromHash(playerInfoKey, playerRankScopeKey));
            currentRankScope = GetNewRankScope();
            redis.SetEntryInHash(playerInfoKey, playerRankScopeKey, currentRankScope.ToString());
        }

    }

    public enum ChessGameValidationStatus
    {
        Searching, Found
    }

    public class ChessGameValidationResult
    {
        public ChessGameValidationStatus status;
        public int newSearchScope;
        public ChessGame game;

        public ChessGameValidationResult(ChessGameValidationStatus status, ChessGame game = null)
        {
            this.status = status;
            this.newSearchScope = ChessGameManager.StartingRankScope;
            this.game = game;
        }
    }
}
