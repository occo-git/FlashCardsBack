using Application.DTO.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LoadTests
{
    public static class ApiRequests
    {
        public static string AuthToken = "http://localhost:8080/api/auth/token";

        public static string UsersMe = "http://localhost:8080/api/users/me";
        public static string UsersLevel = "http://localhost:8080/api/users/level";
        public static string UsersProgress = "http://localhost:8080/api/users/progress";
        public static string UsersProgressSave = "http://localhost:8080/api/users/progress/save";

        public static string CardsCardFromDeck = "http://localhost:8080/api/cards/card-from-deck";
        public static string CardsList = "http://localhost:8080/api/cards/list";
    }
}
