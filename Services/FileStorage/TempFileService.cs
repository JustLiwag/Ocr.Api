using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ocr.Api.Services.FileStorage
{
    public class TempFileService : ITempFileService
    {
        private readonly string _tempRoot;

        public TempFileService(IConfiguration config)
        {
            _tempRoot = config.GetValue<string>("OcrSettings:TempRoot")
                        ?? Path.Combine(Path.GetTempPath(), "ocr-api");

            Directory.CreateDirectory(_tempRoot);
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            var filePath = Path.Combine(
                _tempRoot,
                $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}"
            );

            using var fs = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fs);

            return filePath;
        }
    }
}
