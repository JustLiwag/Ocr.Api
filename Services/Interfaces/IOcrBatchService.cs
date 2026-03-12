using Microsoft.AspNetCore.Http;
using Ocr.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Interfaces
{
    /*
        =========================================================
        IOcrBatchService
        ---------------------------------------------------------
        Purpose:
        - Handles batch OCR processing
        - Accepts multiple uploaded files plus shared request metadata
        - Creates batch job and batch job item records
        - Reuses the single-file OCR flow internally
        - Returns summarized batch processing results
        =========================================================
    */

    public interface IOcrBatchService
    {
        /*
            Processes multiple uploaded files as one batch OCR request.

            Expected responsibilities of the implementation:
            - Validate request metadata
            - Create batch job record
            - Create batch job item records
            - Loop through uploaded files
            - Call the single-file OCR process internally
            - Update processed and failed counters
            - Return summarized batch result
        */
        Task<OcrBatchProcessResponseDto> processBatchAsync(List<IFormFile> files, OcrBatchProcessRequestDto request);

        /*
            Processes multiple files as one batch OCR request using raw byte content.

            This overload is useful if your OCR pipeline or upstream caller
            already provides file content as byte arrays instead of IFormFile.
        */
        Task<OcrBatchProcessResponseDto> processBatchAsync(List<OcrBatchFileRequestDto> files, OcrBatchProcessRequestDto request);
    }

    /*
        =========================================================
        OcrBatchFileRequestDto
        ---------------------------------------------------------
        Helper DTO for batch processing using raw file bytes.
        This is separate from OcrBatchFileItemDto because this one
        includes the actual file content needed for processing.
        =========================================================
    */

    public class OcrBatchFileRequestDto
    {
        public string fileName { get; set; } = string.Empty;
        public byte[] fileBytes { get; set; } = new byte[0];
        public string? externalDocumentId { get; set; }
        public string? externalRecordId { get; set; }
    }
}