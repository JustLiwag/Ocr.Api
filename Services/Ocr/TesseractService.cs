using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Ocr
{
    public class TesseractService : ITesseractService
    {
        public async Task<string> RunOcrAsync(string imagePath, string lang = "eng")
        {
            var outputBase = Path.ChangeExtension(imagePath, null);

            var args = $"\"{imagePath}\" \"{outputBase}\" -l {lang} pdf";

            var psi = new ProcessStartInfo("tesseract", args)
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            await proc.WaitForExitAsync();

            return outputBase + ".pdf";
        }
    }
}
