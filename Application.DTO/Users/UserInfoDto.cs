using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTO.Users
{
    public record UserInfoDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email {  get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Provider {  get; set; } = string.Empty;
    }
}