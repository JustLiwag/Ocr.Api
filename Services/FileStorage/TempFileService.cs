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
            var jobDirectory = CreateJobDirectory();
            return await SaveFileAsync(file, jobDirectory);
        }

        public async Task<string> SaveFileAsync(IFormFile file, string targetDirectory)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file", nameof(file));

            if (string.IsNullOrWhiteSpace(targetDirectory))
                throw new ArgumentException("Target directory is required.", nameof(targetDirectory));

            Directory.CreateDirectory(targetDirectory);

            var safeExtension = Path.GetExtension(file.FileName);
            var filePath = Path.Combine(targetDirectory, $"{Guid.NewGuid()}{safeExtension}");

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(fs);

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Temporary file was not created.", filePath);

            return filePath;
        }

        public string CreateJobDirectory(string? jobName = null)
        {
            var safeJobName = string.IsNullOrWhiteSpace(jobName)
                ? Guid.NewGuid().ToString()
                : string.Concat(jobName.Split(Path.GetInvalidFileNameChars()));

            var jobDirectory = Path.Combine(_tempRoot, $"{safeJobName}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(jobDirectory);

            return jobDirectory;
        }

        public void DeleteFileIfExists(string? filePath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
            }
        }

        public void DeleteDirectoryIfExists(string? directoryPath, bool recursive = true)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
                    Directory.Delete(directoryPath, recursive);
            }
            catch
            {
            }
        }
    }
}