using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.ImageProcessing;
using Ocr.Api.Services.Ocr;
using Ocr.Api.Services.Pdf;
using Ocr.Api.Services.Pipeline;
using Ocr.Api.Services.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

using System.Diagnostics;

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
        private readonly IImagePreprocessingService _imagePreprocessingService;
        private readonly IConfiguration _config;
        private readonly IOcrPipelineService _ocrPipelineService;

        public OcrController(
            ITempFileService tempFileService,
            IPdfTextDetector pdfTextDetector,
            IPdfRenderService renderService,
            ITesseractService tesseractService,
            IPdfMergeService pdfMergeService,
            IImagePreprocessingService imagePreprocessingService,
            IConfiguration config,
            IOcrPipelineService ocrPipelineService)
        {
            _tempFileService = tempFileService;
            _pdfTextDetector = pdfTextDetector;
            _renderService = renderService;
            _tesseractService = tesseractService;
            _pdfMergeService = pdfMergeService;
            _imagePreprocessingService = imagePreprocessingService;
            _config = config;
            _ocrPipelineService = ocrPipelineService;
        }

        // Single File OCR
        [HttpPost("manual")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RunManualOcr(IFormFile file)
        {
            var result = await ProcessSingleFileAsync(file);
            return Ok(result);
        }

        // Batch File OCR
        [HttpPost("batch")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RunBatchOcr(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var batchStopwatch = Stopwatch.StartNew();
            var results = new List<object>();

            foreach (var file in files)
            {
                try
                {
                    var result = await ProcessSingleFileAsync(file);

                    results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        File = file.FileName,
                        Status = "Failed",
                        Error = ex.Message
                    });
                }
            }

            batchStopwatch.Stop();

            return Ok(new
            {
                TotalFiles = files.Count,
                Processed = results.Count,
                TotalElapsed = batchStopwatch.Elapsed.ToString(@"hh\:mm\:ss"),
                Results = results
            });
        }

        // 🔵 NEW ENDPOINT: Merge searchable + non-searchable files
        [HttpPost("merge-searchable")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> MergeSearchable(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var result = await _ocrPipelineService.MergeSearchableAsync(files);

            return Ok(result);
        }

        private async Task<object> ProcessSingleFileAsync(IFormFile file)
        {
            var stopwatch = Stopwatch.StartNew();

            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "OCR Test Data",
                "results"
            );

            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = string.Concat(originalName.Split(Path.GetInvalidFileNameChars()));

            var jobDir = Path.Combine(rootDir, safeName);
            Directory.CreateDirectory(jobDir);

            var inputPath = await _tempFileService.SaveFileAsync(file);

            var images = new List<string>();

            if (IsImageFile(file.FileName))
            {
                images.Add(inputPath);
            }
            else
            {
                if (_pdfTextDetector.HasText(inputPath))
                {
                    stopwatch.Stop();

                    return new
                    {
                        File = file.FileName,
                        Status = "Already searchable",
                        OutputPdf = inputPath,
                        TimeElapsed = stopwatch.Elapsed.ToString(@"hh\:mm\:ss")
                    };
                }

                images = (await _renderService.RenderAsync(inputPath, jobDir, 400)).ToList();
            }

            bool useBest = _config.GetValue<bool>("Ocr:UseBest");

            var tessDataPath = useBest
                ? _config["Tesseract:Best"]
                : _config["Tesseract:Fast"];

            var pagePdfs = new List<string>();
            var confidences = new List<float>();
            var processedImages = new List<string>();

            foreach (var image in images)
            {
                var processedImage = _imagePreprocessingService.Preprocess(image);
                processedImages.Add(processedImage);

                var ocrResult = await _tesseractService.RunOcrAsync(
                    processedImage,
                    "eng+osd",
                    tessDataPath
                );

                pagePdfs.Add(ocrResult.PdfPath);
                confidences.Add(ocrResult.Confidence);
            }

            var mergedPdf = await _pdfMergeService.MergeAsync(
                pagePdfs,
                jobDir,
                safeName
            );

            var overallConfidence = confidences.Count > 0
                ? confidences.Average()
                : 0;

            string quality;

            if (overallConfidence >= 90)
                quality = "Excellent";
            else if (overallConfidence >= 75)
                quality = "Good";
            else if (overallConfidence >= 50)
                quality = "Fair";
            else
                quality = "Poor";

            var qualityDir = Path.Combine(rootDir, quality);
            Directory.CreateDirectory(qualityDir);

            var finalPdfPath = Path.Combine(qualityDir, Path.GetFileName(mergedPdf));

            System.IO.File.Move(mergedPdf, finalPdfPath, true);

            if (_config.GetValue<bool>("Ocr:CleanupIntermediateFiles"))
            {
                CleanupIntermediateFiles(images.Concat(processedImages), pagePdfs, finalPdfPath);
            }

            stopwatch.Stop();

            return new
            {
                File = file.FileName,
                Pages = pagePdfs.Count,
                OcrConfidence = Math.Round(overallConfidence, 2),
                Quality = quality,
                OutputPdf = finalPdfPath,
                TimeElapsed = stopwatch.Elapsed.ToString(@"hh\:mm\:ss")
            };
        }

        private void CleanupIntermediateFiles(
            IEnumerable<string> imageFiles,
            IEnumerable<string> pagePdfFiles,
            string mergedPdfPath)
        {
            var mergedFileName = Path.GetFileName(mergedPdfPath);

            var directoriesToCheck = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var file in imageFiles.Concat(pagePdfFiles))
            {
                try
                {
                    if (Path.GetFileName(file)
                        .Equals(mergedFileName, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (System.IO.File.Exists(file))
                    {
                        directoriesToCheck.Add(Path.GetDirectoryName(file)!);
                        System.IO.File.Delete(file);
                    }
                }
                catch
                {
                }
            }

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
                }
            }
        }

        private static bool IsImageFile(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext == ".png" ||
                   ext == ".jpg" ||
                   ext == ".jpeg" ||
                   ext == ".tif" ||
                   ext == ".tiff" ||
                   ext == ".webp";
        }
    }
}