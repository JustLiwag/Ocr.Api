using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Ocr
{
    public class TesseractService : ITesseractService
    {
        public async Task<string> RunOcrAsync(
        string imagePath,
        string lang,
        string tessDataDir)
        {
            var outputBase = Path.Combine(
                Path.GetDirectoryName(imagePath)!,
                Path.GetFileNameWithoutExtension(imagePath)
            );

            //        var args =
            //$"\"{imagePath}\" \"{outputBase}\" " +
            //$"-l {lang} --tessdata-dir \"{tessDataDir}\" " +
            //"-c tessedit_create_pdf=1 pdf";

            var args =
            $"\"{imagePath}\" \"{outputBase}\" " +
            $"-l {lang} " +
            $"--tessdata-dir \"{tessDataDir}\" " +
            //"--oem 1 " +
            "-c tessedit_create_pdf=1 pdf";


            var psi = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi)!;

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception(
                    $"Tesseract failed.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
            }

            var pdfPath = outputBase + ".pdf";

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("OCR PDF not created", pdfPath);

            return pdfPath;
        }

    }
}
