using Ocr.Api.Models;
using Ocr.Api.Services.Helpers;
using Ocr.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Implementations
{
    /*
        =========================================================
        OcrSuggestionService
        ---------------------------------------------------------
        Purpose:
        - Handles OCR correction suggestions
        - Normalizes OCR text
        - Simulates lookup of learned corrections
        - Saves approved corrections for future reuse

        Notes:
        - This is a safe first implementation skeleton.
        - Replace the in-memory sample data with DB access later.
        =========================================================
    */

    public class OcrSuggestionService : IOcrSuggestionService
    {
        public async Task<OcrSuggestionResponseDto> getSuggestionsAsync(OcrSuggestionRequestDto request)
        {
            await Task.Yield();

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Suggestion request cannot be null.");
            }

            string normalizedInput = normalizeText(request.rawText);
            List<OcrCorrectionMemory> learnedCorrections = getSampleCorrections();

            List<OcrSuggestionItemDto> suggestions = learnedCorrections
                .Where(correction => correction.isActive)
                .Where(correction =>
                    string.Equals(correction.normalizedWrongText, normalizedInput, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        OcrTextNormalizer.normalizeForLooseMatch(correction.normalizedWrongText),
                        OcrTextNormalizer.normalizeForLooseMatch(normalizedInput),
                        StringComparison.OrdinalIgnoreCase
                    ))
                .OrderByDescending(correction => correction.approvalScore)
                .ThenByDescending(correction => correction.confirmationCount)
                .ThenByDescending(correction => correction.usageCount)
                .Select(correction => new OcrSuggestionItemDto
                {
                    correctText = correction.correctText,
                    score = correction.approvalScore,
                    usageCount = correction.usageCount,
                    confirmationCount = correction.confirmationCount
                })
                .ToList();

            return new OcrSuggestionResponseDto
            {
                success = true,
                rawText = request.rawText,
                suggestions = suggestions
            };
        }

        public async Task<bool> learnCorrectionAsync(
            string rawText,
            string correctText,
            string? sourceSystem,
            string? officeCode,
            string? documentTypeCode,
            string? fieldName,
            string? createdBy
        )
        {
            await Task.Yield();

            string normalizedWrongText = normalizeText(rawText);
            string normalizedCorrectText = normalizeText(correctText);

            if (string.IsNullOrWhiteSpace(normalizedWrongText))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedCorrectText))
            {
                return false;
            }

            if (string.Equals(normalizedWrongText, normalizedCorrectText, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            /*
                Replace this later with:
                - lookup existing correction in DB
                - insert or update correction memory
            */
            return true;
        }

        public async Task<int> learnCorrectionsAsync(
            List<OcrReviewFieldDto> fields,
            string? sourceSystem,
            string? officeCode,
            string? documentTypeCode,
            string? reviewedBy
        )
        {
            await Task.Yield();

            if (fields == null || fields.Count == 0)
            {
                return 0;
            }

            int learnedCount = 0;

            foreach (OcrReviewFieldDto field in fields)
            {
                if (!field.learnCorrection)
                {
                    continue;
                }

                bool learned = await learnCorrectionAsync(
                    field.rawText ?? string.Empty,
                    field.finalText ?? string.Empty,
                    sourceSystem,
                    officeCode,
                    documentTypeCode,
                    field.fieldName,
                    reviewedBy
                );

                if (learned)
                {
                    learnedCount++;
                }
            }

            return learnedCount;
        }

        public string normalizeText(string inputText)
        {
            return OcrTextNormalizer.normalize(inputText);
        }

        /*
            =========================================================
            PRIVATE HELPERS
            =========================================================
        */

        private List<OcrCorrectionMemory> getSampleCorrections()
        {
            return new List<OcrCorrectionMemory>
            {
                new OcrCorrectionMemory
                {
                    ocrCorrectionId = 1,
                    wrongText = "HE1L0",
                    correctText = "HELLO",
                    normalizedWrongText = OcrTextNormalizer.normalize("HE1L0"),
                    normalizedCorrectText = OcrTextNormalizer.normalize("HELLO"),
                    usageCount = 14,
                    confirmationCount = 10,
                    approvalScore = 0.96m,
                    isActive = true,
                    sourceSystem = "DAS",
                    officeCode = "VRMD",
                    documentTypeCode = "CLAIM201",
                    fieldName = "BeneName",
                    createdAt = DateTime.Now.AddDays(-7),
                    createdBy = "system"
                },
                new OcrCorrectionMemory
                {
                    ocrCorrectionId = 2,
                    wrongText = "C1AIM-001",
                    correctText = "CLAIM-001",
                    normalizedWrongText = OcrTextNormalizer.normalize("C1AIM-001"),
                    normalizedCorrectText = OcrTextNormalizer.normalize("CLAIM-001"),
                    usageCount = 6,
                    confirmationCount = 5,
                    approvalScore = 0.89m,
                    isActive = true,
                    sourceSystem = "DAS",
                    officeCode = "VRMD",
                    documentTypeCode = "CLAIM201",
                    fieldName = "ClaimNo",
                    createdAt = DateTime.Now.AddDays(-3),
                    createdBy = "system"
                }
            };
        }
    }
}