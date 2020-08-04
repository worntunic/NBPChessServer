using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.Serialization;

namespace RedisData
{
    public struct PlayerGameInfo
    {
        public int gameID;
        public PlayerInfo opponent;
        public ChessGame.GameState gameState;
        public int currentTimeLeft;
    }
    public struct PlayerInfo
    {
        public int id;
        public string username;
        public int rank;

        public PlayerInfo(Player player)
        {
            this.id = player.ID;
            this.username = player.GetUsername();
            this.rank = player.GetRank();
        }
    }
    public class Player
    {
        private readonly RedisClient redis = new RedisClient(Config.SingleHost);
        //Keys
        private const string playerPrefix = "nbpc:player:";
        private const string playerDataSuffix = ":data";
        private const string activeGamesSuffix = ":games:active", finishedGamesSuffix = "games:finished";
        private const string usernameKey = "username", rankKey = "rank";
        private const string latestPlayerIDKey = "nbpc:ai:playerid";
        //Generated Keys
        private string playerKey, activeGamesKey, finishedGamesKey, playerDataKey;
        //Constant data
        private const int startingRank = 1400;
        //Loaded data flags
        private bool loadedPlayerData = false, loadedActiveGames = false, loadedFinishedGames = false;
        //Data
        public int ID { get; private set; }
        private string username;
        private int rank;
        private List<PlayerGameInfo> activeGames;
        private List<PlayerGameInfo> finishedGames;


        public Player()
        {

        }

        public Player(int playerID)
        {
            this.ID = playerID;
            GenerateKeys();
        }

        public void RegisterPlayer(string username)
        {
            ID = (int)redis.Incr(latestPlayerIDKey);
            GenerateKeys();
            SetUsername(username);
            SetRank(startingRank);
        }

        private void GenerateKeys()
        {
            playerKey = playerPrefix + ID;
            activeGamesKey = playerKey + activeGamesSuffix;
            finishedGamesKey = playerKey + finishedGamesSuffix;
            playerDataKey = playerKey + playerDataSuffix;
        }
        //Loading data
        private void LoadPlayerData()
        {
            loadedPlayerData = true;
            this.username = redis.GetValueFromHash(playerDataKey, usernameKey);
            int tmpRank;
            if (!Int32.TryParse(redis.GetValueFromHash(playerDataKey, rankKey), out tmpRank)) {
                SetRank(startingRank);
            } else
            {
                rank = tmpRank;
            }
        }

        public void LoadGames(bool loadActive = true, bool loadFinished = false)
        {
            if (loadActive)
            {
                loadedActiveGames = true;
                activeGames = redis.GetAllItemsFromList(activeGamesKey).Select(int.Parse).Select(ParseGameInfo).ToList();
            }
            if (loadFinished)
            {
                loadedFinishedGames = true;
                finishedGames = redis.GetAllItemsFromList(finishedGamesKey).Select(int.Parse).Select(ParseGameInfo).ToList();
            }
        }
        private PlayerGameInfo ParseGameInfo(int gameID)
        {
            ChessGame game = new ChessGame(gameID);
            PlayerGameInfo gameInfo = new PlayerGameInfo();
            gameInfo.gameID = game.ID;
            Player opponent;
            bool curPlayerWhite = false;
            opponent = game.GetWhitePlayer();
            if (opponent.ID == this.ID)
            {
                opponent = game.GetBlackPlayer();
                curPlayerWhite = true;
            }
            gameInfo.opponent = new PlayerInfo(opponent);
            gameInfo.gameState = game.GetGameState();
            gameInfo.currentTimeLeft = (curPlayerWhite) ? game.GetWhiteActualTimeLeft() : game.GetBlackActualTimeLeft();
            return gameInfo;
        }
        //Changing data
        public void SetUsername(string username)
        {
            if (String.IsNullOrEmpty(username))
            {
                throw new Exception("Username is empty");
            }
            this.username = username;
            redis.SetEntryInHash(playerDataKey, usernameKey, username);
            if (!loadedPlayerData)
            {
                LoadPlayerData();
            }
        }

        public void SetRank(int rank)
        {
            if (rank < 0)
            {
                throw new Exception("Rank cannot be negative");
            }
            this.rank = rank;
            redis.SetEntryInHash(playerDataKey, rankKey, rank.ToString());
            if (!loadedPlayerData)
            {
                LoadPlayerData();
            }
        }

        public void AddGame(int gameID)
        {
            redis.AddItemToList(activeGamesKey, gameID.ToString());
            if (!loadedActiveGames)
            {
                LoadGames(true, false);
            } else
            {
                activeGames.Add(ParseGameInfo(gameID));
            }
        }

        public void FinishGame(int gameID)
        {
            redis.AddItemToList(finishedGamesKey, gameID.ToString());
            if (!loadedFinishedGames)
            {
                LoadGames(false, true);
            } else
            {
                finishedGames.Add(ParseGameInfo(gameID));
            }
            redis.RemoveItemFromList(activeGamesKey, gameID.ToString());
            if (!loadedActiveGames)
            {
                LoadGames(true, false);
            } else
            {
                activeGames.RemoveAll(game => game.gameID == gameID);
            }
        }

        //Getting data
        public string GetUsername()
        {
            if (!loadedPlayerData)
            {
                LoadPlayerData();
            }
            return username;
        }
        public int GetRank()
        {
            if (!loadedPlayerData)
            {
                LoadPlayerData();
            }
            return rank;
        }
        public List<PlayerGameInfo> GetActiveGames()
        {
            if (!loadedActiveGames)
            {
                LoadGames(true, false);
            }
            return activeGames;
        }
        public List<PlayerGameInfo> GetFinishedGames()
        {
            if (!loadedFinishedGames)
            {
                LoadGames(false, true);
            }
            return finishedGames;
        }
        public bool IsDataLoaded()
        {
            return loadedPlayerData;
        }
        public bool AreActiveGamesLoaded()
        {
            return loadedActiveGames;
        }
        public bool AreFinishedGamesLoaded()
        {
            return loadedFinishedGames;
        }
    }
}
