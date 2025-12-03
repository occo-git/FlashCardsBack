using Application.Abstractions.Caching;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Caching
{
    public class RedisInfoService : IRedisInfoService
    {
        private readonly IConnectionMultiplexer _multiplexer;

        public RedisInfoService(IConnectionMultiplexer multiplexer)
        {
            ArgumentNullException.ThrowIfNull(multiplexer, nameof(multiplexer));

            _multiplexer = multiplexer;
        }

        public async Task<string> GetTotalMemoryUsageAsync()
        {
            var server = _multiplexer.GetServer(_multiplexer.GetEndPoints().First());
            var result = await server.ExecuteAsync("INFO", "MEMORY");
            return result.ToString();
        }

        public async Task<string> GetKeySizeAsync(string key)
        {
            var db = _multiplexer.GetDatabase();
            var result = await db.ExecuteAsync("MEMORY", "USAGE", key);
            return result.ToString();
        }

        public async Task<long> GetDatabaseSizeAsync()
        {
            var server = _multiplexer.GetServer(_multiplexer.GetEndPoints().First());
            var dbsize = await server.DatabaseSizeAsync();
            return dbsize;
        }
    }
}
