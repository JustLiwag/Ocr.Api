using Microsoft.AspNetCore.Http;
using Ocr.Api.Models;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Interfaces
{
    /*
        =========================================================
        IOcrProcessingService
        ---------------------------------------------------------
        Purpose:
        - Handles single-file OCR processing
        - Accepts uploaded file and request metadata
        - Produces OCR result data and searchable PDF output
        - Creates the main OCR document/artifact records
        =========================================================
    */

    public interface IOcrProcessingService
    {
        /*
            Processes a single uploaded file and returns the OCR result.

            Expected responsibilities of the implementation:
            - Validate request metadata
            - Validate uploaded file
            - Run OCR pipeline
            - Generate searchable PDF
            - Save OCR document metadata
            - Save OCR artifact metadata
            - Return OCR response DTO
        */
        Task<OcrProcessResponseDto> processSingleAsync(IFormFile file, OcrProcessRequestDto request);

        /*
            Processes a single uploaded file and returns the OCR result.

            This overload is useful if your existing OCR pipeline already
            works with raw file bytes instead of IFormFile.
        */
        Task<OcrProcessResponseDto> processSingleAsync(byte[] fileBytes, string fileName, OcrProcessRequestDto request);
    }
}