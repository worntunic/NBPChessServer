using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NBPChessServer.DataManagers;
using Newtonsoft.Json.Linq;
using RedisData;

namespace NBPChessServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private const int SearchSleepTime = 1000, SearchTryNumber = 5;
        // POST: api/Game
        [Authorize]
        [HttpPost("Find")]
        public ActionResult FindGame()
        {
            ChessGameValidationResult validationResult = FindGameForContext(HttpContext);
            for (int i = 0; i < SearchTryNumber; ++i)
            {
                if (validationResult.status == ChessGameValidationStatus.Searching)
                {
                    System.Threading.Thread.Sleep(SearchSleepTime);
                    validationResult = FindGameForContext(HttpContext);
                } else
                {
                    break;
                }
            }
            GameResponseData responseData = GameResponseData.CreateResponseData(validationResult);
            return responseData.GetActionResult();
        }
        private ChessGameValidationResult FindGameForContext(HttpContext httpContext)
        {
            Player player = PlayerController.GetLoggedInPlayer(httpContext);
            ChessGameManager gameManager = new ChessGameManager(player);
            ChessGameValidationResult validationResult = gameManager.FindGame();
            return validationResult;
        }

        // POST: api/Game/AllInfo
        [Authorize]
        [HttpPost("AllInfo")]
        public ActionResult GetGameInfo([FromBody] JObject jsonData)
        {
            int gameID = int.Parse(jsonData["gameid"].ToString());
            ChessGame game = new ChessGame(gameID);
            GameResponseData gameResponse = GameResponseData.CreateFoundResponseData(game);
            return gameResponse.GetActionResult();
        }
        // POST: api/Game/AllInfo
        [Authorize]
        [HttpPost("PlayMove")]
        public ActionResult PlayMove([FromBody] JObject jsonData)
        {
            int gameID = int.Parse(jsonData["gameid"].ToString());
            string move = jsonData["move"].ToString();
            int newGameState = int.Parse(jsonData["gamestate"].ToString());
            Player player = PlayerController.GetLoggedInPlayer(HttpContext);
            ChessGame game = new ChessGame(gameID);
            game.PlayMove(player.ID, move, newGameState);
            GameResponseData gameResponse = GameResponseData.CreateFoundResponseData(game);
            return gameResponse.GetActionResult();
        }
        [Authorize]
        [HttpPost("WaitForGameState")]
        public ActionResult WaitForGameState([FromBody] JObject jsonData)
        {
            int gameID = int.Parse(jsonData["gameid"].ToString());
            Player player = PlayerController.GetLoggedInPlayer(HttpContext);

            ChessGame game = new ChessGame(gameID);
            ChessGame.GameState existingState;
            if (player.ID == game.GetWhitePlayer().ID )
            {
                existingState = ChessGame.GameState.BlackMove;
            } else if (player.ID == game.GetBlackPlayer().ID)
            {
                existingState = ChessGame.GameState.WhiteMove;
            } else
            {
                ResponseData responseData = new ResponseData(400, "Invalid game for player");
                return responseData.GetActionResult();
            }
            bool stateChanged = false;
            for (int i = 0; i < SearchTryNumber; ++i)
            {
                if (game.GetGameState() == existingState)
                {
                    System.Threading.Thread.Sleep(SearchSleepTime);
                    game.ReloadGameData();
                }
                else
                {
                    //game.ReloadMoves();
                    stateChanged = true;
                    break;
                }
            }
            game.ReloadMoves();
            GameResponseData gameResponse = GameResponseData.CreateFoundResponseData(game);
            return gameResponse.GetActionResult();
        }



    }
}
