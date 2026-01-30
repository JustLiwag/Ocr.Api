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
        private readonly ILogger<OcrController> _logger;
        private readonly IPdfAnalysisService _pdfAnalysisService;

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
        [RequestSizeLimit(104_857_600)]
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

            string tempFilePath = null;

            try
            {
                tempFilePath = await _tempFileService.SaveFileAsync(request.File);

                if (string.IsNullOrWhiteSpace(tempFilePath) || !System.IO.File.Exists(tempFilePath))
                {
                    return StatusCode(500, new OcrErrorResponse
                    {
                        Error = "TEMP_FILE_ERROR",
                        Message = "Failed to save uploaded PDF to temp folder."
                    });
                }

                var analysis = _pdfAnalysisService.Analyze(tempFilePath);

                if (analysis.IsSearchable && !request.ForceOcr)
                {
                    var bytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                    return File(bytes, "application/pdf", request.File.FileName);
                }

                // TODO: Pass tempFilePath to OCR pipeline for image-only pages

                // For now, just return the original PDF
                var fileBytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
                return File(fileBytes, "application/pdf", request.File.FileName);
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
                if (tempFilePath != null && System.IO.File.Exists(tempFilePath))
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { /* ignore */ }
                }
            }
        }
    }
}
