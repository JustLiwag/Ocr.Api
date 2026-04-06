using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Ocr.Api.Models;
using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.ImageProcessing;
using Ocr.Api.Services.Ocr;
using Ocr.Api.Services.Pdf;
using Ocr.Api.Services.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Pipeline
{
    public class OcrPipelineService : IOcrPipelineService
    {
        private readonly ITempFileService _tempFileService;
        private readonly IPdfTextDetector _pdfTextDetector;
        private readonly IPdfRenderService _renderService;
        private readonly ITesseractService _tesseractService;
        private readonly IPdfMergeService _pdfMergeService;
        private readonly IImagePreprocessingService _imagePreprocessingService;
        private readonly IConfiguration _config;

        public OcrPipelineService(
            ITempFileService tempFileService,
            IPdfTextDetector pdfTextDetector,
            IPdfRenderService renderService,
            ITesseractService tesseractService,
            IPdfMergeService pdfMergeService,
            IImagePreprocessingService imagePreprocessingService,
            IConfiguration config)
        {
            _tempFileService = tempFileService;
            _pdfTextDetector = pdfTextDetector;
            _renderService = renderService;
            _tesseractService = tesseractService;
            _pdfMergeService = pdfMergeService;
            _imagePreprocessingService = imagePreprocessingService;
            _config = config;
        }

        public async Task<object> MergeSearchableAsync(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new Exception("No files uploaded.");

            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "OCR Test Data",
                "results"
            );

            Directory.CreateDirectory(rootDir);

            var tempJobDir = _tempFileService.CreateJobDirectory("merge_searchable");
            var outputJobDir = Path.Combine(rootDir, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outputJobDir);

            var finalPdfList = new List<string>();
            var confidences = new List<float>();

            bool cleanupTemps = _config.GetValue<bool>("Ocr:CleanupIntermediateFiles");
            bool useBest = _config.GetValue<bool>("Ocr:UseBest");
            int defaultDpi = _config.GetValue<int?>("Ocr:RenderDpi") ?? 300;
            int largeFilePageThreshold = _config.GetValue<int?>("Ocr:LargeFilePageThreshold") ?? 50;
            int chunkSize = _config.GetValue<int?>("Ocr:MergeChunkSize") ?? 25;

            string? tessDataPath = useBest
                ? _config["Tesseract:Best"]
                : _config["Tesseract:Fast"];

            if (string.IsNullOrWhiteSpace(tessDataPath))
                throw new InvalidOperationException("Tesseract tessdata path is not configured.");

            try
            {
                foreach (var file in files)
                {
                    var safeFileName = string.Concat(
                        Path.GetFileNameWithoutExtension(file.FileName)
                            .Split(Path.GetInvalidFileNameChars())
                    );

                    string inputPath = await _tempFileService.SaveFileAsync(file, tempJobDir);

                    if (IsImageFile(file.FileName))
                    {
                        var imageResult = await ProcessImageFileAsync(
                            inputPath,
                            safeFileName,
                            outputJobDir,
                            tessDataPath,
                            cleanupTemps
                        );

                        finalPdfList.Add(imageResult.PdfPath);
                        confidences.Add(imageResult.Confidence);

                        continue;
                    }

                    if (IsPdfFile(file.FileName))
                    {
                        if (_pdfTextDetector.HasText(inputPath))
                        {
                            finalPdfList.Add(inputPath);
                            continue;
                        }

                        var pdfResult = await ProcessPdfFileAsync(
                            inputPath,
                            safeFileName,
                            outputJobDir,
                            tessDataPath,
                            cleanupTemps,
                            defaultDpi,
                            largeFilePageThreshold,
                            chunkSize,
                            confidences
                        );

                        finalPdfList.Add(pdfResult);
                    }
                }

                if (finalPdfList.Count == 0)
                    throw new InvalidOperationException("No output PDFs were generated.");

                string finalMerged = finalPdfList.Count > chunkSize
                    ? await _pdfMergeService.MergeInChunksAsync(finalPdfList, outputJobDir, "Final_Searchable_Document", chunkSize)
                    : await _pdfMergeService.MergeAsync(finalPdfList, outputJobDir, "Final_Searchable_Document");

                float overallConfidence = confidences.Count > 0
                    ? confidences.Average()
                    : 100;

                string quality = GetQualityLabel(overallConfidence);

                var qualityDir = Path.Combine(rootDir, quality);
                Directory.CreateDirectory(qualityDir);

                var finalPdfPath = Path.Combine(qualityDir, Path.GetFileName(finalMerged));
                File.Move(finalMerged, finalPdfPath, true);

                if (cleanupTemps)
                {
                    foreach (var generatedPdf in finalPdfList)
                    {
                        if (!string.Equals(generatedPdf, finalPdfPath, StringComparison.OrdinalIgnoreCase))
                            _tempFileService.DeleteFileIfExists(generatedPdf);
                    }

                    _tempFileService.DeleteDirectoryIfExists(tempJobDir, true);
                }

                return new
                {
                    FilesUploaded = files.Count,
                    OcrConfidence = Math.Round(overallConfidence, 2),
                    Quality = quality,
                    OutputPdf = finalPdfPath
                };
            }
            catch
            {
                if (cleanupTemps)
                    _tempFileService.DeleteDirectoryIfExists(tempJobDir, true);

                throw;
            }
        }

        private async Task<OcrResult> ProcessImageFileAsync(
            string inputPath,
            string safeFileName,
            string outputJobDir,
            string tessDataPath,
            bool cleanupTemps)
        {
            string imageWorkDir = Path.Combine(outputJobDir, safeFileName);
            Directory.CreateDirectory(imageWorkDir);

            var preprocessingOptions = new ImagePreprocessingOptions
            {
                Enabled = true,
                Profile = "default",
                OverwriteIfExists = true
            };

            string processedImage = _imagePreprocessingService.Preprocess(inputPath, preprocessingOptions);

            var ocrResult = await _tesseractService.RunOcrAsync(
                processedImage,
                "eng+osd",
                tessDataPath
            );

            if (cleanupTemps)
            {
                if (!string.Equals(processedImage, inputPath, StringComparison.OrdinalIgnoreCase))
                    _tempFileService.DeleteFileIfExists(processedImage);

                _tempFileService.DeleteFileIfExists(ocrResult.TextPath);
                _tempFileService.DeleteFileIfExists(ocrResult.TsvPath);
            }

            return ocrResult;
        }

        private static int GetRenderDpi(int pageCount)
        {
            if (pageCount <= 20)
                return 400;

            if (pageCount <= 75)
                return 300;

            return 250;
        }

        private async Task<string> ProcessPdfFileAsync(
            string inputPath,
            string safeFileName,
            string outputJobDir,
            string tessDataPath,
            bool cleanupTemps,
            int defaultDpi,
            int largeFilePageThreshold,
            int chunkSize,
            List<float> confidences)
        {
            int pageCount = await _renderService.GetPageCountAsync(inputPath);
            int renderDpi = GetRenderDpi(pageCount);
            bool largeDocument = pageCount > 75;

            string pdfOutputDir = Path.Combine(outputJobDir, safeFileName);
            Directory.CreateDirectory(pdfOutputDir);

            var pagePdfs = new List<string>();

            for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
            {
                string renderedImagePath = await _renderService.RenderPageAsync(
                    inputPath,
                    pdfOutputDir,
                    pageNumber,
                    renderDpi
                );

                var preprocessingOptions = BuildPreprocessingOptions(largeDocument);

                string processedImagePath = _imagePreprocessingService.Preprocess(
                    renderedImagePath,
                    preprocessingOptions
                );

                var ocrResult = await _tesseractService.RunOcrAsync(
                    processedImagePath,
                    "eng+osd",
                    tessDataPath
                );

                pagePdfs.Add(ocrResult.PdfPath);
                confidences.Add(ocrResult.Confidence);

                if (cleanupTemps)
                {
                    _tempFileService.DeleteFileIfExists(renderedImagePath);

                    if (!string.Equals(processedImagePath, renderedImagePath, StringComparison.OrdinalIgnoreCase))
                        _tempFileService.DeleteFileIfExists(processedImagePath);

                    _tempFileService.DeleteFileIfExists(ocrResult.TextPath);
                    _tempFileService.DeleteFileIfExists(ocrResult.TsvPath);
                }
            }

            if (pagePdfs.Count == 0)
                throw new InvalidOperationException($"No page PDFs were generated for '{safeFileName}'.");

            string mergedPdf = pagePdfs.Count > chunkSize
                ? await _pdfMergeService.MergeInChunksAsync(pagePdfs, pdfOutputDir, safeFileName, chunkSize)
                : await _pdfMergeService.MergeAsync(pagePdfs, pdfOutputDir, safeFileName);

            if (cleanupTemps)
            {
                foreach (var pagePdf in pagePdfs)
                    _tempFileService.DeleteFileIfExists(pagePdf);
            }

            return mergedPdf;
        }

        private static ImagePreprocessingOptions BuildPreprocessingOptions(bool largeDocument)
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

            return ext == ".png"
                || ext == ".jpg"
                || ext == ".jpeg"
                || ext == ".tif"
                || ext == ".tiff"
                || ext == ".webp";
        }

        private static bool IsPdfFile(string fileName)
        {
            return Path.GetExtension(fileName)
                .Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }
    }
}