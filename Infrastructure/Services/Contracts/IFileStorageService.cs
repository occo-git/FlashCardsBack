using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Contracts
{
    public interface IFileStorageService
    {
        Task<Stream> GetFileStreamAsync(string fileId);
        string GetFilePathAsync(string fileId);
    }
}
