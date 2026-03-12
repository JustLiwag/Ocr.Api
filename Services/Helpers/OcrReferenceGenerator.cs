namespace Ocr.Api.Services.Helpers
{
    /*
        =========================================================
        OcrReferenceGenerator
        ---------------------------------------------------------
        Purpose:
        - Generates friendly OCR document and batch references
        - Useful for tracking and display in dashboards/logs
        =========================================================
    */

    public static class OcrReferenceGenerator
    {
        public static string generateOcrReferenceNo()
        {
            return $"OCR-{DateTime.Now:yyyyMMddHHmmssfff}";
        }

        public static string generateBatchReferenceNo()
        {
            return $"BATCH-{DateTime.Now:yyyyMMddHHmmssfff}";
        }

        public static string generateSearchablePdfFileName(long ocrDocumentId, int versionNo)
        {
            return $"{ocrDocumentId}-searchable-v{versionNo}.pdf";
        }
    }
}
