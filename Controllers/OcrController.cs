using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ocr.Api.Models;
using Ocr.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Controllers
{
    [ApiController]
    [Route("api/ocr")]
    public class OcrController : ControllerBase
    {
        private readonly IOcrProcessingService _ocrProcessingService;
        private readonly IOcrBatchService _ocrBatchService;
        private readonly IOcrDocumentService _ocrDocumentService;
        private readonly IOcrReviewService _ocrReviewService;
        private readonly IOcrSuggestionService _ocrSuggestionService;
        private readonly IDashboardService _dashboardService;

        public OcrController(
            IOcrProcessingService ocrProcessingService,
            IOcrBatchService ocrBatchService,
            IOcrDocumentService ocrDocumentService,
            IOcrReviewService ocrReviewService,
            IOcrSuggestionService ocrSuggestionService,
            IDashboardService dashboardService)
        {
            _ocrProcessingService = ocrProcessingService;
            _ocrBatchService = ocrBatchService;
            _ocrDocumentService = ocrDocumentService;
            _ocrReviewService = ocrReviewService;
            _ocrSuggestionService = ocrSuggestionService;
            _dashboardService = dashboardService;
        }

        /*
            =========================================================
            SINGLE FILE OCR
            =========================================================
        */

        [HttpPost("process")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> processSingle(
            [FromForm] IFormFile file,
            [FromForm] string sourceSystem,
            [FromForm] string officeCode,
            [FromForm] string documentTypeCode,
            [FromForm] string sensitivityLevel,
            [FromForm] string? externalDocumentId,
            [FromForm] string? externalRecordId,
            [FromForm] string? requestUserId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No file uploaded."
                    });
                }

                OcrProcessRequestDto request = new OcrProcessRequestDto
                {
                    sourceSystem = sourceSystem,
                    officeCode = officeCode,
                    documentTypeCode = documentTypeCode,
                    sensitivityLevel = sensitivityLevel,
                    externalDocumentId = externalDocumentId,
                    externalRecordId = externalRecordId,
                    originalFileName = file.FileName,
                    requestUserId = requestUserId,
                    returnSearchablePdfAsBase64 = false
                };

                OcrProcessResponseDto response = await _ocrProcessingService.processSingleAsync(file, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to process OCR file.",
                    error = ex.Message
                });
            }
        }

        /*
            =========================================================
            BATCH OCR
            =========================================================
        */

        [HttpPost("process/batch")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> processBatch(
            [FromForm] List<IFormFile> files,
            [FromForm] string sourceSystem,
            [FromForm] string officeCode,
            [FromForm] string documentTypeCode,
            [FromForm] string sensitivityLevel,
            [FromForm] string? requestUserId)
        {
            try
            {
                if (files == null || files.Count == 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "No files uploaded."
                    });
                }

                OcrBatchProcessRequestDto request = new OcrBatchProcessRequestDto
                {
                    sourceSystem = sourceSystem,
                    officeCode = officeCode,
                    documentTypeCode = documentTypeCode,
                    sensitivityLevel = sensitivityLevel,
                    requestUserId = requestUserId
                };

                OcrBatchProcessResponseDto response = await _ocrBatchService.processBatchAsync(files, request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to process batch OCR request.",
                    error = ex.Message
                });
            }
        }

        /*
            =========================================================
            OCR DOCUMENTS
            =========================================================
        */

        [HttpGet("documents/{ocrDocumentId:long}")]
        public async Task<IActionResult> getDocument(long ocrDocumentId)
        {
            try
            {
                OcrDocumentDetailDto? result = await _ocrDocumentService.getByIdAsync(ocrDocumentId);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "OCR document not found."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to retrieve OCR document.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("documents/external/{externalDocumentId}")]
        public async Task<IActionResult> getDocumentByExternalId(string externalDocumentId)
        {
            try
            {
                OcrDocumentDetailDto? result = await _ocrDocumentService.getByExternalDocumentIdAsync(externalDocumentId);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "OCR document not found."
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to retrieve OCR document by external document ID.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("documents/search")]
        public async Task<IActionResult> searchDocuments([FromBody] OcrDocumentFilterDto filter)
        {
            try
            {
                PagedResultDto<OcrDocumentDetailDto> result = await _ocrDocumentService.getPagedAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to search OCR documents.",
                    error = ex.Message
                });
            }
        }

        /*
            =========================================================
            REVIEW
            =========================================================
        */

        [HttpPost("review")]
        public async Task<IActionResult> submitReview([FromBody] OcrReviewSubmitRequestDto request)
        {
            try
            {
                OcrReviewSubmitResponseDto response = await _ocrReviewService.submitReviewAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to submit OCR review.",
                    error = ex.Message
                });
            }
        }

        /*
            =========================================================
            SUGGESTIONS
            =========================================================
        */

        [HttpPost("suggest")]
        public async Task<IActionResult> getSuggestions([FromBody] OcrSuggestionRequestDto request)
        {
            try
            {
                OcrSuggestionResponseDto response = await _ocrSuggestionService.getSuggestionsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get OCR suggestions.",
                    error = ex.Message
                });
            }
        }

        /*
            =========================================================
            DASHBOARD
            =========================================================
        */

        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> getDashboardSummary()
        {
            try
            {
                OcrDashboardSummaryDto response = await _dashboardService.getSummaryAsync();
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to load OCR dashboard summary.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("dashboard/offices/{officeCode}")]
        public async Task<IActionResult> getOfficeDashboard(string officeCode)
        {
            try
            {
                OcrOfficeDashboardDto response = await _dashboardService.getOfficeSummaryAsync(officeCode);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to load OCR office dashboard.",
                    error = ex.Message
                });
            }
        }
    }
}