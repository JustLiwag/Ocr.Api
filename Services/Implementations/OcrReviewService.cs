using Ocr.Api.Models;
using Ocr.Api.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Implementations
{
    /*
        =========================================================
        OcrReviewService
        ---------------------------------------------------------
        Purpose:
        - Handles reviewed OCR submissions from users
        - Simulates final text update
        - Simulates correction audit creation
        - Uses suggestion service to learn approved corrections
        - Simulates searchable PDF regeneration

        Notes:
        - This is a safe first implementation skeleton.
        - Replace the stubbed sections with DB/repository logic later.
        =========================================================
    */

    public class OcrReviewService : IOcrReviewService
    {
        private readonly IOcrSuggestionService _ocrSuggestionService;

        public OcrReviewService(IOcrSuggestionService ocrSuggestionService)
        {
            _ocrSuggestionService = ocrSuggestionService;
        }

        public async Task<OcrReviewSubmitResponseDto> submitReviewAsync(OcrReviewSubmitRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "OCR review request cannot be null.");
            }

            if (request.ocrDocumentId <= 0)
            {
                throw new Exception("Invalid OCR document ID.");
            }

            if (string.IsNullOrWhiteSpace(request.reviewedBy))
            {
                throw new Exception("ReviewedBy is required.");
            }

            /*
                Replace this later with:
                - load OCR document from DB
                - load OCR fields from DB
                - update final text
                - update document status
                - insert correction audit rows
            */

            int learnedCorrectionsCount = await _ocrSuggestionService.learnCorrectionsAsync(
                request.fields,
                "DAS",
                "VRMD",
                "CLAIM201",
                request.reviewedBy
            );

            bool shouldRegeneratePdf = request.fields != null &&
                request.fields.Any(field =>
                    !string.Equals(
                        field.rawText?.Trim(),
                        field.finalText?.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    ));

            bool regenerated = false;
            int searchablePdfVersion = 1;

            if (shouldRegeneratePdf)
            {
                regenerated = await regenerateSearchablePdfAsync(request.ocrDocumentId, request.reviewedBy);
                searchablePdfVersion = regenerated ? 2 : 1;
            }

            return new OcrReviewSubmitResponseDto
            {
                success = true,
                ocrDocumentId = request.ocrDocumentId,
                status = OcrDocumentStatus.Reviewed.ToString(),
                learnedCorrectionsCount = learnedCorrectionsCount,
                searchablePdfRegenerated = regenerated,
                searchablePdfVersion = searchablePdfVersion
            };
        }

        public async Task<bool> regenerateSearchablePdfAsync(long ocrDocumentId, string? updatedBy)
        {
            await Task.Yield();

            if (ocrDocumentId <= 0)
            {
                return false;
            }

            /*
                Replace this later with:
                - get reviewed final OCR text
                - rebuild searchable PDF text layer
                - save new artifact version
                - mark previous artifact as not latest
            */
            return true;
        }

        public async Task<bool> markAsReviewedAsync(long ocrDocumentId, string reviewedBy)
        {
            await Task.Yield();

            if (ocrDocumentId <= 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(reviewedBy))
            {
                return false;
            }

            /*
                Replace this later with:
                - update OcrDocument status to Reviewed
                - optionally create correction audit row
            */
            return true;
        }
    }
}