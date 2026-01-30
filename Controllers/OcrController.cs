using Microsoft.AspNetCore.Mvc;
using Ocr.Api.Contracts.Requests;
using Ocr.Api.Contracts.Responses;
using Ocr.Api.Services.FileStorage;

namespace Ocr.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly ITempFileService _tempFileService;
        private readonly ILogger<OcrController> _logger;

        public OcrController(ITempFileService tempFileService, ILogger<OcrController> logger)
        {
            _tempFileService = tempFileService;
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

            // Validate file extension
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
                // Save file to temp folder
                tempFilePath = await _tempFileService.SaveFileAsync(request.File);

                // For Phase 1: just return the same PDF
                var fileBytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);

                return File(fileBytes, "application/pdf", request.File.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving PDF");
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
                    try { System.IO.File.Delete(tempFilePath); } catch { /* ignore */ }
                }
            }
        }
    }
}
