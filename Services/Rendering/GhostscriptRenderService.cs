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
        public async Task<List<string>> RenderAsync(string pdfPath, int dpi = 300)
        {
            var baseDir = @"C:\Users\jeliwag\Downloads\OCR Test Data\results"; // custom folder
            var outputDir = Path.Combine(baseDir, Guid.NewGuid().ToString());
            Directory.CreateDirectory(outputDir);

            var outputPattern = Path.Combine(outputDir, "page-%03d.png");

            var args = $"-dNOPAUSE -dBATCH -sDEVICE=png16m -r{dpi} " +
                       $"-sOutputFile=\"{outputPattern}\" \"{pdfPath}\"";

            var psi = new ProcessStartInfo("gswin64c.exe", args)
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            await process.WaitForExitAsync();

            return Directory.GetFiles(outputDir, "page-*.png").ToList();
        }
    }
}
