using ServiceStack.Redis;
using System;
using System.Collections.Generic;

namespace RedisData
{
    public class ChessGame
    {
        readonly RedisClient redis = new RedisClient(Config.SingleHost);
        //Keys
        private const string latestGameIDKey = "nbpc:ai:game";
        private const string keyPrefix = "nbpc:game:";
        private const string movesSuffix = ":moves";
        private const string gameDataSuffix = ":data";
        private const string wPlayerKey = "wplayer", bPlayerKey = "bplayer";
        private const string wPlayerTimeKey = "wtime", bPlayerTimeKey = "btime";
        private const string gameStateKey = "state";
        private const string startingTimeKey = "startingtime";
        //Generated Keys
        private string gameKey, movesKey, gameDataKey;

        public List<AlgebraicMove> moves;
        public Player player; 


        public ChessGame()
        {
            int gameID = (int)redis.Incr(latestGameIDKey);
            GenerateKeys(gameID);
            CreateGame();
        }

        public ChessGame(int gameID)
        {
            GenerateKeys(gameID);
            //LoadGame();
        }
        private void GenerateKeys(int gameID)
        {
            gameKey = keyPrefix + gameID;
            movesKey = gameKey + movesSuffix;
        }
        private void CreateGame()
        {

        }

        private void LoadGame()
        {
            List<string> moveList = redis.GetAllItemsFromList(movesKey);
            for (int i = 0; i < moveList.Count; i++)
            {
                AlgebraicMove move = new AlgebraicMove();
                move.move = moveList[i];
            }

        }

        public void RegisterPlayerRandomColor(int playerID)
        {
            Random rand = new Random();
            if (rand.Next() % 2 == 0)
            {
                //this.w = playerID;
            }
        }

        public void PrintMove(string move)
        {
            Console.WriteLine(move);
        }

        public void RegisterMove(int gameID, int playerID, string move)
        {
            AlgebraicMove algMove = new AlgebraicMove();
            algMove.move = move;

            redis.Add<AlgebraicMove>($"nbpC:games:{gameID}", algMove);
        }

        public AlgebraicMove GetMove(int gameID)
        {
            return null;
            //return redis.GetAll<AlgebraicMove>($"nbpC:games:{gameID}");
        }
    }
}
