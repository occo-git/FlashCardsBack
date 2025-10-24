using Application.DTO.User;
using Domain.Entities;
using Shared;
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
        public static UserInfoDto ToDto(User user, string? accessToken = null, string? refreshToken = null)
        {
            return new UserInfoDto
            {
                Id = user.Id,
                Username = user.Username,
                AccessToken = accessToken ?? string.Empty,
                RefreshToken = refreshToken ?? string.Empty,
            };
        }

        public static User ToDomain(CreateUserDto dto)
        {
            return new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = dto.Email,
                Level = Levels.A1,
                LastActive = DateTime.UtcNow,
            };
        }

        public static bool CheckPassword(User user, LoginUserDto loginUserDto)
        {
            return BCrypt.Net.BCrypt.Verify(loginUserDto.Password, user.PasswordHash);
        }
    }
}
