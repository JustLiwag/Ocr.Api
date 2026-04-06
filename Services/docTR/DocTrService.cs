using Microsoft.Extensions.Configuration;
using Ocr.Api.Models;
using System.Diagnostics;
using System.Text.Json;

namespace Ocr.Api.Services.Ocr
{
    public class DocTrService : IDocTrService
    {
        private readonly string _pythonPath;
        private readonly string _runnerPath;

        public DocTrService(IConfiguration config)
        {
            _pythonPath = config["DocTr:PythonPath"]
                ?? throw new InvalidOperationException("DocTr:PythonPath is not configured.");

            _runnerPath = config["DocTr:RunnerPath"]
                ?? throw new InvalidOperationException("DocTr:RunnerPath is not configured.");
        }

        public async Task<DocTrOcrResult> RunOcrAsync(string imagePath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("docTR input image not found.", imagePath);

            string outputJsonPath = Path.Combine(
                Path.GetDirectoryName(imagePath)!,
                Path.GetFileNameWithoutExtension(imagePath) + "_doctr.json"
            );

            var args = $"\"{_runnerPath}\" \"{imagePath}\" \"{outputJsonPath}\"";

            var psi = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_runnerPath)!
            };

            using var process = Process.Start(psi)!;

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"docTR failed.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");

            if (!File.Exists(outputJsonPath))
                throw new FileNotFoundException("docTR output JSON was not created.", outputJsonPath);

            var json = await File.ReadAllTextAsync(outputJsonPath);

            var result = JsonSerializer.Deserialize<DocTrOcrResult>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
                throw new Exception("Failed to deserialize docTR OCR result.");

            return result;
        }
    }
}