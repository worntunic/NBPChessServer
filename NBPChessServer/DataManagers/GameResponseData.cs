using RedisData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NBPChessServer.DataManagers
{
    public class GameResponseData : ResponseData
    {
        private const string whitePlayerKey = "wplayer", blackPlayerKey = "bplayer";
        private const string gameIDKey = "id";
        private const string wTimeLeftKey = "wtimeleft", bTimeLeftKey = "btimeleft";
        private const string gameStateKey = "gamestate";

        private const string gameKey = "game", gameDataKey = "gamedata", movesKey = "moves", gameFoundKey = "gamefound";
        private Dictionary<string, object> keyValueData = new Dictionary<string, object>();

        private const string foundMessage = "Success: Game Found", searchingMessage = "Still searching for a game";

        public GameResponseData(int code, string message, Object data = null) : base(code, message)
        {
            if (data != null)
            {
                keyValueData.Add(gameKey, GetGameData((ChessGame)data));
            }
        }

        public GameResponseData(int code, string message, ChessGame game) : base(code, message)
        {
            keyValueData.Add(gameKey, GetGameData(game));
        }

        public static GameResponseData CreateFoundResponseData(ChessGame game)
        {
            GameResponseData response = new GameResponseData(200, foundMessage, game);
            response.keyValueData.Add(gameFoundKey, true);
            return response;
        }

        public static GameResponseData CreateResponseData(ChessGameValidationResult validationResult)
        {
            GameResponseData response;
            if (validationResult.status == ChessGameValidationStatus.Searching)
            {
                response = new GameResponseData(200, searchingMessage);
                response.keyValueData.Add(gameFoundKey, false);
            }
            else
            {
                response = new GameResponseData(200, foundMessage, validationResult.game);
                response.keyValueData.Add(gameFoundKey, true);
            }
            return response;
        }

        private Dictionary<string, object> GetGameData(ChessGame game)
        {
            Dictionary<string, object> allGameData = new Dictionary<string, object>();


            allGameData.Add(gameIDKey, game.ID);
            if (game.AreMovesLoaded())
            {
                allGameData.Add(movesKey, game.GetMovesAsString());
            }
            if (game.IsGameDataLoaded())
            {
                Dictionary<string, object> gameInfoData = new Dictionary<string, object>();
                gameInfoData.Add(whitePlayerKey, game.GetWhitePlayer().ID);
                gameInfoData.Add(blackPlayerKey, game.GetBlackPlayer().ID);
                gameInfoData.Add(wTimeLeftKey, game.GetWhiteActualTimeLeft());
                gameInfoData.Add(bTimeLeftKey, game.GetBlackActualTimeLeft());
                gameInfoData.Add(gameStateKey, ((int)game.GetGameState()));
                allGameData.Add(gameDataKey, gameInfoData);
            }
            return allGameData;
        }

        protected override void PrepareData()
        {
            if (generalForm.ContainsKey(dataKey))
            {
                generalForm[dataKey] = keyValueData;
            }
            else
            {
                generalForm.Add(dataKey, keyValueData);
            }
        }
    }

}
