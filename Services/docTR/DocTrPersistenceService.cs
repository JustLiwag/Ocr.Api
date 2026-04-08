using Microsoft.Data.SqlClient;
using Ocr.Api.Data;
using Ocr.Api.Models.Records;
using Ocr.Api.Models.Api;

namespace Ocr.Api.Services.Ocr
{
    public class DocTrPersistenceService : IDocTrPersistenceService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IDocTrTextNormalizationService _docTrTextNormalizationService;

        public DocTrPersistenceService(IDbConnectionFactory dbConnectionFactory, IDocTrTextNormalizationService docTrTextNormalizationService)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _docTrTextNormalizationService = docTrTextNormalizationService;
        }

        public async Task SaveDocumentAsync(DocTrDocumentRecord documentRecord)
        {
            const string sql = @"
            IF EXISTS (SELECT 1 FROM OCR_Document WHERE documentId = @documentId)
            BEGIN
                UPDATE OCR_Document
                SET fileName = @fileName,
                    pageCount = @pageCount,
                    engine = @engine
                WHERE documentId = @documentId
            END
            ELSE
            BEGIN
                INSERT INTO OCR_Document (documentId, fileName, pageCount, engine)
                VALUES (@documentId, @fileName, @pageCount, @engine)
            END";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@documentId", documentRecord.DocumentId);
            cmd.Parameters.AddWithValue("@fileName", documentRecord.FileName);
            cmd.Parameters.AddWithValue("@pageCount", documentRecord.PageCount);
            cmd.Parameters.AddWithValue("@engine", documentRecord.Engine);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SavePageAsync(DocTrPageRecord pageRecord)
        {
            const string sql = @"
        IF EXISTS (SELECT 1 FROM OCR_Page WHERE documentId = @documentId AND pageNumber = @pageNumber)
        BEGIN
            UPDATE OCR_Page
            SET engine = @engine,
                sourceImagePath = @sourceImagePath,
                fullText = @fullText,
                confidence = @confidence,
                reviewStatus = @reviewStatus,
                reviewedBy = @reviewedBy,
                reviewedAt = @reviewedAt
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber
        END
        ELSE
        BEGIN
            INSERT INTO OCR_Page
            (
                documentId,
                pageNumber,
                engine,
                sourceImagePath,
                fullText,
                confidence,
                reviewStatus,
                reviewedBy,
                reviewedAt
            )
            VALUES
            (
                @documentId,
                @pageNumber,
                @engine,
                @sourceImagePath,
                @fullText,
                @confidence,
                @reviewStatus,
                @reviewedBy,
                @reviewedAt
            )
        END";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@documentId", pageRecord.DocumentId);
            cmd.Parameters.AddWithValue("@pageNumber", pageRecord.PageNumber);
            cmd.Parameters.AddWithValue("@engine", pageRecord.Engine);
            cmd.Parameters.AddWithValue("@sourceImagePath", (object?)pageRecord.SourceImagePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fullText", (object?)pageRecord.FullText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@confidence", pageRecord.Confidence);
            cmd.Parameters.AddWithValue("@reviewStatus", pageRecord.ReviewStatus);
            cmd.Parameters.AddWithValue("@reviewedBy", (object?)pageRecord.ReviewedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@reviewedAt", (object?)pageRecord.ReviewedAt ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveWordsAsync(IEnumerable<DocTrWordRecord> wordRecords)
        {
            var wordList = wordRecords.ToList();
            if (wordList.Count == 0)
                return;

            const string deleteSql = @"
            DELETE FROM OCR_Word
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber";

                        const string insertSql = @"
            INSERT INTO OCR_Word
            (
                documentId,
                pageNumber,
                wordOrder,
                rawText,
                correctedText,
                confidence,
                xMin,
                yMin,
                xMax,
                yMax,
                tokenType,
                normalizedText
            )
            VALUES
            (
                @documentId,
                @pageNumber,
                @wordOrder,
                @rawText,
                @correctedText,
                @confidence,
                @xMin,
                @yMin,
                @xMax,
                @yMax,
                @tokenType,
                @normalizedText
            )";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                await using (var deleteCmd = new SqlCommand(deleteSql, conn, (SqlTransaction)tx))
                {
                    deleteCmd.Parameters.AddWithValue("@documentId", wordList[0].DocumentId);
                    deleteCmd.Parameters.AddWithValue("@pageNumber", wordList[0].PageNumber);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                foreach (var word in wordList)
                {
                    await using var insertCmd = new SqlCommand(insertSql, conn, (SqlTransaction)tx);
                    insertCmd.Parameters.AddWithValue("@documentId", word.DocumentId);
                    insertCmd.Parameters.AddWithValue("@pageNumber", word.PageNumber);
                    insertCmd.Parameters.AddWithValue("@wordOrder", word.WordOrder);
                    insertCmd.Parameters.AddWithValue("@rawText", word.RawText);
                    insertCmd.Parameters.AddWithValue("@correctedText", (object?)word.CorrectedText ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@confidence", word.Confidence);
                    insertCmd.Parameters.AddWithValue("@xMin", word.XMin);
                    insertCmd.Parameters.AddWithValue("@yMin", word.YMin);
                    insertCmd.Parameters.AddWithValue("@xMax", word.XMax);
                    insertCmd.Parameters.AddWithValue("@yMax", word.YMax);
                    insertCmd.Parameters.AddWithValue("@tokenType", word.TokenType);
                    insertCmd.Parameters.AddWithValue("@normalizedText", (object?)word.NormalizedText ?? DBNull.Value);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<DocTrPageRecord?> GetPageAsync(string documentId, int pageNumber)
        {
            const string sql = @"
            SELECT documentId, pageNumber, engine, sourceImagePath, fullText, confidence,
                   reviewStatus, reviewedBy, reviewedAt, createdAt
            FROM OCR_Page
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@documentId", documentId);
            cmd.Parameters.AddWithValue("@pageNumber", pageNumber);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new DocTrPageRecord
            {
                DocumentId = reader["documentId"].ToString() ?? string.Empty,
                PageNumber = Convert.ToInt32(reader["pageNumber"]),
                Engine = reader["engine"].ToString() ?? "docTR",
                SourceImagePath = reader["sourceImagePath"] == DBNull.Value ? string.Empty : reader["sourceImagePath"].ToString() ?? string.Empty,
                FullText = reader["fullText"] == DBNull.Value ? string.Empty : reader["fullText"].ToString() ?? string.Empty,
                Confidence = Convert.ToSingle(reader["confidence"]),
                CreatedAt = reader["createdAt"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(reader["createdAt"]),
                ReviewStatus = reader["reviewStatus"] == DBNull.Value ? "NotReviewed" : reader["reviewStatus"].ToString() ?? "NotReviewed",
                ReviewedBy = reader["reviewedBy"] == DBNull.Value ? null : reader["reviewedBy"].ToString(),
                ReviewedAt = reader["reviewedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["reviewedAt"]),
            };
        }

        public async Task<List<DocTrWordRecord>> GetWordsAsync(string documentId, int pageNumber)
        {
            const string sql = @"
            SELECT documentId, pageNumber, wordOrder, rawText, correctedText, confidence, xMin, yMin, xMax, yMax, tokenType, normalizedText, createdAt
            FROM OCR_Word
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber
            ORDER BY wordOrder";

            var words = new List<DocTrWordRecord>();

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@documentId", documentId);
            cmd.Parameters.AddWithValue("@pageNumber", pageNumber);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                words.Add(new DocTrWordRecord
                {
                    DocumentId = reader["documentId"].ToString() ?? string.Empty,
                    PageNumber = Convert.ToInt32(reader["pageNumber"]),
                    WordOrder = Convert.ToInt32(reader["wordOrder"]),
                    RawText = reader["rawText"].ToString() ?? string.Empty,
                    CorrectedText = reader["correctedText"] == DBNull.Value ? null : reader["correctedText"].ToString(),
                    Confidence = Convert.ToSingle(reader["confidence"]),
                    XMin = Convert.ToSingle(reader["xMin"]),
                    YMin = Convert.ToSingle(reader["yMin"]),
                    XMax = Convert.ToSingle(reader["xMax"]),
                    YMax = Convert.ToSingle(reader["yMax"]),
                    TokenType = reader["tokenType"] == DBNull.Value ? "Word" : reader["tokenType"].ToString() ?? "Word",
                    NormalizedText = reader["normalizedText"] == DBNull.Value ? string.Empty : reader["normalizedText"].ToString() ?? string.Empty,
                    CreatedAt = reader["createdAt"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(reader["createdAt"])
                });
            }

            return words;
        }

        public async Task SaveCorrectionsAsync(string documentId, int pageNumber, IEnumerable<DocTrWordCorrectionRequest> corrections)
        {
            var correctionList = corrections.ToList();
            if (correctionList.Count == 0)
                return;

            const string selectWordSql = @"
            SELECT correctedText, rawText
            FROM OCR_Word
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber
              AND wordOrder = @wordOrder";

            const string insertHistorySql = @"
            INSERT INTO OCR_WordCorrectionHistory
            (
                documentId,
                pageNumber,
                wordOrder,
                oldText,
                normalizedOldText,
                newText,
                correctedBy
            )
            VALUES
            (
                @documentId,
                @pageNumber,
                @wordOrder,
                @oldText,
                @normalizedOldText,
                @newText,
                @correctedBy
            )";

            const string updateWordSql = @"
            UPDATE OCR_Word
            SET correctedText = @correctedText
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber
              AND wordOrder = @wordOrder";

            const string selectPageStatusSql = @"
            SELECT reviewStatus
            FROM OCR_Page
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber";

            const string updatePageStatusSql = @"
            UPDATE OCR_Page
            SET reviewStatus = 'NeedsRecheck',
                reviewedBy = NULL,
                reviewedAt = NULL
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                bool hasRealChanges = false;

                foreach (var correction in correctionList)
                {
                    string? rawText = null;
                    string? existingCorrectedText = null;

                    await using (var selectCmd = new SqlCommand(selectWordSql, conn, (SqlTransaction)tx))
                    {
                        selectCmd.Parameters.AddWithValue("@documentId", documentId);
                        selectCmd.Parameters.AddWithValue("@pageNumber", pageNumber);
                        selectCmd.Parameters.AddWithValue("@wordOrder", correction.WordOrder);

                        await using var reader = await selectCmd.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            existingCorrectedText = reader["correctedText"] == DBNull.Value ? null : reader["correctedText"].ToString();
                            rawText = reader["rawText"] == DBNull.Value ? null : reader["rawText"].ToString();
                        }
                        else
                        {
                            continue;
                        }
                    }

                    string currentFinalText = string.IsNullOrWhiteSpace(existingCorrectedText)
                        ? (rawText ?? string.Empty)
                        : existingCorrectedText;

                    string newFinalText = correction.CorrectedText ?? string.Empty;
                    string normalizedCurrentFinalText = _docTrTextNormalizationService.NormalizeForSuggestion(currentFinalText);
                    string normalizedNewFinalText = _docTrTextNormalizationService.NormalizeForSuggestion(newFinalText);

                    if (string.Equals(normalizedCurrentFinalText, normalizedNewFinalText, StringComparison.Ordinal))
                        continue;

                    hasRealChanges = true;

                    string normalizedOldText = normalizedCurrentFinalText;

                    await using (var historyCmd = new SqlCommand(insertHistorySql, conn, (SqlTransaction)tx))
                    {
                        historyCmd.Parameters.AddWithValue("@documentId", documentId);
                        historyCmd.Parameters.AddWithValue("@pageNumber", pageNumber);
                        historyCmd.Parameters.AddWithValue("@wordOrder", correction.WordOrder);
                        historyCmd.Parameters.AddWithValue("@oldText", (object?)currentFinalText ?? DBNull.Value);
                        historyCmd.Parameters.AddWithValue("@normalizedOldText", normalizedOldText);
                        historyCmd.Parameters.AddWithValue("@newText", (object?)newFinalText ?? DBNull.Value);
                        historyCmd.Parameters.AddWithValue("@correctedBy", (object?)correction.CorrectedBy ?? DBNull.Value);

                        await historyCmd.ExecuteNonQueryAsync();
                    }

                    await using (var updateCmd = new SqlCommand(updateWordSql, conn, (SqlTransaction)tx))
                    {
                        updateCmd.Parameters.AddWithValue("@documentId", documentId);
                        updateCmd.Parameters.AddWithValue("@pageNumber", pageNumber);
                        updateCmd.Parameters.AddWithValue("@wordOrder", correction.WordOrder);
                        updateCmd.Parameters.AddWithValue("@correctedText", (object?)correction.CorrectedText ?? DBNull.Value);

                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }

                if (hasRealChanges)
                {
                    string? currentReviewStatus = null;

                    await using (var pageStatusCmd = new SqlCommand(selectPageStatusSql, conn, (SqlTransaction)tx))
                    {
                        pageStatusCmd.Parameters.AddWithValue("@documentId", documentId);
                        pageStatusCmd.Parameters.AddWithValue("@pageNumber", pageNumber);

                        var result = await pageStatusCmd.ExecuteScalarAsync();
                        currentReviewStatus = result == null || result == DBNull.Value ? null : result.ToString();
                    }

                    if (string.Equals(currentReviewStatus, "Reviewed", StringComparison.OrdinalIgnoreCase))
                    {
                        await using var updatePageCmd = new SqlCommand(updatePageStatusSql, conn, (SqlTransaction)tx);
                        updatePageCmd.Parameters.AddWithValue("@documentId", documentId);
                        updatePageCmd.Parameters.AddWithValue("@pageNumber", pageNumber);

                        await updatePageCmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<List<int>> GetPageNumbersAsync(string documentId)
        {
            const string sql = @"
            SELECT pageNumber
            FROM OCR_Page
            WHERE documentId = @documentId
            ORDER BY pageNumber";

            var pageNumbers = new List<int>();

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@documentId", documentId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                pageNumbers.Add(Convert.ToInt32(reader["pageNumber"]));
            }

            return pageNumbers;
        }

        public async Task<DocTrDocumentRecord?> GetDocumentAsync(string documentId)
        {
            const string sql = @"
            SELECT documentId, fileName, pageCount, engine, createdAt
            FROM OCR_Document
            WHERE documentId = @documentId";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@documentId", documentId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            return new DocTrDocumentRecord
            {
                DocumentId = reader["documentId"].ToString() ?? string.Empty,
                FileName = reader["fileName"].ToString() ?? string.Empty,
                PageCount = Convert.ToInt32(reader["pageCount"]),
                Engine = reader["engine"].ToString() ?? "docTR",
                CreatedAt = reader["createdAt"] == DBNull.Value
                    ? DateTime.UtcNow
                    : Convert.ToDateTime(reader["createdAt"])
            };
        }
            
        public async Task<List<DocTrDocumentRecord>> GetDocumentsAsync(string? reviewStatus = null)
        {
            string sql = @"
            SELECT
                d.documentId,
                d.fileName,
                d.pageCount,
                d.engine,
                d.createdAt,
                CAST(AVG(CAST(w.confidence AS FLOAT)) * 100.0 AS FLOAT) AS ocrConfidence,
                SUM(CASE WHEN w.correctedText IS NOT NULL AND LTRIM(RTRIM(w.correctedText)) <> '' THEN 1 ELSE 0 END) AS correctedWords,
                SUM(CASE WHEN p.reviewStatus = 'Reviewed' THEN 1 ELSE 0 END) AS reviewedPages,
                CASE
                    WHEN SUM(CASE WHEN p.reviewStatus = 'NeedsRecheck' THEN 1 ELSE 0 END) > 0 THEN 'NeedsRecheck'
                    WHEN SUM(CASE WHEN p.reviewStatus = 'Reviewed' THEN 1 ELSE 0 END) = COUNT(DISTINCT p.pageNumber) THEN 'Reviewed'
                    WHEN SUM(CASE WHEN p.reviewStatus = 'Reviewed' THEN 1 ELSE 0 END) > 0 THEN 'PartiallyReviewed'
                    ELSE 'NotReviewed'
                END AS reviewStatus
            FROM OCR_Document d
            LEFT JOIN OCR_Page p
                ON d.documentId = p.documentId
            LEFT JOIN OCR_Word w
                ON p.documentId = w.documentId
               AND p.pageNumber = w.pageNumber
            WHERE (@reviewStatus IS NULL OR @reviewStatus = '' OR p.reviewStatus = @reviewStatus)
            GROUP BY d.documentId, d.fileName, d.pageCount, d.engine, d.createdAt
            ORDER BY d.createdAt DESC";

            var documents = new List<DocTrDocumentRecord>();

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@reviewStatus", (object?)reviewStatus ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                float? ocrConfidence = reader["ocrConfidence"] == DBNull.Value
                    ? null
                    : Convert.ToSingle(reader["ocrConfidence"]);

                string? quality = null;
                if (ocrConfidence.HasValue)
                {
                    if (ocrConfidence.Value >= 90)
                        quality = "Excellent";
                    else if (ocrConfidence.Value >= 75)
                        quality = "Good";
                    else if (ocrConfidence.Value >= 50)
                        quality = "Fair";
                    else
                        quality = "Poor";
                }

                documents.Add(new DocTrDocumentRecord
                {
                    DocumentId = reader["documentId"].ToString() ?? string.Empty,
                    FileName = reader["fileName"].ToString() ?? string.Empty,
                    PageCount = Convert.ToInt32(reader["pageCount"]),
                    Engine = reader["engine"].ToString() ?? "docTR",
                    CreatedAt = Convert.ToDateTime(reader["createdAt"]),
                    OcrConfidence = ocrConfidence,
                    Quality = quality,
                    ReviewStatus = reader["reviewStatus"] == DBNull.Value ? "NotReviewed" : reader["reviewStatus"].ToString(),
                    ReviewedPages = reader["reviewedPages"] == DBNull.Value ? 0 : Convert.ToInt32(reader["reviewedPages"]),
                    CorrectedWords = reader["correctedWords"] == DBNull.Value ? 0 : Convert.ToInt32(reader["correctedWords"])
                });
            }

            return documents;
        }

        public async Task<List<DocTrWordCorrectionHistoryRecord>> GetCorrectionHistoryAsync(string documentId, int pageNumber, int wordOrder)
        {
            const string sql = @"
            SELECT historyId, documentId, pageNumber, wordOrder, oldText, newText, correctedAt, correctedBy
            FROM OCR_WordCorrectionHistory
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber
              AND wordOrder = @wordOrder
            ORDER BY correctedAt DESC, historyId DESC";

            var history = new List<DocTrWordCorrectionHistoryRecord>();

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@documentId", documentId);
            cmd.Parameters.AddWithValue("@pageNumber", pageNumber);
            cmd.Parameters.AddWithValue("@wordOrder", wordOrder);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                history.Add(new DocTrWordCorrectionHistoryRecord
                {
                    HistoryId = Convert.ToInt64(reader["historyId"]),
                    DocumentId = reader["documentId"].ToString() ?? string.Empty,
                    PageNumber = Convert.ToInt32(reader["pageNumber"]),
                    WordOrder = Convert.ToInt32(reader["wordOrder"]),
                    OldText = reader["oldText"] == DBNull.Value ? null : reader["oldText"].ToString(),
                    NewText = reader["newText"] == DBNull.Value ? null : reader["newText"].ToString(),
                    CorrectedAt = Convert.ToDateTime(reader["correctedAt"]),
                    CorrectedBy = reader["correctedBy"] == DBNull.Value ? null : reader["correctedBy"].ToString()
                });
            }

            return history;
        }

        public async Task UpdatePageReviewAsync(string documentId, int pageNumber, string reviewStatus, string? reviewedBy)
        {
            const string sql = @"
            UPDATE OCR_Page
            SET reviewStatus = @reviewStatus,
                reviewedBy = @reviewedBy,
                reviewedAt = CASE
                    WHEN @reviewStatus = 'Reviewed' THEN SYSUTCDATETIME()
                    ELSE reviewedAt
                END
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@documentId", documentId);
            cmd.Parameters.AddWithValue("@pageNumber", pageNumber);
            cmd.Parameters.AddWithValue("@reviewStatus", reviewStatus);
            cmd.Parameters.AddWithValue("@reviewedBy", (object?)reviewedBy ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<DocTrCorrectionSuggestionDto>> GetCorrectionSuggestionsAsync(string rawText, int top = 5)
        {
            string normalized = _docTrTextNormalizationService.NormalizeForSuggestion(rawText);

            const string sql = @"
            SELECT TOP (@top)
                h.normalizedOldText AS rawText,
                h.newText AS suggestedText,
                COUNT(*) AS occurrences
            FROM OCR_WordCorrectionHistory h
            INNER JOIN OCR_Word w
                ON h.documentId = w.documentId
               AND h.pageNumber = w.pageNumber
               AND h.wordOrder = w.wordOrder
            WHERE h.normalizedOldText = @normalizedOldText
              AND h.newText IS NOT NULL
              AND LTRIM(RTRIM(h.newText)) <> ''
              AND w.tokenType = 'Word'
            GROUP BY h.normalizedOldText, h.newText
            ORDER BY COUNT(*) DESC, h.newText ASC";

            var suggestions = new List<DocTrCorrectionSuggestionDto>();

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@normalizedOldText", normalized);
            cmd.Parameters.AddWithValue("@top", top);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                suggestions.Add(new DocTrCorrectionSuggestionDto
                {
                    RawText = rawText,
                    SuggestedText = reader["suggestedText"].ToString() ?? string.Empty,
                    Occurrences = Convert.ToInt32(reader["occurrences"])
                });
            }

            return suggestions;
        }

        public async Task<Dictionary<string, List<DocTrCorrectionSuggestionDto>>> GetCorrectionSuggestionsBatchAsync(IEnumerable<string> rawTexts, int top = 5)
        {
            var keys = rawTexts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => _docTrTextNormalizationService.NormalizeForSuggestion(x))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

            var result = keys.ToDictionary(
                k => k,
                _ => new List<DocTrCorrectionSuggestionDto>(),
                StringComparer.Ordinal
            );

            if (keys.Count == 0)
                return result;

            const string sql = @"
            WITH RankedSuggestions AS
            (
                SELECT
                    h.normalizedOldText AS rawText,
                    h.newText AS suggestedText,
                    COUNT(*) AS occurrences,
                    ROW_NUMBER() OVER (
                        PARTITION BY h.normalizedOldText
                        ORDER BY COUNT(*) DESC, h.newText ASC
                    ) AS rn
                FROM OCR_WordCorrectionHistory h
                INNER JOIN OCR_Word w
                    ON h.documentId = w.documentId
                   AND h.pageNumber = w.pageNumber
                   AND h.wordOrder = w.wordOrder
                WHERE h.normalizedOldText IN ({0})
                  AND h.newText IS NOT NULL
                  AND LTRIM(RTRIM(h.newText)) <> ''
                  AND w.tokenType = 'Word'
                GROUP BY h.normalizedOldText, h.newText
            )
            SELECT rawText, suggestedText, occurrences
            FROM RankedSuggestions
            WHERE rn <= @top
            ORDER BY rawText, occurrences DESC, suggestedText ASC;";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();

            var parameterNames = new List<string>();
            for (int i = 0; i < keys.Count; i++)
                parameterNames.Add($"@rawText{i}");

            string finalSql = string.Format(sql, string.Join(", ", parameterNames));

            await using var cmd = new SqlCommand(finalSql, conn);
            cmd.Parameters.AddWithValue("@top", top);

            for (int i = 0; i < keys.Count; i++)
                cmd.Parameters.AddWithValue(parameterNames[i], keys[i]);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string rawText = reader["rawText"].ToString() ?? string.Empty;

                if (!result.ContainsKey(rawText))
                    result[rawText] = new List<DocTrCorrectionSuggestionDto>();

                result[rawText].Add(new DocTrCorrectionSuggestionDto
                {
                    RawText = rawText,
                    SuggestedText = reader["suggestedText"].ToString() ?? string.Empty,
                    Occurrences = Convert.ToInt32(reader["occurrences"])
                });
            }

            return result;
        }
    }
}