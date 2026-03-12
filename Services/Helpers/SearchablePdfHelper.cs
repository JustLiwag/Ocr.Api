namespace Ocr.Api.Services.Helpers
{
    /*
        =========================================================
        SearchablePdfHelper
        ---------------------------------------------------------
        Purpose:
        - Builds OCR artifact paths for searchable PDFs
        - Handles versioned searchable PDF file naming
        - Provides simple helper methods for artifact storage
        =========================================================
    */

    public static class SearchablePdfHelper
    {
        public static string buildArtifactDirectory(string baseArtifactPath)
        {
            string targetDirectory = Path.Combine(
                baseArtifactPath,
                DateTime.Now.ToString("yyyy"),
                DateTime.Now.ToString("MM")
            );

            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            return targetDirectory;
        }

        public static string buildSearchablePdfPath(string baseArtifactPath, long ocrDocumentId, int versionNo)
        {
            string targetDirectory = buildArtifactDirectory(baseArtifactPath);
            string fileName = OcrReferenceGenerator.generateSearchablePdfFileName(ocrDocumentId, versionNo);

            return Path.Combine(targetDirectory, fileName);
        }

        public static string getMimeType()
        {
            return "application/pdf";
        }

        public static string getExtension()
        {
            return ".pdf";
        }

        public static long getFileSizeBytes(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return 0;
            }

            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
    }
}
