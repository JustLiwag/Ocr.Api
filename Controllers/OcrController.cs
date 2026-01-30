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
        [RequestSizeLimit(104_857_600)] // 100 MB limit
        public async Task<IActionResult> MakePdfSearchable([FromForm] OcrPdfRequest request)
        {
            // 1️⃣ Validate input
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

            string tempFilePath = null;

            try
            {
                // 2️⃣ Save file to temp folder
                tempFilePath = await _tempFileService.SaveFileAsync(request.File);

                if (string.IsNullOrWhiteSpace(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                {
                    return StatusCode(500, new OcrErrorResponse
                    {
                        Error = "TEMP_FILE_ERROR",
                        Message = "Failed to save uploaded PDF to temp folder."
                    });
                }

                // 3️⃣ Analyze PDF pages
                var analysis = _pdfAnalysisService.Analyze(tempFilePath);

                // 4️⃣ Log analysis for debugging / UI feedback
                foreach (var page in analysis.Pages)
                {
                    _logger.LogInformation($"Page {page.PageNumber}: " +
                                           (page.HasText ? "Text Found" : "Image Only"));
                }

                // 5️⃣ Skip OCR if fully searchable
                if (analysis.IsSearchable && !request.ForceOcr)
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                    return File(bytes, "application/pdf", request.File.FileName);
                }

                // 6️⃣ Placeholder for Phase 3 OCR
                // Here we will later extract image-only pages and pass to Tesseract
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
                // 7️⃣ Clean up temp file
                if (tempFilePath != null && System.IO.File.Exists(tempFilePath))
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                }
            }
        }
    }
}
