using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Rendering
{
    public class GhostscriptRenderService : IPdfRenderService
    {
        private async Task RunGhostscriptAsync(string args)
        {
            var psi = new ProcessStartInfo("gswin64c.exe", args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"Ghostscript render failed.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }

        public async Task<List<string>> RenderAsync(string pdfPath, string baseDir, int dpi = 300)
        {
            var outputDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            Directory.CreateDirectory(outputDir);

            var outputPattern = Path.Combine(outputDir, "page-%03d.png");

            var args =
                $"-dNOPAUSE -dBATCH " +
                $"-sDEVICE=png16m " +
                $"-r{dpi} " +
                $"-sOutputFile=\"{outputPattern}\" " +
                $"\"{pdfPath}\"";

            await RunGhostscriptAsync(args);

            return Directory.GetFiles(outputDir, "page-*.png")
                .OrderBy(x => x)
                .ToList();
        }

        public async Task<int> GetPageCountAsync(string pdfPath)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ocr_pagecount", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var outputPattern = Path.Combine(tempDir, "page-%03d.png");

                var args =
                    $"-dNOPAUSE -dBATCH " +
                    $"-sDEVICE=png16m " +
                    $"-r10 " +
                    $"-sOutputFile=\"{outputPattern}\" " +
                    $"\"{pdfPath}\"";

                await RunGhostscriptAsync(args);

                return Directory.GetFiles(tempDir, "page-*.png").Length;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }
        }

        public async Task<string> RenderPageAsync(
            string pdfPath,
            string baseDir,
            int pageNumber,
            int dpi = 300)
        {
            if (pageNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than zero.");

            var outputDir = Path.Combine(baseDir, "rendered_pages");
            Directory.CreateDirectory(outputDir);

            var outputPath = Path.Combine(outputDir, $"page-{pageNumber:000}.png");

            var args =
                $"-dNOPAUSE -dBATCH " +
                $"-sDEVICE=png16m " +
                $"-r{dpi} " +
                $"-dFirstPage={pageNumber} " +
                $"-dLastPage={pageNumber} " +
                $"-sOutputFile=\"{outputPath}\" " +
                $"\"{pdfPath}\"";

            await RunGhostscriptAsync(args);

            if (!File.Exists(outputPath))
                throw new FileNotFoundException("Rendered page image was not created.", outputPath);

            return outputPath;
        }

        public async Task<List<string>> RenderPagesAsync(
            string pdfPath,
            string baseDir,
            int startPage,
            int endPage,
            int dpi = 300)
        {
            if (startPage <= 0)
                throw new ArgumentOutOfRangeException(nameof(startPage), "Start page must be greater than zero.");

            if (endPage < startPage)
                throw new ArgumentOutOfRangeException(nameof(endPage), "End page must be greater than or equal to start page.");

            var outputDir = Path.Combine(baseDir, $"pages-{startPage:000}-{endPage:000}");
            Directory.CreateDirectory(outputDir);

            var outputPattern = Path.Combine(outputDir, "page-%03d.png");

            var args =
                $"-dNOPAUSE -dBATCH " +
                $"-sDEVICE=png16m " +
                $"-r{dpi} " +
                $"-dFirstPage={startPage} " +
                $"-dLastPage={endPage} " +
                $"-sOutputFile=\"{outputPattern}\" " +
                $"\"{pdfPath}\"";

            await RunGhostscriptAsync(args);

            return Directory.GetFiles(outputDir, "page-*.png")
                .OrderBy(x => x)
                .ToList();
        }
    }
}