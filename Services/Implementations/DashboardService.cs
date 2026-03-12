using Ocr.Api.Models;
using Ocr.Api.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Implementations
{
    /*
        =========================================================
        DashboardService
        ---------------------------------------------------------
        Purpose:
        - Provides OCR summary statistics for dashboards
        - Provides office-specific counts
        - Supports date-range office summaries

        Notes:
        - This is a safe first implementation skeleton.
        - Replace the hardcoded values with DB aggregate queries later.
        =========================================================
    */

    public class DashboardService : IDashboardService
    {
        public async Task<OcrDashboardSummaryDto> getSummaryAsync()
        {
            await Task.Yield();

            /*
                Replace this later with DB aggregate queries.
            */
            return new OcrDashboardSummaryDto
            {
                totalProcessed = 10520,
                totalNeedsReview = 1210,
                totalReviewed = 8560,
                totalLowConfidence = 980,
                totalSearchablePdfGenerated = 10490
            };
        }

        public async Task<OcrOfficeDashboardDto> getOfficeSummaryAsync(string officeCode)
        {
            await Task.Yield();

            if (string.IsNullOrWhiteSpace(officeCode))
            {
                throw new Exception("Office code is required.");
            }

            /*
                Replace this later with office-specific aggregate queries.
            */
            return new OcrOfficeDashboardDto
            {
                officeCode = officeCode.ToUpperInvariant(),
                totalProcessed = 3210,
                pendingReview = 250,
                reviewed = 2700,
                lowConfidence = 190,
                todayProcessed = 48,
                todayReviewed = 39
            };
        }

        public async Task<OcrOfficeDashboardDto> getOfficeSummaryAsync(string officeCode, DateTime dateFrom, DateTime dateTo)
        {
            await Task.Yield();

            if (string.IsNullOrWhiteSpace(officeCode))
            {
                throw new Exception("Office code is required.");
            }

            if (dateFrom > dateTo)
            {
                throw new Exception("dateFrom cannot be later than dateTo.");
            }

            /*
                Replace this later with date-range filtered aggregate queries.
            */
            return new OcrOfficeDashboardDto
            {
                officeCode = officeCode.ToUpperInvariant(),
                totalProcessed = 420,
                pendingReview = 37,
                reviewed = 360,
                lowConfidence = 29,
                todayProcessed = 12,
                todayReviewed = 10
            };
        }
    }
}