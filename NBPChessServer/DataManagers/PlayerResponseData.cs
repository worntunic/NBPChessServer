using RedisData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NBPChessServer.DataManagers
{
    public class PlayerResponseData : ResponseData
    {
        private const string usernameKey = "username", rankKey = "rank", idKey = "id";
        private const string activeGamesKey = "activeGames", finishedGamesKey = "finishedGames";
        private Dictionary<string, object> keyValueData = new Dictionary<string, object>();
        private const string playerKey = "player", tokenKey = "token";

        public PlayerResponseData(int code, string message, Object data = null) : base(code, message)
        {
            if (data != null)
            {
                keyValueData.Add(playerKey, GetPlayerData((Player)data));
            }
        }

        public PlayerResponseData(int code, string message, Player player) : base(code, message)
        {
            keyValueData.Add(playerKey, GetPlayerData(player));
        }

        public PlayerResponseData(int code, string message, Player player, string token) : base(code, message)
        {
            keyValueData.Add(playerKey, GetPlayerData(player));
            keyValueData.Add(tokenKey, token);
        }

        public static PlayerResponseData CreateResponseData(Player player, string validMessage)
        {
            return new PlayerResponseData(200, validMessage, player);
        }

        public static PlayerResponseData CreateResponseData(PlayerValidationResult validationResult, string validMessage, string token = null)
        {

            if (validationResult.validationStatus != ValidationStatus.Valid)
            {
                return new PlayerResponseData(400, validationResult.GetErrorMessage(), null);
            }
            else
            {
                if (string.IsNullOrEmpty(token))
                {
                    return new PlayerResponseData(200, validMessage, validationResult.player);
                }
                else
                {
                    return new PlayerResponseData(200, validMessage, validationResult.player, token);
                }
            }
        }

        private Dictionary<string, object> GetPlayerData(Player player)
        {
            Dictionary<string, object> playerData = new Dictionary<string, object>();
            playerData.Add(idKey, player.ID);
            if (player.IsDataLoaded())
            {
                playerData.Add(usernameKey, player.GetUsername());
                playerData.Add(rankKey, player.GetRank());
            }
            if (player.AreActiveGamesLoaded())
            {
                playerData.Add(activeGamesKey, player.GetActiveGames());
            }
            if (player.AreFinishedGamesLoaded())
            {
                playerData.Add(finishedGamesKey, player.GetFinishedGames());
            }
            return playerData;
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
