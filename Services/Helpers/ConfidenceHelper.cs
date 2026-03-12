namespace Ocr.Api.Services.Helpers
{
    /*
        =========================================================
        ConfidenceHelper
        ---------------------------------------------------------
        Purpose:
        - Computes confidence bands
        - Computes average confidence scores
        - Helps standardize review thresholds
        =========================================================
    */

    public static class ConfidenceHelper
    {
        public static string getConfidenceBand(decimal? confidenceScore)
        {
            if (!confidenceScore.HasValue)
            {
                return "Unknown";
            }

            if (confidenceScore.Value >= 95m)
            {
                return "95-100";
            }

            if (confidenceScore.Value >= 85m)
            {
                return "85-94";
            }

            if (confidenceScore.Value >= 70m)
            {
                return "70-84";
            }

            return "Below-70";
        }

        public static decimal? getAverageConfidence(IEnumerable<decimal?> confidenceScores)
        {
            if (confidenceScores == null)
            {
                return null;
            }

            List<decimal> validScores = confidenceScores
                .Where(score => score.HasValue)
                .Select(score => score!.Value)
                .ToList();

            if (validScores.Count == 0)
            {
                return null;
            }

            return Math.Round(validScores.Average(), 2);
        }

        public static bool needsReview(decimal? confidenceScore)
        {
            if (!confidenceScore.HasValue)
            {
                return true;
            }

            return confidenceScore.Value < 85m;
        }
    }
}
