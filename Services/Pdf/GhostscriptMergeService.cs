namespace Ocr.Api.Services.Pdf
{
    using System.Diagnostics;

    public class GhostscriptMergeService : IPdfMergeService
    {
        public async Task<string> MergeAsync(IEnumerable<string> pdfPaths)
        {
            var baseDir = @"C:\Users\jeliwag\Downloads\OCR Test Data\results"; // custom folder
            var output = Path.Combine(baseDir, $"ocr_{Guid.NewGuid()}.pdf");
            var inputs = string.Join(" ", pdfPaths.Select(p => $"\"{p}\""));

            var args = $"-dBATCH -dNOPAUSE -q -sDEVICE=pdfwrite -sOutputFile=\"{output}\" {inputs}";

            var psi = new ProcessStartInfo("gswin64c", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            await process.WaitForExitAsync();

            return output;
        }
    }
}
