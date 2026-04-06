using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.ImageProcessing;
using Ocr.Api.Services.Ocr;
using Ocr.Api.Services.Pdf;
using Ocr.Api.Services.Pipeline;
using Ocr.Api.Services.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private readonly IImagePreprocessingService _imagePreprocessingService;
        private readonly IConfiguration _config;
        private readonly IOcrPipelineService _ocrPipelineService;
        private readonly IDocTrService _docTrService;

        public OcrController(
            ITempFileService tempFileService,
            IPdfTextDetector pdfTextDetector,
            IPdfRenderService renderService,
            ITesseractService tesseractService,
            IPdfMergeService pdfMergeService,
            IImagePreprocessingService imagePreprocessingService,
            IConfiguration config,
            IOcrPipelineService ocrPipelineService,
            IDocTrService docTrService)
        {
            _tempFileService = tempFileService;
            _pdfTextDetector = pdfTextDetector;
            _renderService = renderService;
            _tesseractService = tesseractService;
            _pdfMergeService = pdfMergeService;
            _imagePreprocessingService = imagePreprocessingService;
            _config = config;
            _ocrPipelineService = ocrPipelineService;
            _docTrService = docTrService;
        }

        [HttpPost("doctr-test")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RunDocTrTest(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var tempJobDir = _tempFileService.CreateJobDirectory("doctr_test");
            var inputPath = await _tempFileService.SaveFileAsync(file, tempJobDir);

            var result = await _docTrService.RunOcrAsync(inputPath);

            return Ok(new
            {
                result.Engine,
                result.ImagePath,
                result.Confidence,
                result.FullText,
                Words = result.Words.Count,
                result.JsonPath
            });
        }

        [HttpPost("manual")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RunManualOcr(IFormFile file)
        {
            var result = await ProcessSingleFileAsync(file);
            return Ok(result);
        }

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

        [HttpPost("merge-searchable")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> MergeSearchable(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var result = await _ocrPipelineService.MergeSearchableAsync(files);
            return Ok(result);
        }

        private static int GetRenderDpi(int pageCount)
        {
            if (pageCount <= 20)
                return 400;

            if (pageCount <= 75)
                return 300;

            return 250;
        }

        private async Task<object> ProcessSingleFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            var stopwatch = Stopwatch.StartNew();

            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "OCR Test Data",
                "results"
            );

            Directory.CreateDirectory(rootDir);

            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = string.Concat(originalName.Split(Path.GetInvalidFileNameChars()));

            var outputJobDir = Path.Combine(rootDir, safeName);
            Directory.CreateDirectory(outputJobDir);

            var tempJobDir = _tempFileService.CreateJobDirectory(safeName);
            var inputPath = await _tempFileService.SaveFileAsync(file, tempJobDir);

            bool cleanupTemps = _config.GetValue<bool>("Ocr:CleanupIntermediateFiles");
            bool useBest = _config.GetValue<bool>("Ocr:UseBest");
            int defaultDpi = _config.GetValue<int?>("Ocr:RenderDpi") ?? 300;
            int largeFilePageThreshold = _config.GetValue<int?>("Ocr:LargeFilePageThreshold") ?? 50;
            int chunkSize = _config.GetValue<int?>("Ocr:MergeChunkSize") ?? 25;

            var pagePdfs = new List<string>();
            var confidences = new List<float>();
            int pagesProcessed = 0;

            try
            {
                if (!IsImageFile(file.FileName) && _pdfTextDetector.HasText(inputPath))
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

                string tessDataPath = useBest
                    ? _config["Tesseract:Best"]
                    : _config["Tesseract:Fast"];

                if (string.IsNullOrWhiteSpace(tessDataPath))
                    throw new InvalidOperationException("Tesseract tessdata path is not configured.");

                if (IsImageFile(file.FileName))
                {
                    var ocrResult = await ProcessPageImageAsync(
                        sourceImagePath: inputPath,
                        pageNumber: 1,
                        outputJobDir: outputJobDir,
                        tessDataPath: tessDataPath,
                        cleanupTemps: cleanupTemps,
                        preprocessingOptions: BuildPreprocessingOptions(1, false)
                    );

                    pagePdfs.Add(ocrResult.PdfPath);
                    confidences.Add(ocrResult.Confidence);
                    pagesProcessed = 1;
                }
                else
                {
                    int pageCount = await _renderService.GetPageCountAsync(inputPath);
                    int renderDpi = GetRenderDpi(pageCount);
                    bool largeDocument = pageCount > 75;

                    for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
                    {
                        string renderedImagePath = await _renderService.RenderPageAsync(
                            inputPath,
                            tempJobDir,
                            pageNumber,
                            renderDpi
                        );

                        var ocrResult = await ProcessPageImageAsync(
                            sourceImagePath: renderedImagePath,
                            pageNumber: pageNumber,
                            outputJobDir: outputJobDir,
                            tessDataPath: tessDataPath,
                            cleanupTemps: cleanupTemps,
                            preprocessingOptions: BuildPreprocessingOptions(pageCount, largeDocument)
                        );

                        pagePdfs.Add(ocrResult.PdfPath);
                        confidences.Add(ocrResult.Confidence);
                        pagesProcessed++;

                        if (cleanupTemps)
                            _tempFileService.DeleteFileIfExists(renderedImagePath);
                    }
                }

                string mergedPdf;

                if (pagePdfs.Count == 0)
                    throw new InvalidOperationException("No OCR page PDFs were generated.");

                if (pagePdfs.Count > chunkSize)
                {
                    mergedPdf = await _pdfMergeService.MergeInChunksAsync(
                        pagePdfs,
                        outputJobDir,
                        safeName,
                        chunkSize
                    );
                }
                else
                {
                    mergedPdf = await _pdfMergeService.MergeAsync(
                        pagePdfs,
                        outputJobDir,
                        safeName
                    );
                }

                float overallConfidence = confidences.Count > 0
                    ? confidences.Average()
                    : 0;

                string quality = GetQualityLabel(overallConfidence);

                var qualityDir = Path.Combine(rootDir, quality);
                Directory.CreateDirectory(qualityDir);

                var finalPdfPath = Path.Combine(qualityDir, Path.GetFileName(mergedPdf));
                System.IO.File.Move(mergedPdf, finalPdfPath, true);

                if (cleanupTemps)
                {
                    foreach (var pagePdf in pagePdfs)
                        _tempFileService.DeleteFileIfExists(pagePdf);

                    _tempFileService.DeleteDirectoryIfExists(tempJobDir, true);
                }

                stopwatch.Stop();

                return new
                {
                    File = file.FileName,
                    Pages = pagesProcessed,
                    OcrConfidence = Math.Round(overallConfidence, 2),
                    Quality = quality,
                    OutputPdf = finalPdfPath,
                    TimeElapsed = stopwatch.Elapsed.ToString(@"hh\:mm\:ss")
                };
            }
            catch
            {
                if (cleanupTemps)
                    _tempFileService.DeleteDirectoryIfExists(tempJobDir, true);

                throw;
            }
        }

        private async Task<Ocr.Api.Models.OcrResult> ProcessPageImageAsync(
            string sourceImagePath,
            int pageNumber,
            string outputJobDir,
            string tessDataPath,
            bool cleanupTemps,
            ImagePreprocessingOptions preprocessingOptions)
        {
            string pageWorkDir = Path.Combine(outputJobDir, $"page_{pageNumber:000}");
            Directory.CreateDirectory(pageWorkDir);

            string pageImagePath = CopyImageToPageWorkDir(sourceImagePath, pageWorkDir, pageNumber);

            string processedImagePath = _imagePreprocessingService.Preprocess(pageImagePath, preprocessingOptions);

            var ocrResult = await _tesseractService.RunOcrAsync(
                processedImagePath,
                "eng+osd",
                tessDataPath
            );

            if (cleanupTemps)
            {
                if (!string.Equals(pageImagePath, sourceImagePath, StringComparison.OrdinalIgnoreCase))
                    _tempFileService.DeleteFileIfExists(pageImagePath);

                if (!string.Equals(processedImagePath, pageImagePath, StringComparison.OrdinalIgnoreCase))
                    _tempFileService.DeleteFileIfExists(processedImagePath);

                _tempFileService.DeleteFileIfExists(ocrResult.TextPath);
                _tempFileService.DeleteFileIfExists(ocrResult.TsvPath);
            }

            return ocrResult;
        }

        private ImagePreprocessingOptions BuildPreprocessingOptions(int pageCount, bool largeDocument)
        {
            if (largeDocument)
            {
                return new ImagePreprocessingOptions
                {
                    Enabled = true,
                    Profile = "light",
                    OverwriteIfExists = true
                };
            }

            return new ImagePreprocessingOptions
            {
                Enabled = true,
                Profile = "default",
                OverwriteIfExists = true
            };
        }

        private static string CopyImageToPageWorkDir(string sourceImagePath, string pageWorkDir, int pageNumber)
        {
            string extension = Path.GetExtension(sourceImagePath);
            string targetPath = Path.Combine(pageWorkDir, $"page-{pageNumber:000}{extension}");

            if (!string.Equals(sourceImagePath, targetPath, StringComparison.OrdinalIgnoreCase))
                System.IO.File.Copy(sourceImagePath, targetPath, true);

            return targetPath;
        }

        private static string GetQualityLabel(float overallConfidence)
        {
            if (overallConfidence >= 90)
                return "Excellent";

            if (overallConfidence >= 75)
                return "Good";

            if (overallConfidence >= 50)
                return "Fair";

            return "Poor";
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