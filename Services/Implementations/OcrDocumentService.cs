namespace Ocr.Api.Services.Implementations
{
    using global::Ocr.Api.Models;
    using global::Ocr.Api.Services.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    namespace Ocr.Api.Services.Implementations
    {
        /*
            =========================================================
            OcrDocumentService
            ---------------------------------------------------------
            Purpose:
            - Retrieves OCR document records and related details
            - Returns OCR document metadata, fields, and artifacts
            - Supports filtering and paging for future document lists

            Notes:
            - This is a safe first implementation skeleton.
            - Actual database access should be plugged in later.
            =========================================================
        */

        public class OcrDocumentService : IOcrDocumentService
        {
            public async Task<OcrDocumentDetailDto?> getByIdAsync(long ocrDocumentId)
            {
                await Task.Yield();

                if (ocrDocumentId <= 0)
                {
                    return null;
                }

                /*
                    Replace this later with:
                    - DB lookup for OcrDocument
                    - DB lookup for related OcrArtifact
                    - DB lookup for related OcrDocumentField
                */
                return buildSampleDocumentDetail(ocrDocumentId, $"EXT-{ocrDocumentId}");
            }

            public async Task<OcrDocumentDetailDto?> getByExternalDocumentIdAsync(string externalDocumentId)
            {
                await Task.Yield();

                if (string.IsNullOrWhiteSpace(externalDocumentId))
                {
                    return null;
                }

                /*
                    Replace this later with:
                    - DB lookup by ExternalDocumentId
                */
                return buildSampleDocumentDetail(1, externalDocumentId);
            }

            public async Task<PagedResultDto<OcrDocumentDetailDto>> getPagedAsync(OcrDocumentFilterDto filter)
            {
                await Task.Yield();

                if (filter == null)
                {
                    throw new ArgumentNullException(nameof(filter), "OCR document filter cannot be null.");
                }

                if (filter.pageNumber <= 0)
                {
                    filter.pageNumber = 1;
                }

                if (filter.pageSize <= 0)
                {
                    filter.pageSize = 20;
                }

                /*
                    Replace this later with:
                    - filtered DB query
                    - total count query
                    - paging query
                */
                List<OcrDocumentDetailDto> sampleItems = new List<OcrDocumentDetailDto>
            {
                buildSampleDocumentDetail(1, "EXT-1"),
                buildSampleDocumentDetail(2, "EXT-2"),
                buildSampleDocumentDetail(3, "EXT-3")
            };

                IEnumerable<OcrDocumentDetailDto> query = sampleItems;

                if (!string.IsNullOrWhiteSpace(filter.sourceSystem))
                {
                    query = query.Where(item =>
                        string.Equals(item.sourceSystem, filter.sourceSystem, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filter.officeCode))
                {
                    query = query.Where(item =>
                        string.Equals(item.officeCode, filter.officeCode, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filter.documentTypeCode))
                {
                    query = query.Where(item =>
                        string.Equals(item.documentTypeCode, filter.documentTypeCode, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filter.status))
                {
                    query = query.Where(item =>
                        string.Equals(item.status, filter.status, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(filter.sensitivityLevel))
                {
                    query = query.Where(item =>
                        string.Equals(item.sensitivityLevel, filter.sensitivityLevel, StringComparison.OrdinalIgnoreCase));
                }

                if (filter.dateFrom.HasValue)
                {
                    query = query.Where(item => item.createdAt >= filter.dateFrom.Value);
                }

                if (filter.dateTo.HasValue)
                {
                    query = query.Where(item => item.createdAt <= filter.dateTo.Value);
                }

                int totalCount = query.Count();

                List<OcrDocumentDetailDto> pagedItems = query
                    .OrderByDescending(item => item.createdAt)
                    .Skip((filter.pageNumber - 1) * filter.pageSize)
                    .Take(filter.pageSize)
                    .ToList();

                return new PagedResultDto<OcrDocumentDetailDto>
                {
                    pageNumber = filter.pageNumber,
                    pageSize = filter.pageSize,
                    totalCount = totalCount,
                    items = pagedItems
                };
            }

            /*
                =========================================================
                PRIVATE HELPERS
                =========================================================
            */

            private OcrDocumentDetailDto buildSampleDocumentDetail(long ocrDocumentId, string externalDocumentId)
            {
                return new OcrDocumentDetailDto
                {
                    ocrDocumentId = ocrDocumentId,
                    ocrReferenceNo = $"OCR-20260312-{ocrDocumentId:D6}",
                    sourceSystem = "DAS",
                    officeCode = "VRMD",
                    documentTypeCode = "CLAIM201",
                    sensitivityLevel = SensitivityLevel.Private.ToString(),
                    externalDocumentId = externalDocumentId,
                    externalRecordId = $"REC-{ocrDocumentId:D6}",
                    originalFileName = $"sample-{ocrDocumentId}.pdf",
                    totalPages = 1,
                    rawFullText = "SIMULATED OCR TEXT OUTPUT",
                    finalFullText = "SIMULATED OCR TEXT OUTPUT",
                    averageConfidence = 82.50m,
                    confidenceBand = "70-84",
                    needsReview = true,
                    status = OcrDocumentStatus.NeedsReview.ToString(),
                    createdAt = DateTime.Now.AddMinutes(-ocrDocumentId),
                    artifacts = new List<OcrArtifactDto>
                {
                    new OcrArtifactDto
                    {
                        ocrArtifactId = ocrDocumentId,
                        artifactType = OcrArtifactType.SearchablePdf.ToString(),
                        fileName = $"{ocrDocumentId}-searchable-v1.pdf",
                        filePath = $@"C:\Users\Public\Desktop\OcrArtifacts\2026\03\{ocrDocumentId}-searchable-v1.pdf",
                        versionNo = 1,
                        isLatest = true
                    }
                },
                    fields = new List<OcrFieldResultDto>
                {
                    new OcrFieldResultDto
                    {
                        fieldName = "ClaimNo",
                        fieldLabel = "Claim Number",
                        rawText = "C1AIM-001",
                        suggestedText = "CLAIM-001",
                        finalText = "CLAIM-001",
                        confidenceScore = 78.20m,
                        pageNo = 1
                    },
                    new OcrFieldResultDto
                    {
                        fieldName = "BeneName",
                        fieldLabel = "Beneficiary Name",
                        rawText = "HE1L0",
                        suggestedText = "HELLO",
                        finalText = "HELLO",
                        confidenceScore = 72.00m,
                        pageNo = 1
                    }
                }
                };
            }
        }
    }
}
