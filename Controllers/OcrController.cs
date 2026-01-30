using Microsoft.AspNetCore.Mvc;
using Ocr.Api.Contracts.Requests;
using Ocr.Api.Contracts.Responses;
using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.Pdf;

namespace Ocr.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly ITempFileService _tempFileService;
        private readonly IPdfAnalysisService _pdfAnalysisService;
        private readonly ILogger<OcrController> _logger;

        public OcrController(
            ITempFileService tempFileService,
            IPdfAnalysisService pdfAnalysisService,
            ILogger<OcrController> logger)
        {
            _tempFileService = tempFileService;
            _pdfAnalysisService = pdfAnalysisService;
            _logger = logger;
        }

        [HttpPost("pdf/searchable")]
        [RequestSizeLimit(104_857_600)] // 100 MB
        public async Task<IActionResult> MakePdfSearchable([FromForm] OcrPdfRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest(new OcrErrorResponse
                {
                    Error = "NO_FILE",
                    Message = "No PDF file uploaded."
                });
            }

            var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                return StatusCode(415, new OcrErrorResponse
                {
                    Error = "UNSUPPORTED_FORMAT",
                    Message = "Only PDF files are supported."
                });
            }

            string tempFilePath = await _tempFileService.SaveFileAsync(request.File);

            try
            {
                // Save uploaded file to temp folder
                tempFilePath = await _tempFileService.SaveFileAsync(request.File);

                if (string.IsNullOrWhiteSpace(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                {
                    return StatusCode(500, new OcrErrorResponse
                    {
                        Error = "TEMP_FILE_ERROR",
                        Message = "Failed to save uploaded PDF to temp folder."
                    });
                }

                // Analyze PDF pages
                var analysis = _pdfAnalysisService.Analyze(tempFilePath);

                // Log analysis for debugging
                foreach (var page in analysis.Pages)
                {
                    _logger.LogInformation($"Page {page.PageNumber}: " +
                                           (page.HasText ? "Text Found" : "Image Only"));
                }

                // If PDF is fully searchable and ForceOcr is false, skip OCR
                if (analysis.IsSearchable && !request.ForceOcr)
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                    return File(bytes, "application/pdf", request.File.FileName);
                }

                // Phase 3: Send image-only pages to OCR (not implemented yet)
                // For now, return original PDF as placeholder
                var outputBytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                return File(outputBytes, "application/pdf", request.File.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDF");
                return StatusCode(500, new OcrErrorResponse
                {
                    Error = "INTERNAL_ERROR",
                    Message = "An unexpected error occurred while processing the PDF."
                });
            }
            finally
            {
                // Clean up temp file
                if (tempFilePath != null && System.IO.File.Exists(tempFilePath))
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                }
            }
        }
    }
}
