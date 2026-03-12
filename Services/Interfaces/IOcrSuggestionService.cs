using Ocr.Api.Models;

namespace Ocr.Api.Services.Interfaces
{
    /*
         =========================================================
         IOcrSuggestionService
         ---------------------------------------------------------
         Purpose:
         - Handles OCR correction suggestions
         - Checks learned correction memory
         - Returns candidate suggestions for raw OCR text
         - Saves approved corrections for future reuse
         =========================================================
     */

    public interface IOcrSuggestionService
    {
        /*
            Returns correction suggestions for a given raw OCR text.

            Expected responsibilities of the implementation:
            - Normalize input OCR text
            - Check correction memory
            - Rank matching corrections
            - Return suggestion list
        */
        Task<OcrSuggestionResponseDto> getSuggestionsAsync(OcrSuggestionRequestDto request);

        /*
            Saves one approved OCR correction into correction memory.

            Example:
            - rawText: HE1L0
            - correctText: HELLO
        */
        Task<bool> learnCorrectionAsync(
            string rawText,
            string correctText,
            string? sourceSystem,
            string? officeCode,
            string? documentTypeCode,
            string? fieldName,
            string? createdBy
        );

        /*
            Saves multiple approved OCR corrections at once.

            Useful when a review screen submits several corrected fields.
        */
        Task<int> learnCorrectionsAsync(
            List<OcrReviewFieldDto> fields,
            string? sourceSystem,
            string? officeCode,
            string? documentTypeCode,
            string? reviewedBy
        );

        /*
            Normalizes OCR text before matching against correction memory.

            Example normalization:
            - trim spaces
            - uppercase text
            - collapse multiple spaces
        */
        string normalizeText(string inputText);
    }
}
