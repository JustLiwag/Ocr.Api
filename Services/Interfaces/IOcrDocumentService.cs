using Ocr.Api.Models;

namespace Ocr.Api.Services.Interfaces
{
    /*
        =========================================================
        IOcrDocumentService
        ---------------------------------------------------------
        Purpose:
        - Retrieves OCR document records and related details
        - Returns OCR document metadata, fields, and artifacts
        - Supports filtering and paging for future document lists
        =========================================================
    */

    public interface IOcrDocumentService
    {
        /*
            Retrieves one OCR document by its internal OCR document ID.

            Expected responsibilities of the implementation:
            - Load OcrDocument record
            - Load related artifact records
            - Load related field records
            - Map them into OcrDocumentDetailDto
        */
        Task<OcrDocumentDetailDto?> getByIdAsync(long ocrDocumentId);

        /*
            Retrieves one OCR document by external document ID coming
            from the client system such as DAS ImageID.
        */
        Task<OcrDocumentDetailDto?> getByExternalDocumentIdAsync(string externalDocumentId);

        /*
            Retrieves a filtered and paged list of OCR documents.

            This is useful later for:
            - document history screens
            - correction queues
            - admin search screens
            - dashboard drill-down screens
        */
        Task<PagedResultDto<OcrDocumentDetailDto>> getPagedAsync(OcrDocumentFilterDto filter);
    }
}
