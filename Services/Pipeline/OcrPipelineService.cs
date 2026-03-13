using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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

            var rootDir = @"C:\Users\emedeleon\Downloads\OCR Test Data\results";
            Directory.CreateDirectory(rootDir);

            var jobId = Guid.NewGuid().ToString();
            var jobDir = Path.Combine(rootDir, jobId);
            Directory.CreateDirectory(jobDir);

            var finalPdfList = new List<string>();
            var confidences = new List<float>();

            bool useBest = _config.GetValue<bool>("Ocr:UseBest");

            var tessDataPath = useBest
                ? _config["Tesseract:Best"]
                : _config["Tesseract:Fast"];

            foreach (var file in files)
            {
                var inputPath = await _tempFileService.SaveFileAsync(file);

                // IMAGE FILE
                if (IsImageFile(file.FileName))
                {
                    var processedImage = _imagePreprocessingService.Preprocess(inputPath);

                    var ocrResult = await _tesseractService.RunOcrAsync(
                        processedImage,
                        "eng+osd",
                        tessDataPath
                    );

                    finalPdfList.Add(ocrResult.PdfPath);
                    confidences.Add(ocrResult.Confidence);

                    continue;
                }

                // PDF FILE
                if (IsPdfFile(file.FileName))
                {
                    // Already searchable → keep but do not add confidence
                    if (_pdfTextDetector.HasText(inputPath))
                    {
                        finalPdfList.Add(inputPath);
                        continue;
                    }

                    var images = await _renderService.RenderAsync(
                        inputPath,
                        jobDir,
                        400
                    );

                    var pagePdfs = new List<string>();

                    foreach (var image in images)
                    {
                        var processedImage = _imagePreprocessingService.Preprocess(image);

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
                        Path.GetFileNameWithoutExtension(file.FileName)
                    );

                    finalPdfList.Add(mergedPdf);
                }
            }

            // FINAL MERGE
            var finalMerged = await _pdfMergeService.MergeAsync(
                finalPdfList,
                jobDir,
                "Final_Searchable_Document"
            );

            // Calculate confidence
            var overallConfidence = confidences.Count > 0
                ? confidences.Average()
                : 100;

            // Determine quality
            string quality;

            if (overallConfidence >= 90)
                quality = "Excellent";
            else if (overallConfidence >= 75)
                quality = "Good";
            else if (overallConfidence >= 50)
                quality = "Fair";
            else
                quality = "Poor";

            // Create quality folder
            var qualityDir = Path.Combine(rootDir, quality);
            Directory.CreateDirectory(qualityDir);

            // Move final PDF
            var finalPdfPath = Path.Combine(
                qualityDir,
                Path.GetFileName(finalMerged)
            );

            File.Move(finalMerged, finalPdfPath, true);

            return new
            {
                FilesUploaded = files.Count,
                OcrConfidence = Math.Round(overallConfidence, 2),
                Quality = quality,
                OutputPdf = finalPdfPath
            };
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