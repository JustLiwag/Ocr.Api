using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Ocr.Api.Services.FileStorage
{
    public interface ITempFileService
    {
        Task<string> SaveFileAsync(IFormFile file);
        string CreateJobDirectory(string? jobName = null);
        Task<string> SaveFileAsync(IFormFile file, string targetDirectory);
        void DeleteFileIfExists(string? filePath);
        void DeleteDirectoryIfExists(string? directoryPath, bool recursive = true);
    }
}