namespace Ocr.Api.Services.Pdf
{
    using System.Diagnostics;

    public class GhostscriptMergeService : IPdfMergeService
    {
        public async Task<string> MergeAsync(
            IEnumerable<string> pdfPaths,
            string baseDir,
            string outputFileName)
        {
            // Ensure output directory exists
            Directory.CreateDirectory(baseDir);

            // Final PDF path: folderName/folderName.pdf
            var output = Path.Combine(
                baseDir,
                $"{outputFileName}.pdf"
            );

            var inputs = string.Join(
                " ",
                pdfPaths.Select(p => $"\"{p}\"")
            );

            var args =
                $"-dBATCH -dNOPAUSE -q " +
                $"-sDEVICE=pdfwrite " +
                $"-sOutputFile=\"{output}\" {inputs}";

            var psi = new ProcessStartInfo("gswin64c", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi)!;
            await process.WaitForExitAsync();

            return output;
        }
    }
}
