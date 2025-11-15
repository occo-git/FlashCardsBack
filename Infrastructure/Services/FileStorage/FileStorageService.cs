using Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.FileStorage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _storagePath;

        public FileStorageService(IConfiguration config)
        {
            _storagePath = config[SharedConstants.EnvFileStoragePath] ?? Path.Combine("/storage", "images");
            Directory.CreateDirectory(_storagePath);
        }

        public async Task<Stream> GetFileStreamAsync(string fileId)
        {
            var filePath = Path.Combine(_storagePath, $"card_{fileId}.jpg");
            Console.WriteLine($"Attempting to access file at: {filePath}");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);
            
            // Асинхронное открытие файла с управлением
            return await Task.FromResult(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous));
        }

        public string GetFilePathAsync(string fileId)
        {
            var filePath = Path.Combine(_storagePath, $"card_{fileId}.jpg");
            Console.WriteLine($"Attempting to access file at: {filePath}");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);
            
            return filePath;
        }
    }
}
