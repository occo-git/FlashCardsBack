using Application.DTO.Users;
using Domain.Constants;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
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
                Level = user.Level
            };
        }

        public static User ToDomain(RegisterRequestDto dto)
        {
            return new User
            {
                UserName = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = dto.Email,
                Level = Levels.A1,
                LastActive = DateTime.UtcNow,
            };
        }

        public static bool CheckPassword(User user, LoginRequestDto loginUserDto)
        {
            return BCrypt.Net.BCrypt.Verify(loginUserDto.Password, user.PasswordHash);
        }
    }
}
