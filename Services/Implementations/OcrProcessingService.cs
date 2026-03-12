using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Ocr.Api.Models;
using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.Helpers;
using Ocr.Api.Services.ImageProcessing;
using Ocr.Api.Services.Interfaces;
using Ocr.Api.Services.Ocr;
using Ocr.Api.Services.Pdf;
using Ocr.Api.Services.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Implementations
{
    /*
        =========================================================
        OcrProcessingService
        ---------------------------------------------------------
        Purpose:
        - Handles single-file OCR processing
        - Uses the real OCR pipeline services
        - Produces searchable PDF output
        - Returns platform-style OCR response DTO

        Notes:
        - DB persistence is still not wired yet
        - OCR document/artifact IDs are still simulated for now
        - Raw text extraction is left empty for now because the
          current ITesseractService contract shown only guarantees
          PdfPath and Confidence.
        =========================================================
    */

    public class OcrProcessingService : IOcrProcessingService
    {
        private readonly ITempFileService _tempFileService;
        private readonly IPdfTextDetector _pdfTextDetector;
        private readonly IPdfRenderService _renderService;
        private readonly ITesseractService _tesseractService;
        private readonly IPdfMergeService _pdfMergeService;
        private readonly IImagePreprocessingService _imagePreprocessingService;
        private readonly IConfiguration _config;
        private readonly string _artifactBasePath;

        public OcrProcessingService(
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

            _artifactBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "OcrArtifacts"
            );
        }

        public async Task<OcrProcessResponseDto> processSingleAsync(IFormFile file, OcrProcessRequestDto request)
        {
            if (file == null || file.Length == 0)
            {
                return new OcrProcessResponseDto
                {
                    success = false,
                    status = OcrDocumentStatus.Failed.ToString()
                };
            }

            validateRequest(request);

            string inputPath = await _tempFileService.SaveFileAsync(file);

            return await processSavedFileAsync(
                inputPath,
                file.FileName,
                file.Length,
                request
            );
        }

        public async Task<OcrProcessResponseDto> processSingleAsync(byte[] fileBytes, string fileName, OcrProcessRequestDto request)
        {
            validateRequest(request);
            validateFile(fileBytes, fileName);

            string tempDirectory = Path.Combine(Path.GetTempPath(), "ocr-api-input");
            Directory.CreateDirectory(tempDirectory);

            string tempFilePath = Path.Combine(
                tempDirectory,
                $"{Guid.NewGuid():N}{Path.GetExtension(fileName)}"
            );

            await File.WriteAllBytesAsync(tempFilePath, fileBytes);

            return await processSavedFileAsync(
                tempFilePath,
                fileName,
                fileBytes.LongLength,
                request
            );
        }

        /*
            =========================================================
            MAIN PROCESSING FLOW
            =========================================================
        */

        private async Task<OcrProcessResponseDto> processSavedFileAsync(
            string inputPath,
            string fileName,
            long fileSizeBytes,
            OcrProcessRequestDto request)
        {
            DateTime processingStartTime = DateTime.Now;
            string ocrReferenceNo = OcrReferenceGenerator.generateOcrReferenceNo();

            OcrDocument ocrDocument = new OcrDocument
            {
                ocrDocumentId = 1,
                ocrReferenceNo = ocrReferenceNo,
                sourceSystem = request.sourceSystem,
                officeCode = request.officeCode,
                documentTypeCode = request.documentTypeCode,
                sensitivityLevel = request.sensitivityLevel,
                externalDocumentId = request.externalDocumentId,
                externalRecordId = request.externalRecordId,
                originalFileName = fileName,
                originalFileExtension = Path.GetExtension(fileName),
                originalMimeType = getMimeTypeFromFileName(fileName),
                fileSizeBytes = fileSizeBytes,
                totalPages = 1,
                status = OcrDocumentStatus.Processing.ToString(),
                processingStartedAt = processingStartTime,
                createdAt = DateTime.Now,
                createdBy = request.requestUserId
            };

            string originalName = Path.GetFileNameWithoutExtension(fileName);
            string safeName = buildSafeFileName(originalName);
            string jobDirectory = Path.Combine(_artifactBasePath, safeName);

            Directory.CreateDirectory(jobDirectory);

            List<string> images = new List<string>();
            List<string> processedImages = new List<string>();
            List<string> pagePdfs = new List<string>();
            List<float> confidences = new List<float>();
            string rawFullText = string.Empty;
            string finalPdfPath = string.Empty;

            try
            {
                if (isImageFile(fileName))
                {
                    images.Add(inputPath);
                }
                else
                {
                    if (_pdfTextDetector.HasText(inputPath))
                    {
                        ocrDocument.rawFullText = string.Empty;
                        ocrDocument.finalFullText = string.Empty;
                        ocrDocument.averageConfidence = 100m;
                        ocrDocument.confidenceBand = ConfidenceHelper.getConfidenceBand(100m);
                        ocrDocument.needsReview = false;
                        ocrDocument.status = OcrDocumentStatus.Processed.ToString();
                        ocrDocument.processingCompletedAt = DateTime.Now;

                        return new OcrProcessResponseDto
                        {
                            success = true,
                            ocrDocumentId = ocrDocument.ocrDocumentId,
                            ocrReferenceNo = ocrDocument.ocrReferenceNo,
                            sourceSystem = ocrDocument.sourceSystem,
                            officeCode = ocrDocument.officeCode,
                            documentTypeCode = ocrDocument.documentTypeCode,
                            averageConfidence = ocrDocument.averageConfidence,
                            confidenceBand = ocrDocument.confidenceBand,
                            needsReview = ocrDocument.needsReview,
                            status = "AlreadySearchable",
                            rawFullText = string.Empty,
                            searchablePdf = new OcrSearchablePdfDto
                            {
                                fileName = Path.GetFileName(inputPath),
                                filePath = inputPath,
                                version = 1,
                                base64Content = null
                            },
                            suggestionsAvailable = false,
                            fields = new List<OcrFieldResultDto>()
                        };
                    }

                    images = (await _renderService.RenderAsync(inputPath, jobDirectory, 400)).ToList();
                }

                bool useBest = _config.GetValue<bool>("Ocr:UseBest");

                string tessDataPath = useBest
                    ? _config["Tesseract:Best"] ?? string.Empty
                    : _config["Tesseract:Fast"] ?? string.Empty;

                foreach (string image in images)
                {
                    string processedImage = _imagePreprocessingService.Preprocess(image);
                    processedImages.Add(processedImage);

                    OcrResult ocrResult = await _tesseractService.RunOcrAsync(
                        processedImage,
                        "eng+osd",
                        tessDataPath
                    );

                    pagePdfs.Add(ocrResult.PdfPath);
                    confidences.Add(ocrResult.Confidence);
                }

                finalPdfPath = await _pdfMergeService.MergeAsync(
                    pagePdfs,
                    jobDirectory,
                    safeName
                );

                decimal? averageConfidence = confidences.Count > 0
                    ? Math.Round((decimal)confidences.Average(), 2)
                    : 0m;

                string confidenceBand = ConfidenceHelper.getConfidenceBand(averageConfidence);
                bool needsReview = ConfidenceHelper.needsReview(averageConfidence);

                ocrDocument.totalPages = pagePdfs.Count;
                ocrDocument.rawFullText = rawFullText.Trim();
                ocrDocument.finalFullText = rawFullText.Trim();
                ocrDocument.averageConfidence = averageConfidence;
                ocrDocument.confidenceBand = confidenceBand;
                ocrDocument.needsReview = needsReview;
                ocrDocument.status = needsReview
                    ? OcrDocumentStatus.NeedsReview.ToString()
                    : OcrDocumentStatus.Processed.ToString();
                ocrDocument.processingCompletedAt = DateTime.Now;

                OcrArtifact searchablePdfArtifact = new OcrArtifact
                {
                    ocrArtifactId = 1,
                    ocrDocumentId = ocrDocument.ocrDocumentId,
                    artifactType = OcrArtifactType.SearchablePdf.ToString(),
                    fileName = Path.GetFileName(finalPdfPath),
                    filePath = finalPdfPath,
                    fileExtension = Path.GetExtension(finalPdfPath),
                    mimeType = SearchablePdfHelper.getMimeType(),
                    fileSizeBytes = SearchablePdfHelper.getFileSizeBytes(finalPdfPath),
                    versionNo = 1,
                    isLatest = true,
                    createdAt = DateTime.Now,
                    createdBy = request.requestUserId
                };

                if (_config.GetValue<bool>("Ocr:CleanupIntermediateFiles"))
                {
                    cleanupIntermediateFiles(images, processedImages, pagePdfs, finalPdfPath);
                }

                return new OcrProcessResponseDto
                {
                    success = true,
                    ocrDocumentId = ocrDocument.ocrDocumentId,
                    ocrReferenceNo = ocrDocument.ocrReferenceNo,
                    sourceSystem = ocrDocument.sourceSystem,
                    officeCode = ocrDocument.officeCode,
                    documentTypeCode = ocrDocument.documentTypeCode,
                    averageConfidence = ocrDocument.averageConfidence,
                    confidenceBand = ocrDocument.confidenceBand,
                    needsReview = ocrDocument.needsReview,
                    status = ocrDocument.status,
                    rawFullText = ocrDocument.rawFullText,
                    searchablePdf = new OcrSearchablePdfDto
                    {
                        fileName = searchablePdfArtifact.fileName,
                        filePath = searchablePdfArtifact.filePath,
                        version = searchablePdfArtifact.versionNo,
                        base64Content = request.returnSearchablePdfAsBase64
                            ? Convert.ToBase64String(await File.ReadAllBytesAsync(finalPdfPath))
                            : null
                    },
                    suggestionsAvailable = false,
                    fields = buildFieldResults(request.fields)
                };
            }
            catch
            {
                ocrDocument.status = OcrDocumentStatus.Failed.ToString();
                ocrDocument.processingCompletedAt = DateTime.Now;
                throw;
            }
        }

        /*
            =========================================================
            PRIVATE HELPERS
            =========================================================
        */

        private void validateRequest(OcrProcessRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "OCR request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.sourceSystem))
            {
                throw new Exception("Source system is required.");
            }

            if (string.IsNullOrWhiteSpace(request.officeCode))
            {
                throw new Exception("Office code is required.");
            }

            if (string.IsNullOrWhiteSpace(request.documentTypeCode))
            {
                throw new Exception("Document type code is required.");
            }

            if (string.IsNullOrWhiteSpace(request.sensitivityLevel))
            {
                throw new Exception("Sensitivity level is required.");
            }
        }

        private void validateFile(byte[] fileBytes, string fileName)
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new Exception("File content is empty.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new Exception("File name is required.");
            }
        }

        private string getMimeTypeFromFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;

            switch (extension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".webp":
                    return "image/webp";
                case ".tif":
                case ".tiff":
                    return "image/tiff";
                default:
                    return "application/octet-stream";
            }
        }

        private static bool isImageFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();

            return ext == ".png" ||
                   ext == ".jpg" ||
                   ext == ".jpeg" ||
                   ext == ".tif" ||
                   ext == ".tiff" ||
                   ext == ".webp";
        }

        private string buildSafeFileName(string fileNameWithoutExtension)
        {
            return string.Concat(fileNameWithoutExtension.Split(Path.GetInvalidFileNameChars()));
        }

        private void cleanupIntermediateFiles(
            IEnumerable<string> imageFiles,
            IEnumerable<string> processedImageFiles,
            IEnumerable<string> pagePdfFiles,
            string mergedPdfPath)
        {
            string mergedFileName = Path.GetFileName(mergedPdfPath);

            HashSet<string> directoriesToCheck = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in imageFiles.Concat(processedImageFiles).Concat(pagePdfFiles))
            {
                try
                {
                    if (Path.GetFileName(file).Equals(mergedFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (File.Exists(file))
                    {
                        string? directory = Path.GetDirectoryName(file);

                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            directoriesToCheck.Add(directory);
                        }

                        File.Delete(file);
                    }
                }
                catch
                {
                }
            }

            foreach (string directory in directoriesToCheck)
            {
                try
                {
                    if (Directory.Exists(directory) &&
                        !Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory);
                    }
                }
                catch
                {
                }
            }
        }

        private List<OcrFieldResultDto> buildFieldResults(List<OcrFieldRequestDto> requestedFields)
        {
            List<OcrFieldResultDto> fieldResults = new List<OcrFieldResultDto>();

            if (requestedFields == null || requestedFields.Count == 0)
            {
                return fieldResults;
            }

            foreach (OcrFieldRequestDto field in requestedFields)
            {
                fieldResults.Add(new OcrFieldResultDto
                {
                    fieldName = field.fieldName,
                    fieldLabel = field.fieldLabel,
                    rawText = null,
                    suggestedText = null,
                    finalText = null,
                    confidenceScore = null,
                    pageNo = null,
                    boundingBoxX = null,
                    boundingBoxY = null,
                    boundingBoxWidth = null,
                    boundingBoxHeight = null
                });
            }

            return fieldResults;
        }
    }
}