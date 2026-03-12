namespace Ocr.Api.Models
{
    /*
        =========================================================
        ENUMS
        These help standardize common values used across entities
        and DTOs. You can keep them as enums here, or later move
        them to a separate folder if the project grows.
        =========================================================
    */

    public enum OcrDocumentStatus
    {
        Queued,
        Processing,
        Processed,
        NeedsReview,
        Reviewed,
        Failed
    }

    public enum OcrBatchJobStatus
    {
        Queued,
        Processing,
        Completed,
        PartiallyCompleted,
        Failed
    }

    public enum OcrBatchJobItemStatus
    {
        Pending,
        Processing,
        Processed,
        Failed
    }

    public enum OcrCorrectionTaskStatus
    {
        Pending,
        Assigned,
        InReview,
        Completed,
        Rejected
    }

    public enum OcrArtifactType
    {
        SearchablePdf,
        CorrectedSearchablePdf,
        TextExport,
        OcrJson,
        PreprocessedImageZip
    }

    public enum SensitivityLevel
    {
        General,
        Restricted,
        Private,
        Confidential
    }

    public enum OcrCorrectionActionType
    {
        SuggestedAccepted,
        UserCorrected,
        Rejected,
        Reviewed
    }


    /*
        =========================================================
        ENTITY MODELS
        These mirror the OCR database tables.
        They represent the data as stored in the OCR database.
        =========================================================
    */

    public class Office
    {
        public string officeCode { get; set; } = string.Empty;
        public string officeName { get; set; } = string.Empty;
        public bool isActive { get; set; } = true;
        public DateTime createdAt { get; set; } = DateTime.Now;
    }

    public class DocumentType
    {
        public string documentTypeCode { get; set; } = string.Empty;
        public string documentTypeName { get; set; } = string.Empty;
        public bool isActive { get; set; } = true;
        public DateTime createdAt { get; set; } = DateTime.Now;
    }

    public class OcrDocument
    {
        public long ocrDocumentId { get; set; }
        public string ocrReferenceNo { get; set; } = string.Empty;
        public string sourceSystem { get; set; } = string.Empty;
        public string officeCode { get; set; } = string.Empty;
        public string documentTypeCode { get; set; } = string.Empty;
        public string sensitivityLevel { get; set; } = string.Empty;
        public string? externalDocumentId { get; set; }
        public string? externalRecordId { get; set; }
        public string? originalFileName { get; set; }
        public string? originalFileExtension { get; set; }
        public string? originalMimeType { get; set; }
        public long? fileSizeBytes { get; set; }
        public int? totalPages { get; set; }
        public string? rawFullText { get; set; }
        public string? finalFullText { get; set; }
        public decimal? averageConfidence { get; set; }
        public string? confidenceBand { get; set; }
        public bool needsReview { get; set; }
        public string status { get; set; } = OcrDocumentStatus.Processed.ToString();
        public DateTime? processingStartedAt { get; set; }
        public DateTime? processingCompletedAt { get; set; }
        public DateTime createdAt { get; set; } = DateTime.Now;
        public string? createdBy { get; set; }
        public DateTime? updatedAt { get; set; }
        public string? updatedBy { get; set; }
    }

    public class OcrArtifact
    {
        public long ocrArtifactId { get; set; }
        public long ocrDocumentId { get; set; }
        public string artifactType { get; set; } = string.Empty;
        public string fileName { get; set; } = string.Empty;
        public string filePath { get; set; } = string.Empty;
        public string? fileExtension { get; set; }
        public string? mimeType { get; set; }
        public long? fileSizeBytes { get; set; }
        public int versionNo { get; set; } = 1;
        public bool isLatest { get; set; } = true;
        public DateTime createdAt { get; set; } = DateTime.Now;
        public string? createdBy { get; set; }
    }

    public class OcrBatchJob
    {
        public long ocrBatchJobId { get; set; }
        public string batchReferenceNo { get; set; } = string.Empty;
        public string sourceSystem { get; set; } = string.Empty;
        public string officeCode { get; set; } = string.Empty;
        public int totalFiles { get; set; }
        public int processedFiles { get; set; }
        public int failedFiles { get; set; }
        public string status { get; set; } = OcrBatchJobStatus.Processing.ToString();
        public DateTime requestedAt { get; set; } = DateTime.Now;
        public DateTime? startedAt { get; set; }
        public DateTime? completedAt { get; set; }
        public string? requestedBy { get; set; }
    }

    public class OcrBatchJobItem
    {
        public long ocrBatchJobItemId { get; set; }
        public long ocrBatchJobId { get; set; }
        public long? ocrDocumentId { get; set; }
        public string? originalFileName { get; set; }
        public string status { get; set; } = OcrBatchJobItemStatus.Pending.ToString();
        public string? errorMessage { get; set; }
        public DateTime createdAt { get; set; } = DateTime.Now;
        public DateTime? processedAt { get; set; }
    }

    public class OcrDocumentField
    {
        public long ocrFieldId { get; set; }
        public long ocrDocumentId { get; set; }
        public string fieldName { get; set; } = string.Empty;
        public string? fieldLabel { get; set; }
        public string? rawText { get; set; }
        public string? suggestedText { get; set; }
        public string? finalText { get; set; }
        public decimal? confidenceScore { get; set; }
        public bool isUserCorrected { get; set; }
        public bool isAccepted { get; set; }
        public int? pageNo { get; set; }
        public int? boundingBoxX { get; set; }
        public int? boundingBoxY { get; set; }
        public int? boundingBoxWidth { get; set; }
        public int? boundingBoxHeight { get; set; }
        public DateTime createdAt { get; set; } = DateTime.Now;
        public DateTime? updatedAt { get; set; }
    }

    public class OcrCorrectionMemory
    {
        public long ocrCorrectionId { get; set; }
        public string wrongText { get; set; } = string.Empty;
        public string correctText { get; set; } = string.Empty;
        public string normalizedWrongText { get; set; } = string.Empty;
        public string normalizedCorrectText { get; set; } = string.Empty;
        public int usageCount { get; set; } = 1;
        public int confirmationCount { get; set; } = 1;
        public decimal approvalScore { get; set; } = 1;
        public bool isActive { get; set; } = true;
        public string? sourceSystem { get; set; }
        public string? officeCode { get; set; }
        public string? documentTypeCode { get; set; }
        public string? fieldName { get; set; }
        public DateTime createdAt { get; set; } = DateTime.Now;
        public string? createdBy { get; set; }
        public DateTime? updatedAt { get; set; }
        public string? updatedBy { get; set; }
    }

    public class OcrCorrectionAudit
    {
        public long ocrCorrectionAuditId { get; set; }
        public long ocrDocumentId { get; set; }
        public long? ocrFieldId { get; set; }
        public string? rawText { get; set; }
        public string? suggestedText { get; set; }
        public string? finalText { get; set; }
        public string actionTaken { get; set; } = string.Empty;
        public string? actionBy { get; set; }
        public DateTime actionAt { get; set; } = DateTime.Now;
    }

    public class OcrCorrectionTask
    {
        public long ocrCorrectionTaskId { get; set; }
        public long ocrDocumentId { get; set; }
        public string sourceSystem { get; set; } = string.Empty;
        public string officeCode { get; set; } = string.Empty;
        public string sensitivityLevel { get; set; } = string.Empty;
        public string taskStatus { get; set; } = OcrCorrectionTaskStatus.Pending.ToString();
        public string? assignedToUserId { get; set; }
        public string? assignedToRole { get; set; }
        public DateTime createdAt { get; set; } = DateTime.Now;
        public DateTime? startedAt { get; set; }
        public DateTime? completedAt { get; set; }
        public string? completedBy { get; set; }
    }


    /*
        =========================================================
        COMMON RESPONSE DTO
        Useful base response for API endpoints.
        =========================================================
    */

    public class ApiResponseDto<T>
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public T? data { get; set; }
        public List<string> errors { get; set; } = new List<string>();
    }


    /*
        =========================================================
        OCR PROCESSING DTOs
        Used for single-file OCR requests and responses.
        =========================================================
    */

    public class OcrFieldRequestDto
    {
        public string fieldName { get; set; } = string.Empty;
        public string? fieldLabel { get; set; }
    }

    public class OcrProcessRequestDto
    {
        public string sourceSystem { get; set; } = string.Empty;
        public string officeCode { get; set; } = string.Empty;
        public string documentTypeCode { get; set; } = string.Empty;
        public string sensitivityLevel { get; set; } = SensitivityLevel.General.ToString();
        public string? externalDocumentId { get; set; }
        public string? externalRecordId { get; set; }
        public string? originalFileName { get; set; }
        public string? requestUserId { get; set; }
        public bool returnSearchablePdfAsBase64 { get; set; }
        public List<OcrFieldRequestDto> fields { get; set; } = new List<OcrFieldRequestDto>();
    }

    public class OcrSearchablePdfDto
    {
        public string? fileName { get; set; }
        public string? filePath { get; set; }
        public int version { get; set; }
        public string? base64Content { get; set; }
    }

    public class OcrFieldResultDto
    {
        public string fieldName { get; set; } = string.Empty;
        public string? fieldLabel { get; set; }
        public string? rawText { get; set; }
        public string? suggestedText { get; set; }
        public string? finalText { get; set; }
        public decimal? confidenceScore { get; set; }
        public int? pageNo { get; set; }
        public int? boundingBoxX { get; set; }
        public int? boundingBoxY { get; set; }
        public int? boundingBoxWidth { get; set; }
        public int? boundingBoxHeight { get; set; }
    }

    public class OcrProcessResponseDto
    {
        public bool success { get; set; }
        public long ocrDocumentId { get; set; }
        public string? ocrReferenceNo { get; set; }
        public string sourceSystem { get; set; } = string.Empty;
        public string officeCode { get; set; } = string.Empty;
        public string documentTypeCode { get; set; } = string.Empty;
        public decimal? averageConfidence { get; set; }
        public string? confidenceBand { get; set; }
        public bool needsReview { get; set; }
        public string status { get; set; } = string.Empty;
        public string? rawFullText { get; set; }
        public OcrSearchablePdfDto? searchablePdf { get; set; }
        public bool suggestionsAvailable { get; set; }
        public List<OcrFieldResultDto> fields { get; set; } = new List<OcrFieldResultDto>();
    }


    /*
        =========================================================
        BATCH OCR DTOs
        Used for batch OCR requests and responses.
        =========================================================
    */

    public class OcrBatchFileItemDto
    {
        public string originalFileName { get; set; } = string.Empty;
        public string? externalDocumentId { get; set; }
        public string? externalRecordId { get; set; }
    }

    public class OcrBatchProcessRequestDto
    {
        public string sourceSystem { get; set; } = string.Empty;
        public string officeCode { get; set; } = string.Empty;
        public string documentTypeCode { get; set; } = string.Empty;
        public string sensitivityLevel { get; set; } = SensitivityLevel.General.ToString();
        public string? requestUserId { get; set; }
        public List<OcrBatchFileItemDto> files { get; set; } = new List<OcrBatchFileItemDto>();
    }

    public class OcrBatchItemResultDto
    {
        public string fileName { get; set; } = string.Empty;
        public long? ocrDocumentId { get; set; }
        public bool success { get; set; }
        public string? errorMessage { get; set; }
    }

    public class OcrBatchProcessResponseDto
    {
        public bool success { get; set; }
        public string batchReferenceNo { get; set; } = string.Empty;
        public long ocrBatchJobId { get; set; }
        public int totalFiles { get; set; }
        public int processed { get; set; }
        public int failed { get; set; }
        public List<OcrBatchItemResultDto> results { get; set; } = new List<OcrBatchItemResultDto>();
    }


    /*
        =========================================================
        OCR DOCUMENT DTOs
        Used for querying OCR document details later.
        =========================================================
    */

    public class OcrArtifactDto
    {
        public long ocrArtifactId { get; set; }
        public string artifactType { get; set; } = string.Empty;
        public string fileName { get; set; } = string.Empty;
        public string filePath { get; set; } = string.Empty;
        public int versionNo { get; set; }
        public bool isLatest { get; set; }
    }

    public class OcrDocumentDetailDto
    {
        public long ocrDocumentId { get; set; }
        public string ocrReferenceNo { get; set; } = string.Empty;
        public string sourceSystem { get; set; } = string.Empty;
        public string officeCode { get; set; } = string.Empty;
        public string documentTypeCode { get; set; } = string.Empty;
        public string sensitivityLevel { get; set; } = string.Empty;
        public string? externalDocumentId { get; set; }
        public string? externalRecordId { get; set; }
        public string? originalFileName { get; set; }
        public int? totalPages { get; set; }
        public string? rawFullText { get; set; }
        public string? finalFullText { get; set; }
        public decimal? averageConfidence { get; set; }
        public string? confidenceBand { get; set; }
        public bool needsReview { get; set; }
        public string status { get; set; } = string.Empty;
        public DateTime createdAt { get; set; }
        public List<OcrArtifactDto> artifacts { get; set; } = new List<OcrArtifactDto>();
        public List<OcrFieldResultDto> fields { get; set; } = new List<OcrFieldResultDto>();
    }


    /*
        =========================================================
        REVIEW / CORRECTION DTOs
        Used when users review OCR results and submit corrections.
        =========================================================
    */

    public class OcrReviewFieldDto
    {
        public string fieldName { get; set; } = string.Empty;
        public string? rawText { get; set; }
        public string? finalText { get; set; }
        public bool learnCorrection { get; set; }
    }

    public class OcrReviewSubmitRequestDto
    {
        public long ocrDocumentId { get; set; }
        public string reviewedBy { get; set; } = string.Empty;
        public string? reviewerRole { get; set; }
        public List<OcrReviewFieldDto> fields { get; set; } = new List<OcrReviewFieldDto>();
    }

    public class OcrReviewSubmitResponseDto
    {
        public bool success { get; set; }
        public long ocrDocumentId { get; set; }
        public string status { get; set; } = string.Empty;
        public int learnedCorrectionsCount { get; set; }
        public bool searchablePdfRegenerated { get; set; }
        public int searchablePdfVersion { get; set; }
    }


    /*
        =========================================================
        SUGGESTION DTOs
        Used for correction suggestions based on learned OCR memory.
        =========================================================
    */

    public class OcrSuggestionRequestDto
    {
        public string rawText { get; set; } = string.Empty;
        public string? sourceSystem { get; set; }
        public string? officeCode { get; set; }
        public string? documentTypeCode { get; set; }
        public string? fieldName { get; set; }
    }

    public class OcrSuggestionItemDto
    {
        public string correctText { get; set; } = string.Empty;
        public decimal score { get; set; }
        public int usageCount { get; set; }
        public int confirmationCount { get; set; }
    }

    public class OcrSuggestionResponseDto
    {
        public bool success { get; set; }
        public string rawText { get; set; } = string.Empty;
        public List<OcrSuggestionItemDto> suggestions { get; set; } = new List<OcrSuggestionItemDto>();
    }


    /*
        =========================================================
        DASHBOARD DTOs
        Used by DAS or other systems to fetch OCR statistics.
        =========================================================
    */

    public class OcrDashboardSummaryDto
    {
        public int totalProcessed { get; set; }
        public int totalNeedsReview { get; set; }
        public int totalReviewed { get; set; }
        public int totalLowConfidence { get; set; }
        public int totalSearchablePdfGenerated { get; set; }
    }

    public class OcrOfficeDashboardDto
    {
        public string officeCode { get; set; } = string.Empty;
        public int totalProcessed { get; set; }
        public int pendingReview { get; set; }
        public int reviewed { get; set; }
        public int lowConfidence { get; set; }
        public int todayProcessed { get; set; }
        public int todayReviewed { get; set; }
    }


    /*
        =========================================================
        COMMON FILTER DTOs
        Useful for list/search/report endpoints later.
        =========================================================
    */

    public class OcrDocumentFilterDto
    {
        public string? sourceSystem { get; set; }
        public string? officeCode { get; set; }
        public string? documentTypeCode { get; set; }
        public string? status { get; set; }
        public string? sensitivityLevel { get; set; }
        public DateTime? dateFrom { get; set; }
        public DateTime? dateTo { get; set; }
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 20;
    }

    public class PagedResultDto<T>
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int totalCount { get; set; }
        public List<T> items { get; set; } = new List<T>();
    }   
}
