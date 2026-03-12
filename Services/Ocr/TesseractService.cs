using Ocr.Api.Models;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Ocr
{
    public class TesseractService : ITesseractService
    {
        private readonly string _tesseractPath = @"C:\Users\jeliwag\AppData\Local\Programs\Tesseract-OCR\tesseract.exe";

        private float CalculateConfidence(string tsvPath)
        {
            if (!File.Exists(tsvPath))
                return 80;

            var lines = File.ReadAllLines(tsvPath);

            float total = 0;
            int count = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var cols = lines[i].Split('\t');

                if (cols.Length < 11)
                    continue;

                if (float.TryParse(cols[10], out float conf))
                {
                    if (conf >= 0)
                    {
                        total += conf;
                        count++;
                    }
                }
            }

            if (count == 0)
                return 0;

            return total / count;
        }

        private async Task RunProcessAsync(string args, string workingDir)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _tesseractPath,        // Use full path to tesseract.exe
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDir    // Important to produce TSV/PDF in correct folder
            };

            using var process = Process.Start(psi)!;

            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"Tesseract failed.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }

        public async Task<OcrResult> RunOcrAsync(
            string imagePath,
            string lang,
            string tessDataDir)
        {
            var outputBase = Path.Combine(
                Path.GetDirectoryName(imagePath)!,
                Path.GetFileNameWithoutExtension(imagePath)
            );

            var workingDir = Path.GetDirectoryName(imagePath)!;

            // 1️⃣ Generate PDF
            var pdfArgs = $"\"{imagePath}\" \"{outputBase}\" -l {lang} --tessdata-dir \"{tessDataDir}\" --oem 1 --psm 1 -c tessedit_create_pdf=1 -c textonly_pdf=0 -c preserve_interword_spaces=1 -c pdf_font_hinting=1 pdf";
            await RunProcessAsync(pdfArgs, workingDir);

            // 2️⃣ Generate TSV
            var tsvArgs = $"\"{imagePath}\" \"{outputBase}\" -l {lang} --tessdata-dir \"{tessDataDir}\" --oem 1 --psm 1 tsv";
            await RunProcessAsync(tsvArgs, workingDir);

            var pdfPath = outputBase + ".pdf";
            var tsvPath = outputBase + ".tsv";

            if (!File.Exists(pdfPath))
                throw new FileNotFoundException("OCR PDF not created", pdfPath);

            float confidence = CalculateConfidence(tsvPath);

            return new OcrResult
            {
                PdfPath = pdfPath,
                Confidence = confidence
            };
        }
    }
}