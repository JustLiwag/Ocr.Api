using Ocr.Api.Models;

namespace Ocr.Api.Services.Interfaces
{
    /*
        =========================================================
        IDashboardService
        ---------------------------------------------------------
        Purpose:
        - Provides OCR statistics for dashboards and reports
        - Supports summary counts across all systems/offices
        - Supports office-specific and date-range statistics
        =========================================================
    */

    public interface IDashboardService
    {
        /*
            Returns top-level OCR summary counts across all records.

            Expected responsibilities of the implementation:
            - Count total processed OCR documents
            - Count total documents needing review
            - Count total reviewed documents
            - Count low-confidence documents
            - Count generated searchable PDFs
        */
        Task<OcrDashboardSummaryDto> getSummaryAsync();

        /*
            Returns dashboard statistics for one office.

            Example:
            - VRMD totals
            - pending review under VRMD
            - reviewed under VRMD
            - low-confidence under VRMD
            - today's OCR activity under VRMD
        */
        Task<OcrOfficeDashboardDto> getOfficeSummaryAsync(string officeCode);

        /*
            Returns dashboard statistics for one office within a date range.

            This is useful for:
            - daily dashboards
            - weekly/monthly reports
            - custom report filtering
        */
        Task<OcrOfficeDashboardDto> getOfficeSummaryAsync(string officeCode, DateTime dateFrom, DateTime dateTo);
    }
}
