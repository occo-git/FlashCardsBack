using Application.DTO.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.ContractTests.ProviderState
{
    public interface IProviderStateHandler
    {
        DbHelper DbHelper { get; }  
        HttpHelper HttpHelper { get; }
        Task HandleProviderStateAsync(string state);
    }

    public class ProviderStateHandler : IProviderStateHandler
    {
        private readonly DbHelper _dbHelper;
        private readonly HttpHelper _httpHelper;

        public ProviderStateHandler(DbHelper dbHelper, HttpHelper httpHelper)
        {
            _dbHelper = dbHelper;
            _httpHelper = httpHelper;
        }

        public DbHelper DbHelper => _dbHelper;
        public HttpHelper HttpHelper => _httpHelper;

        public async Task HandleProviderStateAsync(string state)
        {
            Console.WriteLine("---------------------> HandleProviderStateAsync");
            switch (state)
            {
                case ProviderStates.UserIsAuthenticated:
                    Console.WriteLine("---------------------> Setting provider state: User is authenticated");
                    await AuthorizeAsync();
                    break;
                default:
                    throw new InvalidOperationException($"⚠️ Unknown provider state: {state}");
            }
        }

        protected async Task<TokenResponseDto> AuthorizeAsync(string username = "testuser", string email = "test@test.com", string password = "strongpass123!")
        {
            await _dbHelper.AddConfirmedUserAsync(username, email, password);
            return await _httpHelper.LoginAsync(username, password);
        }
    }
}
