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
        public ActionResult FindGame([FromBody] JObject jsonData)
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



    }
}
