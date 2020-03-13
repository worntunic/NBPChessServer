using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace RedisData
{
    public static class PlayerManager
    {
        private const string loginList = "nbpc:logindata:";
        private const string idKey = "id";
        private const string passwordKey = "password";
        public const int minUsernameLength = 3, minPasswordLength = 6;

        public static PlayerValidationResult LoginPlayer(string username, string password)
        {
            RedisClient redis = new RedisClient(Config.SingleHost);
            string passwordHash = GetPasswordHash(password);
            string loginListEntry = loginList + username;
            if (redis.Exists(loginListEntry) == 0)
            {
                return new PlayerValidationResult(ValidationStatus.PlayerNotFound);
            }
            int playerID = int.Parse(redis.GetValueFromHash(loginListEntry, idKey));
            string actualPasswordHash = redis.GetValueFromHash(loginListEntry, passwordKey);
            if (!string.Equals(passwordHash, actualPasswordHash))
            {
                return new PlayerValidationResult(ValidationStatus.InvalidPassword);
            }

            return new PlayerValidationResult(ValidationStatus.Valid, GetPlayer(playerID));
        }

        public static Player GetPlayer(int playerID)
        {
            Player player = new Player(playerID);
            return player;
        }

        public static PlayerValidationResult RegisterPlayer(string username, string password)
        {
            RedisClient redis = new RedisClient(Config.SingleHost);
            if (!IsUsernameValid(username))
            {
                return new PlayerValidationResult(ValidationStatus.UsernameTooShort);
            }
            if (!IsPasswordValid(password))
            {
                return new PlayerValidationResult(ValidationStatus.PasswordTooShort);
            }
            string passwordHash = GetPasswordHash(password);
            string loginListEntry = loginList + username;
            if (redis.Exists(loginListEntry) != 0)
            {
                return new PlayerValidationResult(ValidationStatus.PlayerAlreadyExists);
            }
            redis.SetEntryInHash(loginListEntry, passwordKey, passwordHash);
            Player player = new Player();
            player.RegisterPlayer(username);
            redis.SetEntryInHash(loginListEntry, idKey, player.ID.ToString());
            return new PlayerValidationResult(ValidationStatus.Valid, player);
        }

        private static string GetPasswordHash(string password)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(password);
            data = new System.Security.Cryptography.SHA256Managed().ComputeHash(data);
            return System.Text.Encoding.ASCII.GetString(data);
        }

        private static bool IsUsernameValid(string username)
        {
            return !String.IsNullOrEmpty(username) && username.Length >= minUsernameLength;
        }

        private static bool IsPasswordValid(string password)
        {
            return !String.IsNullOrEmpty(password) && password.Length >= 7;
        }
    }

    public enum ValidationStatus
    {
        PlayerNotFound, InvalidPassword, PlayerAlreadyExists, UsernameTooShort, PasswordTooShort, Valid 
    }
    public struct PlayerValidationResult
    {
        private static Dictionary<ValidationStatus, string> errorMessages = new Dictionary<ValidationStatus, string>()
        {
            { ValidationStatus.PlayerNotFound,
                $"Player not found!" },
            { ValidationStatus.InvalidPassword,
                $"Invalid password" },
            { ValidationStatus.PlayerAlreadyExists,
                $"Player with this username already exists" },
            { ValidationStatus.UsernameTooShort,
                $"This username is too short, it should be minimum {PlayerManager.minUsernameLength} characters long." },
            { ValidationStatus.UsernameTooShort,
                $"This password is too short, it should be minimum {PlayerManager.minPasswordLength} characters long." },
            { ValidationStatus.Valid, $"Valid player"}
        };
        public Player player;
        public ValidationStatus validationStatus;

        public PlayerValidationResult(ValidationStatus status)
        {
            validationStatus = status;
            player = null;
        }

        public PlayerValidationResult(ValidationStatus status, Player player)
        {
            validationStatus = status;
            this.player = player;
        }

        public string GetErrorMessage()
        {
            return errorMessages[validationStatus];
        }
    }
}
