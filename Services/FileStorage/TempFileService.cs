using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace Ocr.Api.Services.FileStorage
{
    // Single interface for temp file handling
    public interface ITempFileService
    {
        /// <summary>
        /// Saves the uploaded file to a temporary folder and returns the full file path.
        /// </summary>
        /// <param name="file">Uploaded file from client</param>
        /// <returns>Full path to saved temp file</returns>
        Task<string> SaveFileAsync(IFormFile file);
    }

    // Concrete implementation
    public class TempFileService : ITempFileService
    {
        private readonly string _tempRoot;

        public TempFileService(IConfiguration config)
        {
            // Read folder from config or use system temp path
            _tempRoot = config.GetValue<string>("OcrSettings:TempRoot") ?? Path.GetTempPath();
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty", nameof(file));

            // Ensure temp folder exists
            Directory.CreateDirectory(_tempRoot);

            // Generate unique file name
            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(_tempRoot, fileName);

            // Write file to disk safely
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }
    }
}
