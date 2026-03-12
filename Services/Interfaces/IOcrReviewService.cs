using Ocr.Api.Models;

namespace Ocr.Api.Services.Interfaces
{
    /*
        =========================================================
        IOcrReviewService
        ---------------------------------------------------------
        Purpose:
        - Handles reviewed OCR submissions from users
        - Updates final OCR field values
        - Creates correction audit records
        - Optionally learns approved corrections
        - Optionally regenerates searchable PDF after review
        =========================================================
    */

    public interface IOcrReviewService
    {
        /*
            Submits reviewed OCR results for one OCR document.

            Expected responsibilities of the implementation:
            - Validate OCR document exists
            - Validate review request
            - Update final text values in OCR fields
            - Update final full text in OCR document
            - Create correction audit records
            - Learn approved corrections when requested
            - Update document status
            - Optionally regenerate searchable PDF
            - Return review result summary
        */
        Task<OcrReviewSubmitResponseDto> submitReviewAsync(OcrReviewSubmitRequestDto request);

        /*
            Regenerates the searchable PDF for a reviewed OCR document.

            This is useful when the final approved text differs from the
            original OCR output and you want the text layer to reflect
            the corrected content.
        */
        Task<bool> regenerateSearchablePdfAsync(long ocrDocumentId, string? updatedBy);

        /*
            Marks an OCR document as reviewed without learning corrections.

            This is useful for cases where a user only validates the OCR
            result and no field changes are needed.
        */
        Task<bool> markAsReviewedAsync(long ocrDocumentId, string reviewedBy);
    }
}
