using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Ocr.Api.Models.Records;
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
        // docTR service for testing and comparison
        private readonly IDocTrService _docTrService;
        private readonly ISearchablePdfBuilderService _searchablePdfBuilderService;
        private readonly IDocTrPersistenceService _docTrPersistenceService;

        public OcrController(
            ITempFileService tempFileService,
            IPdfTextDetector pdfTextDetector,
            IPdfRenderService renderService,
            ITesseractService tesseractService,
            IPdfMergeService pdfMergeService,
            IImagePreprocessingService imagePreprocessingService,
            IConfiguration config,
            IOcrPipelineService ocrPipelineService,
            IDocTrService docTrService,
            ISearchablePdfBuilderService searchablePdfBuilderService,
            IDocTrPersistenceService docTrPersistenceService)
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
            _searchablePdfBuilderService = searchablePdfBuilderService;
            _docTrPersistenceService = docTrPersistenceService;
        }

        [HttpPost("doctr-build-page")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RunDocTrBuildPage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".tif" && ext != ".tiff" && ext != ".webp")
                return BadRequest("Please upload an image file.");

            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "OCR Test Data",
                "results",
                "doctr_test"
            );

            Directory.CreateDirectory(rootDir);

            var safeName = string.Concat(
                Path.GetFileNameWithoutExtension(file.FileName)
                    .Split(Path.GetInvalidFileNameChars())
            );

            var tempJobDir = _tempFileService.CreateJobDirectory("doctr_build_page");
            var inputPath = await _tempFileService.SaveFileAsync(file, tempJobDir);

            try
            {
                var doctrResult = await _docTrService.RunOcrAsync(inputPath);

                var outputPdfPath = Path.Combine(rootDir, $"{safeName}_doctr.pdf");

                var builtPdf = await _searchablePdfBuilderService.BuildPagePdfAsync(
                    inputPath,
                    doctrResult.Words,
                    outputPdfPath
                );

                var response = new Ocr.Api.Models.Api.DocTrBuildPageImageResponseDto
                {
                    Engine = doctrResult.Engine,
                    ImagePath = doctrResult.ImagePath,
                    Confidence = doctrResult.Confidence,
                    FullText = doctrResult.FullText,
                    WordCount = doctrResult.Words.Count,
                    JsonPath = doctrResult.JsonPath,
                    OutputPdf = builtPdf
                };

                return Ok(response);
            }
            finally
            {
                bool cleanupTemps = _config.GetValue<bool>("Ocr:CleanupIntermediateFiles");
                if (cleanupTemps)
                    _tempFileService.DeleteDirectoryIfExists(tempJobDir, true);
            }
        }

        [HttpPost("doctr-build-pdf-page")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RunDocTrBuildPdfPage(IFormFile file, int pageNumber = 1)
        {
            var stopwatch = Stopwatch.StartNew();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Please upload a PDF file.");

            if (pageNumber <= 0)
                return BadRequest("Page number must be greater than zero.");

            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "OCR Test Data",
                "results",
                "doctr_pdf_test"
            );

            Directory.CreateDirectory(rootDir);

            var safeName = string.Concat(
                Path.GetFileNameWithoutExtension(file.FileName)
                    .Split(Path.GetInvalidFileNameChars())
            );

            var tempJobDir = _tempFileService.CreateJobDirectory("doctr_build_pdf_page");
            var inputPath = await _tempFileService.SaveFileAsync(file, tempJobDir);

            try
            {
                int pageCount = await _renderService.GetPageCountAsync(inputPath);

                if (pageNumber > pageCount)
                    return BadRequest($"Requested page {pageNumber} exceeds PDF page count of {pageCount}.");

                int renderDpi = GetRenderDpi(pageCount);

                string renderedImagePath = await _renderService.RenderPageAsync(
                    inputPath,
                    tempJobDir,
                    pageNumber,
                    renderDpi
                );

                var preprocessingOptions = new ImagePreprocessingOptions
                {
                    Enabled = true,
                    Profile = pageCount > 75 ? "light" : "default",
                    OverwriteIfExists = true
                };

                string processedImagePath = _imagePreprocessingService.Preprocess(
                    renderedImagePath,
                    preprocessingOptions
                );

                var doctrResult = await _docTrService.RunOcrAsync(processedImagePath);

                string documentId = $"{safeName}_{DateTime.Now:yyyyMMddHHmmss}";

                await _docTrPersistenceService.SaveDocumentAsync(new Ocr.Api.Models.Records.DocTrDocumentRecord
                {
                    DocumentId = documentId,
                    FileName = file.FileName,
                    PageCount = pageCount,
                    Engine = "docTR"
                });

                string pageImageDir = Path.Combine(rootDir, "page_images", documentId);
                Directory.CreateDirectory(pageImageDir);

                string savedPageImagePath = Path.Combine(
                    pageImageDir,
                    $"page_{pageNumber:000}{Path.GetExtension(processedImagePath)}"
                );

                System.IO.File.Copy(processedImagePath, savedPageImagePath, true);

                var pageRecord = new Ocr.Api.Models.Records.DocTrPageRecord
                {
                    DocumentId = documentId,
                    PageNumber = pageNumber,
                    Engine = doctrResult.Engine,
                    SourceImagePath = savedPageImagePath,
                    FullText = doctrResult.FullText,
                    Confidence = doctrResult.Confidence,
                    ReviewStatus = "NotReviewed",
                    ReviewedBy = null,
                    ReviewedAt = null
                };

                await _docTrPersistenceService.SavePageAsync(pageRecord);

                var wordRecords = doctrResult.Words
                    .Select((word, index) => new Ocr.Api.Models.Records.DocTrWordRecord
                    {
                        DocumentId = documentId,
                        PageNumber = pageNumber,
                        WordOrder = index + 1,
                        RawText = word.Text,
                        Confidence = word.Confidence,
                        XMin = word.XMin,
                        YMin = word.YMin,
                        XMax = word.XMax,
                        YMax = word.YMax
                    })
                    .ToList();

                await _docTrPersistenceService.SaveWordsAsync(wordRecords);

                var outputPdfPath = Path.Combine(
                    rootDir,
                    $"{safeName}_page_{pageNumber:000}_doctr.pdf"
                );

                var builtPdf = await _searchablePdfBuilderService.BuildPagePdfAsync(
                    savedPageImagePath,
                    doctrResult.Words,
                    outputPdfPath
                );

                float averageConfidence = doctrResult.Words.Count > 0
                ? doctrResult.Words.Average(w => w.Confidence) * 100f
                : doctrResult.Confidence * 100f;

                string quality = GetQualityLabel(averageConfidence);

                stopwatch.Stop();

                var response = new Ocr.Api.Models.Api.DocTrBuildPageResponseDto
                {
                    DocumentId = documentId,
                    File = file.FileName,
                    PageNumber = pageNumber,
                    PageCount = pageCount,
                    RenderDpi = renderDpi,
                    Engine = doctrResult.Engine,
                    EngineConfidence = doctrResult.Confidence,
                    OcrConfidence = (float)Math.Round(averageConfidence, 2),
                    Quality = quality,
                    FullText = doctrResult.FullText,
                    WordCount = doctrResult.Words.Count,
                    SourceImagePath = savedPageImagePath,
                    OutputPdf = builtPdf,
                    TimeElapsed = stopwatch.Elapsed.ToString(@"hh\:mm\:ss")
                };

                return Ok(response);
            }
            finally
            {
                bool cleanupTemps = _config.GetValue<bool>("Ocr:CleanupIntermediateFiles");
                if (cleanupTemps)
                    _tempFileService.DeleteDirectoryIfExists(tempJobDir, true);
            }
        }

        [HttpPost("doctr-rebuild-page")]
        public async Task<IActionResult> RebuildDocTrPage(string documentId, int pageNumber)
        {
            var stopwatch = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest("documentId is required.");

            if (pageNumber <= 0)
                return BadRequest("pageNumber must be greater than zero.");

            var page = await _docTrPersistenceService.GetPageAsync(documentId, pageNumber);
            if (page == null)
                return NotFound("Page record not found.");

            var words = await _docTrPersistenceService.GetWordsAsync(documentId, pageNumber);
            if (words == null || words.Count == 0)
                return NotFound("No persisted words found for the specified page.");

            if (string.IsNullOrWhiteSpace(page.SourceImagePath) || !System.IO.File.Exists(page.SourceImagePath))
                return NotFound("Source page image not found for rebuild.");

            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "OCR Test Data",
                "results",
                "doctr_rebuild"
            );

            Directory.CreateDirectory(rootDir);

            var wordsForPdf = words
                .OrderBy(w => w.WordOrder)
                .Select(w => new Ocr.Api.Models.DocTrWordResult
                {
                    Text = w.FinalText,
                    Confidence = w.Confidence,
                    XMin = w.XMin,
                    YMin = w.YMin,
                    XMax = w.XMax,
                    YMax = w.YMax
                })
                .ToList();

            var outputPdfPath = Path.Combine(
                rootDir,
                $"{documentId}_page_{pageNumber:000}_rebuilt.pdf"
            );

            var rebuiltPdf = await _searchablePdfBuilderService.BuildPagePdfAsync(
                page.SourceImagePath,
                wordsForPdf,
                outputPdfPath
            );

            float averageConfidence = words.Count > 0
                ? words.Average(w => w.Confidence) * 100f
                : 0f;

            string quality = GetQualityLabel(averageConfidence);

            stopwatch.Stop();

            var response = new Ocr.Api.Models.Api.DocTrRebuildPageResponseDto
            {
                DocumentId = documentId,
                PageNumber = pageNumber,
                WordCount = words.Count,
                CorrectedWords = words.Count(w => !string.IsNullOrWhiteSpace(w.CorrectedText)),
                OcrConfidence = (float)Math.Round(averageConfidence, 2),
                Quality = quality,
                OutputPdf = rebuiltPdf,
                TimeElapsed = stopwatch.Elapsed.ToString(@"hh\:mm\:ss")
            };

            return Ok(response);
        }

        [HttpGet("doctr-page")]
        public async Task<IActionResult> GetDocTrPage(string documentId, int pageNumber)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest("documentId is required.");

            if (pageNumber <= 0)
                return BadRequest("pageNumber must be greater than zero.");

            var page = await _docTrPersistenceService.GetPageAsync(documentId, pageNumber);
            if (page == null)
                return NotFound("Page record not found.");

            var words = await _docTrPersistenceService.GetWordsAsync(documentId, pageNumber);

            float averageConfidence = words.Count > 0
                ? words.Average(w => w.Confidence) * 100f
                : page.Confidence * 100f;

            var response = new Ocr.Api.Models.Api.DocTrPageResponseDto
            {
                Page = new Ocr.Api.Models.Api.DocTrPageDto
                {
                    DocumentId = page.DocumentId,
                    PageNumber = page.PageNumber,
                    Engine = page.Engine,
                    SourceImagePath = page.SourceImagePath,
                    FullText = page.FullText,
                    Confidence = page.Confidence,
                    OcrConfidence = (float)Math.Round(averageConfidence, 2),
                    Quality = GetQualityLabel(averageConfidence),
                    ReviewStatus = page.ReviewStatus,
                    ReviewedBy = page.ReviewedBy,
                    ReviewedAt = page.ReviewedAt,
                    CreatedAt = page.CreatedAt
                },
                Words = words.Select(w => new Ocr.Api.Models.Api.DocTrWordDto
                {
                    WordOrder = w.WordOrder,
                    RawText = w.RawText,
                    CorrectedText = w.CorrectedText,
                    FinalText = w.FinalText,
                    Confidence = w.Confidence,
                    XMin = w.XMin,
                    YMin = w.YMin,
                    XMax = w.XMax,
                    YMax = w.YMax
                }).ToList()
            };

            return Ok(response);
        }

        [HttpPost("doctr-corrections")]
        [Consumes("application/json")]
        public async Task<IActionResult> SaveDocTrCorrections(
        [FromQuery] string documentId,
        [FromQuery] int pageNumber,
        [FromBody] List<DocTrWordCorrectionRequest> corrections)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest("documentId is required.");

            if (pageNumber <= 0)
                return BadRequest("pageNumber must be greater than zero.");

            if (corrections == null || corrections.Count == 0)
                return BadRequest("No corrections submitted.");

            await _docTrPersistenceService.SaveCorrectionsAsync(documentId, pageNumber, corrections);

            var words = await _docTrPersistenceService.GetWordsAsync(documentId, pageNumber);

            var response = new Ocr.Api.Models.Api.DocTrCorrectionsResponseDto
            {
                DocumentId = documentId,
                PageNumber = pageNumber,
                CorrectedWords = words.Count(w => !string.IsNullOrWhiteSpace(w.CorrectedText)),
                Words = words.Select(w => new Ocr.Api.Models.Api.DocTrWordDto
                {
                    WordOrder = w.WordOrder,
                    RawText = w.RawText,
                    CorrectedText = w.CorrectedText,
                    FinalText = w.FinalText,
                    Confidence = w.Confidence,
                    XMin = w.XMin,
                    YMin = w.YMin,
                    XMax = w.XMax,
                    YMax = w.YMax
                }).ToList()
            };

            return Ok(response);
        }

        [HttpPost("doctr-rebuild-document")]
        public async Task<IActionResult> RebuildDocTrDocument(string documentId)
        {
            var stopwatch = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest("documentId is required.");

            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "OCR Test Data",
                "results",
                "doctr_rebuild_document"
            );

            Directory.CreateDirectory(rootDir);

            string documentOutputDir = Path.Combine(rootDir, documentId);
            Directory.CreateDirectory(documentOutputDir);

            var rebuiltPagePdfPaths = new List<string>();
            var confidenceScores = new List<float>();
            int pageNumber = 1;

            while (true)
            {
                var page = await _docTrPersistenceService.GetPageAsync(documentId, pageNumber);
                if (page == null)
                    break;

                var words = await _docTrPersistenceService.GetWordsAsync(documentId, pageNumber);
                if (words.Count == 0)
                {
                    pageNumber++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(page.SourceImagePath) || !System.IO.File.Exists(page.SourceImagePath))
                    return NotFound($"Source image not found for page {pageNumber}.");

                var wordsForPdf = words
                    .OrderBy(w => w.WordOrder)
                    .Select(w => new Ocr.Api.Models.DocTrWordResult
                    {
                        Text = w.FinalText,
                        Confidence = w.Confidence,
                        XMin = w.XMin,
                        YMin = w.YMin,
                        XMax = w.XMax,
                        YMax = w.YMax
                    })
                    .ToList();

                string pagePdfPath = Path.Combine(
                    documentOutputDir,
                    $"{documentId}_page_{pageNumber:000}_rebuilt.pdf"
                );  

                var rebuiltPagePdf = await _searchablePdfBuilderService.BuildPagePdfAsync(
                    page.SourceImagePath,
                    wordsForPdf,
                    pagePdfPath
                );

                rebuiltPagePdfPaths.Add(rebuiltPagePdf);

                float pageConfidence = words.Count > 0
                    ? words.Average(w => w.Confidence) * 100f
                    : 0f;

                confidenceScores.Add(pageConfidence);

                pageNumber++;
            }

            if (rebuiltPagePdfPaths.Count == 0)
                return NotFound("No persisted pages found for the specified document.");

            int totalPages = rebuiltPagePdfPaths.Count;
            int chunkSize = _config.GetValue<int?>("Ocr:MergeChunkSize") ?? 25;

            string mergedPdf = totalPages > chunkSize
                ? await _pdfMergeService.MergeInChunksAsync(rebuiltPagePdfPaths, documentOutputDir, $"{documentId}_rebuilt", chunkSize)
                : await _pdfMergeService.MergeAsync(rebuiltPagePdfPaths, documentOutputDir, $"{documentId}_rebuilt");

            float overallConfidence = confidenceScores.Count > 0
                ? confidenceScores.Average()
                : 0f;

            string quality = GetQualityLabel(overallConfidence);

            int correctedWords = 0;
            int totalWords = 0;

            for (int i = 1; i <= totalPages; i++)
            {
                var pageWords = await _docTrPersistenceService.GetWordsAsync(documentId, i);
                totalWords += pageWords.Count;
                correctedWords += pageWords.Count(w => !string.IsNullOrWhiteSpace(w.CorrectedText));
            }

            stopwatch.Stop();

            var response = new Ocr.Api.Models.Api.DocTrRebuildDocumentResponseDto
            {
                DocumentId = documentId,
                Pages = totalPages,
                WordCount = totalWords,
                CorrectedWords = correctedWords,
                OcrConfidence = (float)Math.Round(overallConfidence, 2),
                Quality = quality,
                OutputPdf = mergedPdf,
                TimeElapsed = stopwatch.Elapsed.ToString(@"hh\:mm\:ss")
            };

            return Ok(response);
        }

        [HttpPost("doctr-build-document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RunDocTrBuildDocument(IFormFile file)
        {
            var stopwatch = Stopwatch.StartNew();

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Please upload a PDF file.");

            string rootDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "OCR Test Data",
                "results",
                "doctr_document"
            );

            Directory.CreateDirectory(rootDir);

            var safeName = string.Concat(
                Path.GetFileNameWithoutExtension(file.FileName)
                    .Split(Path.GetInvalidFileNameChars())
            );

            string documentId = $"{safeName}_{DateTime.Now:yyyyMMddHHmmss}";
            string documentOutputDir = Path.Combine(rootDir, documentId);
            string pageImageDir = Path.Combine(documentOutputDir, "page_images");
            string pagePdfDir = Path.Combine(documentOutputDir, "page_pdfs");

            Directory.CreateDirectory(documentOutputDir);
            Directory.CreateDirectory(pageImageDir);
            Directory.CreateDirectory(pagePdfDir);

            var tempJobDir = _tempFileService.CreateJobDirectory("doctr_build_document");
            var inputPath = await _tempFileService.SaveFileAsync(file, tempJobDir);

            var pagePdfPaths = new List<string>();
            var pageConfidenceScores = new List<float>();

            try
            {
                int pageCount = await _renderService.GetPageCountAsync(inputPath);
                int renderDpi = GetRenderDpi(pageCount);
                bool largeDocument = pageCount > 75;

                await _docTrPersistenceService.SaveDocumentAsync(new Ocr.Api.Models.Records.DocTrDocumentRecord
                {
                    DocumentId = documentId,
                    FileName = file.FileName,
                    PageCount = pageCount,
                    Engine = "docTR"
                });

                for (int pageNumber = 1; pageNumber <= pageCount; pageNumber++)
                {
                    string renderedImagePath = await _renderService.RenderPageAsync(
                        inputPath,
                        tempJobDir,
                        pageNumber,
                        renderDpi
                    );

                    var preprocessingOptions = new ImagePreprocessingOptions
                    {
                        Enabled = true,
                        Profile = largeDocument ? "light" : "default",
                        OverwriteIfExists = true
                    };

                    string processedImagePath = _imagePreprocessingService.Preprocess(
                        renderedImagePath,
                        preprocessingOptions
                    );

                    var doctrResult = await _docTrService.RunOcrAsync(processedImagePath);

                    string savedPageImagePath = Path.Combine(
                        pageImageDir,
                        $"page_{pageNumber:000}{Path.GetExtension(processedImagePath)}"
                    );

                    System.IO.File.Copy(processedImagePath, savedPageImagePath, true);

                    var pageRecord = new Ocr.Api.Models.Records.DocTrPageRecord
                    {
                        DocumentId = documentId,
                        PageNumber = pageNumber,
                        Engine = doctrResult.Engine,
                        SourceImagePath = savedPageImagePath,
                        FullText = doctrResult.FullText,
                        Confidence = doctrResult.Confidence,
                        ReviewStatus = "NotReviewed",
                        ReviewedBy = null,
                        ReviewedAt = null
                    };

                    await _docTrPersistenceService.SavePageAsync(pageRecord);

                    var wordRecords = doctrResult.Words
                        .Select((word, index) => new Ocr.Api.Models.Records.    DocTrWordRecord
                        {
                            DocumentId = documentId,
                            PageNumber = pageNumber,
                            WordOrder = index + 1,
                            RawText = word.Text,
                            Confidence = word.Confidence,
                            XMin = word.XMin,
                            YMin = word.YMin,
                            XMax = word.XMax,
                            YMax = word.YMax
                        })
                        .ToList();

                    await _docTrPersistenceService.SaveWordsAsync(wordRecords);

                    var pagePdfPath = Path.Combine(
                        pagePdfDir,
                        $"{documentId}_page_{pageNumber:000}.pdf"
                    );

                    var builtPagePdf = await _searchablePdfBuilderService.BuildPagePdfAsync(
                        savedPageImagePath,
                        doctrResult.Words,
                        pagePdfPath
                    );

                    pagePdfPaths.Add(builtPagePdf);

                    float pageConfidence = doctrResult.Words.Count > 0
                        ? doctrResult.Words.Average(w => w.Confidence) * 100f
                        : doctrResult.Confidence * 100f;

                    pageConfidenceScores.Add(pageConfidence);
                }

                if (pagePdfPaths.Count == 0)
                    throw new InvalidOperationException("No page PDFs were generated.");

                int chunkSize = _config.GetValue<int?>("Ocr:MergeChunkSize") ?? 25;

                string mergedPdf = pagePdfPaths.Count > chunkSize
                    ? await _pdfMergeService.MergeInChunksAsync(pagePdfPaths, documentOutputDir, documentId, chunkSize)
                    : await _pdfMergeService.MergeAsync(pagePdfPaths, documentOutputDir, documentId);

                float overallConfidence = pageConfidenceScores.Count > 0
                    ? pageConfidenceScores.Average()
                    : 0f;

                string quality = GetQualityLabel(overallConfidence);

                stopwatch.Stop();

                var response = new Ocr.Api.Models.Api.DocTrBuildDocumentResponseDto
                {
                    DocumentId = documentId,
                    File = file.FileName,
                    Pages = pageCount,
                    RenderDpi = renderDpi,
                    OcrConfidence = (float)Math.Round(overallConfidence, 2),
                    Quality = quality,
                    OutputPdf = mergedPdf,
                    TimeElapsed = stopwatch.Elapsed.ToString(@"hh\:mm\:ss")
                };

                return Ok(response);
            }
            finally
            {
                bool cleanupTemps = _config.GetValue<bool>("Ocr:CleanupIntermediateFiles");
                if (cleanupTemps)
                    _tempFileService.DeleteDirectoryIfExists(tempJobDir, true);
            }
        }

        [HttpGet("doctr-documents")]
        public async Task<IActionResult> GetDocTrDocuments(string? reviewStatus = null)
        {
            var documents = await _docTrPersistenceService.GetDocumentsAsync(reviewStatus);

            var response = new Ocr.Api.Models.Api.DocTrDocumentsResponseDto
            {
                Count = documents.Count,
                Documents = documents.Select(d => new Ocr.Api.Models.Api.DocTrDocumentListItemDto
                {
                    DocumentId = d.DocumentId,
                    FileName = d.FileName,
                    PageCount = d.PageCount,
                    Engine = d.Engine,
                    CreatedAt = d.CreatedAt,
                    OcrConfidence = d.OcrConfidence,
                    Quality = d.Quality,
                    ReviewStatus = d.ReviewStatus,
                    ReviewedPages = d.ReviewedPages ?? 0,
                    CorrectedWords = d.CorrectedWords ?? 0
                }).ToList()
            };

            return Ok(response);
        }

        [HttpGet("doctr-document")]
        public async Task<IActionResult> GetDocTrDocument(string documentId)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest("documentId is required.");

            var document = await _docTrPersistenceService.GetDocumentAsync(documentId);
            if (document == null)
                return NotFound("Document record not found.");

            var pageNumbers = await _docTrPersistenceService.GetPageNumbersAsync(documentId);

            if (pageNumbers.Count == 0)
                return NotFound("No persisted document found for the specified documentId.");

            var pageSummaries = new List<Ocr.Api.Models.Api.DocTrPageSummaryDto>();
            int totalWords = 0;
            int correctedWords = 0;
            var confidenceScores = new List<float>();

            int reviewedPages = 0;
            int needsRecheckPages = 0;
            int notReviewedPages = 0;

            foreach (var pageNumber in pageNumbers)
            {
                var page = await _docTrPersistenceService.GetPageAsync(documentId, pageNumber);
                var words = await _docTrPersistenceService.GetWordsAsync(documentId, pageNumber);

                int pageWordCount = words.Count;
                int pageCorrectedWords = words.Count(w => !string.IsNullOrWhiteSpace(w.CorrectedText));
                float pageConfidence = pageWordCount > 0
                    ? words.Average(w => w.Confidence) * 100f
                    : 0f;

                string reviewStatus = page?.ReviewStatus ?? "NotReviewed";

                if (string.Equals(reviewStatus, "Reviewed", StringComparison.OrdinalIgnoreCase))
                    reviewedPages++;
                else if (string.Equals(reviewStatus, "NeedsRecheck", StringComparison.OrdinalIgnoreCase))
                    needsRecheckPages++;
                else
                    notReviewedPages++;

                totalWords += pageWordCount;
                correctedWords += pageCorrectedWords;
                confidenceScores.Add(pageConfidence);

                pageSummaries.Add(new Ocr.Api.Models.Api.DocTrPageSummaryDto
                {
                    PageNumber = pageNumber,
                    Engine = page?.Engine ?? "docTR",
                    WordCount = pageWordCount,
                    CorrectedWords = pageCorrectedWords,
                    OcrConfidence = (float)Math.Round(pageConfidence, 2),
                    Quality = GetQualityLabel(pageConfidence),
                    ReviewStatus = reviewStatus,
                    ReviewedBy = page?.ReviewedBy,
                    ReviewedAt = page?.ReviewedAt,
                    SourceImagePath = page?.SourceImagePath ?? string.Empty
                });
            }

            float overallConfidence = confidenceScores.Count > 0
                ? confidenceScores.Average()
                : 0f;

            string overallStatus;

            if (needsRecheckPages > 0)
                overallStatus = "NeedsRecheck";
            else if (reviewedPages == pageNumbers.Count)
                overallStatus = "Reviewed";
            else if (reviewedPages > 0)
                overallStatus = "PartiallyReviewed";
            else
                overallStatus = "NotReviewed";

            var response = new Ocr.Api.Models.Api.DocTrDocumentResponseDto
            {
                DocumentId = document.DocumentId,
                FileName = document.FileName,
                Engine = document.Engine,
                DeclaredPageCount = document.PageCount,
                Pages = pageNumbers.Count,
                ReviewedPages = reviewedPages,
                NeedsRecheckPages = needsRecheckPages,
                NotReviewedPages = notReviewedPages,
                WordCount = totalWords,
                CorrectedWords = correctedWords,
                OcrConfidence = (float)Math.Round(overallConfidence, 2),
                Quality = GetQualityLabel(overallConfidence),
                ReviewStatus = overallStatus,
                CreatedAt = document.CreatedAt,
                PageSummaries = pageSummaries
            };

            return Ok(response);
        }

        [HttpGet("doctr-correction-history")]
        public async Task<IActionResult> GetDocTrCorrectionHistory(string documentId, int pageNumber, int wordOrder)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest("documentId is required.");

            if (pageNumber <= 0)
                return BadRequest("pageNumber must be greater than zero.");

            if (wordOrder <= 0)
                return BadRequest("wordOrder must be greater than zero.");

            var history = await _docTrPersistenceService.GetCorrectionHistoryAsync(documentId, pageNumber, wordOrder);

            var response = new Ocr.Api.Models.Api.DocTrCorrectionHistoryResponseDto
            {
                DocumentId = documentId,
                PageNumber = pageNumber,
                WordOrder = wordOrder,
                History = history.Select(h => new Ocr.Api.Models.Api.DocTrCorrectionHistoryItemDto
                {
                    HistoryId = h.HistoryId,
                    DocumentId = h.DocumentId,
                    PageNumber = h.PageNumber,
                    WordOrder = h.WordOrder,
                    OldText = h.OldText,
                    NewText = h.NewText,
                    CorrectedAt = h.CorrectedAt,
                    CorrectedBy = h.CorrectedBy
                }).ToList()
            };

            return Ok(response);
        }   

        [HttpPost("doctr-page-review")]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateDocTrPageReview(
        [FromQuery] string documentId,
        [FromQuery] int pageNumber,
        [FromBody] DocTrPageReviewRequest request)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                return BadRequest("documentId is required.");

            if (pageNumber <= 0)
                return BadRequest("pageNumber must be greater than zero.");

            if (request == null || string.IsNullOrWhiteSpace(request.ReviewStatus))
                return BadRequest("ReviewStatus is required.");

            await _docTrPersistenceService.UpdatePageReviewAsync(
                documentId,
                pageNumber,
                request.ReviewStatus,
                request.ReviewedBy
            );

            var page = await _docTrPersistenceService.GetPageAsync(documentId, pageNumber);
            if (page == null)
                return NotFound("Page record not found.");

            var words = await _docTrPersistenceService.GetWordsAsync(documentId, pageNumber);

            float averageConfidence = words.Count > 0
                ? words.Average(w => w.Confidence) * 100f
                : page.Confidence * 100f;

            string quality = GetQualityLabel(averageConfidence);

            var response = new Ocr.Api.Models.Api.DocTrPageReviewResponseDto
            {
                DocumentId = documentId,
                PageNumber = pageNumber,
                Page = new Ocr.Api.Models.Api.DocTrPageDto
                {
                    DocumentId = page.DocumentId,
                    PageNumber = page.PageNumber,
                    Engine = page.Engine,
                    SourceImagePath = page.SourceImagePath,
                    FullText = page.FullText,
                    Confidence = page.Confidence,
                    OcrConfidence = (float)Math.Round(averageConfidence, 2),
                    Quality = quality,
                    ReviewStatus = page.ReviewStatus,
                    ReviewedBy = page.ReviewedBy,
                    ReviewedAt = page.ReviewedAt,
                    CreatedAt = page.CreatedAt
                }
            };

            return Ok(response);
        }
        /*
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
        */
        /*
        [HttpPost("manual")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RunManualOcr(IFormFile file)
        {
            var result = await ProcessSingleFileAsync(file);
            return Ok(result);
        }
        /*
        /*
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
        */
        /*
        [HttpPost("merge-searchable")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> MergeSearchable(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded.");

            var result = await _ocrPipelineService.MergeSearchableAsync(files);
            return Ok(result);
        }
        */

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