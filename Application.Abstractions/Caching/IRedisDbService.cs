using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Caching
{
    public  interface IRedisDbService
    {
        Task<string> GetMemoryInfoAsync();
        Task<string> GetKeySizeAsync(string key);
        Task<long> GetDatabaseSizeAsync();
        Task FlushDb();
        Task FlushAll();
    }
}