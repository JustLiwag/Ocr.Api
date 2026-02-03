using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.Pdf;
using Ocr.Api.Services.Rendering;
using Ocr.Api.Services.Ocr;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Controllers
{
    [ApiController]
    [Route("api/ocr")]
    public class OcrController : ControllerBase
    {
        private readonly ITempFileService _tempFileService;
        private readonly IPdfTextDetector _pdfTextDetector;
        private readonly IPdfRenderService _renderService;
        private readonly ITesseractService _tesseractService;
        private readonly IPdfMergeService _pdfMergeService;
        private readonly IConfiguration _config;


        public OcrController(
            ITempFileService tempFileService,
            IPdfTextDetector pdfTextDetector,
            IPdfRenderService renderService,
            ITesseractService tesseractService,
            IPdfMergeService pdfMergeService,
            IConfiguration config)
        {
            _tempFileService = tempFileService;
            _pdfTextDetector = pdfTextDetector;
            _renderService = renderService;
            _tesseractService = tesseractService;
            _pdfMergeService = pdfMergeService;
            _config = config;
        }

        [HttpPost("manual")]
        public async Task<IActionResult> RunManualOcr(IFormFile file)
        {
            var rootDir = @"C:\Users\jeliwag\Downloads\OCR Test Data\results";

            // 🔹 Use uploaded file name (without extension) for folder & PDF
            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = string.Concat(
                originalName.Split(Path.GetInvalidFileNameChars())
            );

            var jobDir = Path.Combine(rootDir, safeName);
            Directory.CreateDirectory(jobDir);

            var pdfPath = await _tempFileService.SaveFileAsync(file);

            if (_pdfTextDetector.HasText(pdfPath))
                return Ok("PDF already searchable.");

            // ✅ Render PNGs INTO job folder
            var images = await _renderService.RenderAsync(pdfPath, jobDir, 300);

            bool useBest = false;

            var tessDataPath = useBest
                ? _config["Tesseract:Best"]
                : _config["Tesseract:Fast"];

            var pagePdfs = new List<string>();

            foreach (var image in images)
            {
                var pdf = await _tesseractService.RunOcrAsync(
                    image,
                    "eng",
                    tessDataPath
                );

                pagePdfs.Add(pdf);
            }

            // ✅ Merge INTO SAME folder with SAME name
            var mergedPdf = await _pdfMergeService.MergeAsync(
                pagePdfs,
                jobDir,
                safeName
            );

            // 🔥 CLEANUP (keep only merged PDF)
            if (_config.GetValue<bool>("Ocr:CleanupIntermediateFiles"))
            {
                CleanupIntermediateFiles(images, pagePdfs, mergedPdf);
            }

            return Ok(new
            {
                Pages = pagePdfs.Count,
                OutputPdf = mergedPdf
            });
        }


        private void CleanupIntermediateFiles(
    IEnumerable<string> imageFiles,
    IEnumerable<string> pagePdfFiles,
    string mergedPdfPath)
        {
            var mergedFileName = Path.GetFileName(mergedPdfPath);

            // Track parent directories of deleted files
            var directoriesToCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in imageFiles.Concat(pagePdfFiles))
            {
                try
                {
                    // Extra safety: never delete the merged PDF
                    if (Path.GetFileName(file)
                        .Equals(mergedFileName, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (System.IO.File.Exists(file))
                    {
                        directoriesToCheck.Add(Path.GetDirectoryName(file)!);
                        System.IO.File.Delete(file);
                    }
                }
                catch
                {
                    // Intentionally swallow errors
                    // (file locks, antivirus, etc.)
                }
            }

            // 🔥 Remove empty per-page folders
            foreach (var dir in directoriesToCheck)
            {
                try
                {
                    if (Directory.Exists(dir) &&
                        !Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                    }
                }
                catch
                {
                    // Ignore folder delete issues
                }
            }
        }

    }
}
