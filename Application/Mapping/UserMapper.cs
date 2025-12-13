using Application.DTO.Tokens;
using Application.DTO.Users;
using Domain.Constants;
using Domain.Entities;
using Shared.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public static class UserMapper
    {
        public static UserInfoDto ToDto(User user)
        {
            return new UserInfoDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Level = user.Level,
                Provider = user.Provider,
            };
        }

        public static User ToDomain(RegisterRequestDto dto, string passwordHash)
        {
            return new User
            {
                UserName = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Level = Levels.A1,
                Provider = Providers.ProviderLocal,
                LastActive = DateTime.UtcNow,
            };
        }

        public static string GenerateRandomPassword(int length = 32)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            random.GetBytes(bytes);
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }
}