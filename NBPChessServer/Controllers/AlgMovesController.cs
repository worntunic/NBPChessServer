using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
using RedisData;
namespace NBPChessServer.Controllers
{
    public class AlgMovesController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        // GET api/values/5
        [HttpGet]
        [Route("api/AlgMoves/Add")]
        public ActionResult<string> Add([FromQuery]int id, [FromQuery]int plr, [FromQuery]string move, [FromQuery]int clr)
        {
            PieceColor color = (PieceColor)clr;
            ChessGame chessMove = new ChessGame();
            chessMove.RegisterMove(id, plr, move);
            return chessMove.moves[0].move;
        }
    }
}
