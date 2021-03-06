﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using RedisData;
using NBPChessServer.DataManagers;

namespace NBPChessServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private IConfiguration _config;
        private const double tokenDurationInSeconds = 60 * 60 * 24;
        private const string playerIDClaimKey = "NBPCPLAYERID";

        public PlayerController(IConfiguration config)
        {
            this._config = config;
        }
        // GET: api/Player
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Player/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }
        // POST: api/Player/Register
        [AllowAnonymous]
        [HttpPost("Register")]
        public ActionResult Register([FromBody] JObject data)
        {
            string username = data["username"].ToString();
            string password = data["password"].ToString();
            PlayerValidationResult result =  PlayerManager.RegisterPlayer(username, password);
            string jwtToken = null;
            if (result.validationStatus == ValidationStatus.Valid)
            {
                jwtToken = GenerateJSONWebToken(result.player.ID, result.player.GetUsername());
            }
            return PlayerResponseData.CreateResponseData(result, "Registration successful", jwtToken).GetActionResult();
        }
        // POST: api/Player/Login
        [AllowAnonymous]
        [HttpPost("Login")]
        public ActionResult Login([FromBody] JObject data)
        {
            string username = data["username"].ToString();
            string password = data["password"].ToString();
            
            PlayerValidationResult result = PlayerManager.LoginPlayer(username, password);
            string jwtToken = null;
            if (result.validationStatus == ValidationStatus.Valid)
            {
                jwtToken = GenerateJSONWebToken(result.player.ID, result.player.GetUsername());
            }
            return PlayerResponseData.CreateResponseData(result, "Login successful", jwtToken).GetActionResult();
        }

        [HttpPost("ActiveGames")]
        [Authorize]
        public ActionResult ActiveGames()
        {
            Player player = GetLoggedInPlayer(HttpContext);
            player.LoadGames(true, false);
            player.GetRank();

            return PlayerResponseData.CreateResponseData(player, "Active Games Fetched Successfuly").GetActionResult();
        }

        [HttpPost("FinishedGames")]
        [Authorize]
        public ActionResult FinishedGames()
        {
            Player player = GetLoggedInPlayer(HttpContext);
            player.LoadGames(false, true);

            return PlayerResponseData.CreateResponseData(player, "Finished Games Fetched Successfuly").GetActionResult();
        }

        [HttpPost("AllData")]
        [Authorize]
        public ActionResult AllData()
        {
            Player player = GetLoggedInPlayer(HttpContext);
            player.GetUsername();
            player.LoadGames(true, true);

            return PlayerResponseData.CreateResponseData(player, "All Data Loaded Successfuly").GetActionResult();
        }

        private string GenerateJSONWebToken(int playerID, string username)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[] {
                new Claim(playerIDClaimKey, playerID.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Issuer"],
              claims,
              expires: DateTime.Now.AddSeconds(tokenDurationInSeconds),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static Player GetLoggedInPlayer(HttpContext httpContext)
        {
            var currentUser = httpContext.User;
            Claim claim = currentUser.Claims.FirstOrDefault(c => c.Type == playerIDClaimKey);
            if (claim == null)
            {
                throw new Exception("User doesn't have ID claim");
            }
            int playerID = int.Parse(claim.Value);
            Player player = PlayerManager.GetPlayer(playerID);
            return player;
        }
    }
}
