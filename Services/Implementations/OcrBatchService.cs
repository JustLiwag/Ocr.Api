using Microsoft.AspNetCore.Http;
using Ocr.Api.Models;
using Ocr.Api.Services.Helpers;
using Ocr.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Implementations
{
    /*
        =========================================================
        OcrBatchService
        ---------------------------------------------------------
        Purpose:
        - Handles batch OCR processing
        - Reuses the single-file OCR flow internally
        - Builds batch response summaries
        - Prepares batch job style output for future DB persistence

        Notes:
        - This is a safe first implementation skeleton.
        - Actual batch job save/update logic can be added later.
        =========================================================
    */

    public class OcrBatchService : IOcrBatchService
    {
        private readonly IOcrProcessingService _ocrProcessingService;

        public OcrBatchService(IOcrProcessingService ocrProcessingService)
        {
            _ocrProcessingService = ocrProcessingService;
        }

        public async Task<OcrBatchProcessResponseDto> processBatchAsync(List<IFormFile> files, OcrBatchProcessRequestDto request)
        {
            validateRequest(request);

            if (files == null || files.Count == 0)
            {
                return new OcrBatchProcessResponseDto
                {
                    success = false,
                    batchReferenceNo = string.Empty,
                    ocrBatchJobId = 0,
                    totalFiles = 0,
                    processed = 0,
                    failed = 0,
                    results = new List<OcrBatchItemResultDto>()
                };
            }

            string batchReferenceNo = OcrReferenceGenerator.generateBatchReferenceNo();
            long simulatedBatchJobId = 1;

            List<OcrBatchItemResultDto> results = new List<OcrBatchItemResultDto>();
            int processedCount = 0;
            int failedCount = 0;

            foreach (IFormFile file in files)
            {
                if (file == null || file.Length == 0)
                {
                    failedCount++;

                    results.Add(new OcrBatchItemResultDto
                    {
                        fileName = file?.FileName ?? "UNKNOWN_FILE",
                        ocrDocumentId = null,
                        success = false,
                        errorMessage = "File is null or empty."
                    });

                    continue;
                }

                try
                {
                    OcrProcessRequestDto singleRequest = buildSingleRequestFromBatch(request, file.FileName);

                    OcrProcessResponseDto singleResponse = await _ocrProcessingService.processSingleAsync(file, singleRequest);

                    if (singleResponse.success)
                    {
                        processedCount++;

                        results.Add(new OcrBatchItemResultDto
                        {
                            fileName = file.FileName,
                            ocrDocumentId = singleResponse.ocrDocumentId,
                            success = true,
                            errorMessage = null
                        });
                    }
                    else
                    {
                        failedCount++;

                        results.Add(new OcrBatchItemResultDto
                        {
                            fileName = file.FileName,
                            ocrDocumentId = null,
                            success = false,
                            errorMessage = "OCR processing failed."
                        });
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;

                    results.Add(new OcrBatchItemResultDto
                    {
                        fileName = file.FileName,
                        ocrDocumentId = null,
                        success = false,
                        errorMessage = ex.Message
                    });
                }
            }

            return new OcrBatchProcessResponseDto
            {
                success = processedCount > 0,
                batchReferenceNo = batchReferenceNo,
                ocrBatchJobId = simulatedBatchJobId,
                totalFiles = files.Count,
                processed = processedCount,
                failed = failedCount,
                results = results
            };
        }

        public async Task<OcrBatchProcessResponseDto> processBatchAsync(List<OcrBatchFileRequestDto> files, OcrBatchProcessRequestDto request)
        {
            validateRequest(request);

            if (files == null || files.Count == 0)
            {
                return new OcrBatchProcessResponseDto
                {
                    success = false,
                    batchReferenceNo = string.Empty,
                    ocrBatchJobId = 0,
                    totalFiles = 0,
                    processed = 0,
                    failed = 0,
                    results = new List<OcrBatchItemResultDto>()
                };
            }

            string batchReferenceNo = OcrReferenceGenerator.generateBatchReferenceNo();
            long simulatedBatchJobId = 1;

            List<OcrBatchItemResultDto> results = new List<OcrBatchItemResultDto>();
            int processedCount = 0;
            int failedCount = 0;

            foreach (OcrBatchFileRequestDto file in files)
            {
                if (file == null || file.fileBytes == null || file.fileBytes.Length == 0)
                {
                    failedCount++;

                    results.Add(new OcrBatchItemResultDto
                    {
                        fileName = file?.fileName ?? "UNKNOWN_FILE",
                        ocrDocumentId = null,
                        success = false,
                        errorMessage = "File content is null or empty."
                    });

                    continue;
                }

                try
                {
                    OcrProcessRequestDto singleRequest = buildSingleRequestFromBatch(
                        request,
                        file.fileName,
                        file.externalDocumentId,
                        file.externalRecordId
                    );

                    OcrProcessResponseDto singleResponse = await _ocrProcessingService.processSingleAsync(
                        file.fileBytes,
                        file.fileName,
                        singleRequest
                    );

                    if (singleResponse.success)
                    {
                        processedCount++;

                        results.Add(new OcrBatchItemResultDto
                        {
                            fileName = file.fileName,
                            ocrDocumentId = singleResponse.ocrDocumentId,
                            success = true,
                            errorMessage = null
                        });
                    }
                    else
                    {
                        failedCount++;

                        results.Add(new OcrBatchItemResultDto
                        {
                            fileName = file.fileName,
                            ocrDocumentId = null,
                            success = false,
                            errorMessage = "OCR processing failed."
                        });
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;

                    results.Add(new OcrBatchItemResultDto
                    {
                        fileName = file.fileName,
                        ocrDocumentId = null,
                        success = false,
                        errorMessage = ex.Message
                    });
                }
            }

            return new OcrBatchProcessResponseDto
            {
                success = processedCount > 0,
                batchReferenceNo = batchReferenceNo,
                ocrBatchJobId = simulatedBatchJobId,
                totalFiles = files.Count,
                processed = processedCount,
                failed = failedCount,
                results = results
            };
        }

        /*
            =========================================================
            PRIVATE HELPERS
            =========================================================
        */

        private void validateRequest(OcrBatchProcessRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Batch OCR request cannot be null.");
            }

            if (string.IsNullOrWhiteSpace(request.sourceSystem))
            {
                throw new Exception("Source system is required.");
            }

            if (string.IsNullOrWhiteSpace(request.officeCode))
            {
                throw new Exception("Office code is required.");
            }

            if (string.IsNullOrWhiteSpace(request.documentTypeCode))
            {
                throw new Exception("Document type code is required.");
            }

            if (string.IsNullOrWhiteSpace(request.sensitivityLevel))
            {
                throw new Exception("Sensitivity level is required.");
            }
        }

        private OcrProcessRequestDto buildSingleRequestFromBatch(
            OcrBatchProcessRequestDto batchRequest,
            string fileName,
            string? externalDocumentId = null,
            string? externalRecordId = null
        )
        {
            return new OcrProcessRequestDto
            {
                sourceSystem = batchRequest.sourceSystem,
                officeCode = batchRequest.officeCode,
                documentTypeCode = batchRequest.documentTypeCode,
                sensitivityLevel = batchRequest.sensitivityLevel,
                externalDocumentId = externalDocumentId,
                externalRecordId = externalRecordId,
                originalFileName = fileName,
                requestUserId = batchRequest.requestUserId,
                returnSearchablePdfAsBase64 = false,
                fields = new List<OcrFieldRequestDto>()
            };
        }
    }
}