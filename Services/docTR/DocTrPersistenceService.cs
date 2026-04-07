using Microsoft.Data.SqlClient;
using Ocr.Api.Models;
using Ocr.Api.Data;

namespace Ocr.Api.Services.Ocr
{
    public class DocTrPersistenceService : IDocTrPersistenceService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public DocTrPersistenceService(IDbConnectionFactory dbConnectionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
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
                    confidence = @confidence
                WHERE documentId = @documentId
                  AND pageNumber = @pageNumber
            END
            ELSE
            BEGIN
                INSERT INTO OCR_Page (documentId, pageNumber, engine, sourceImagePath, fullText, confidence)
                VALUES (@documentId, @pageNumber, @engine, @sourceImagePath, @fullText, @confidence)
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
                yMax
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
                @yMax
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
            SELECT documentId, pageNumber, engine, sourceImagePath, fullText, confidence, createdAt
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
                CreatedAt = reader["createdAt"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(reader["createdAt"])
            };
        }

        public async Task<List<DocTrWordRecord>> GetWordsAsync(string documentId, int pageNumber)
        {
            const string sql = @"
            SELECT documentId, pageNumber, wordOrder, rawText, correctedText, confidence, xMin, yMin, xMax, yMax, createdAt
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

            const string selectSql = @"
            SELECT correctedText, rawText
            FROM OCR_Word
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber
              AND wordOrder = @wordOrder";

                        const string historySql = @"
            INSERT INTO OCR_WordCorrectionHistory
            (
                documentId,
                pageNumber,
                wordOrder,
                oldText,
                newText,
                correctedBy
            )
            VALUES
            (
                @documentId,
                @pageNumber,
                @wordOrder,
                @oldText,
                @newText,
                @correctedBy
            )";

            const string updateSql = @"
            UPDATE OCR_Word
            SET correctedText = @correctedText
            WHERE documentId = @documentId
              AND pageNumber = @pageNumber
              AND wordOrder = @wordOrder";

            await using var conn = _dbConnectionFactory.CreateConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                foreach (var correction in correctionList)
                {
                    string? oldText = null;

                    await using (var selectCmd = new SqlCommand(selectSql, conn, (SqlTransaction)tx))
                    {
                        selectCmd.Parameters.AddWithValue("@documentId", documentId);
                        selectCmd.Parameters.AddWithValue("@pageNumber", pageNumber);
                        selectCmd.Parameters.AddWithValue("@wordOrder", correction.WordOrder);

                        await using var reader = await selectCmd.ExecuteReaderAsync();

                        if (await reader.ReadAsync())
                        {
                            var existingCorrected = reader["correctedText"] == DBNull.Value ? null : reader["correctedText"].ToString();
                            var rawText = reader["rawText"] == DBNull.Value ? null : reader["rawText"].ToString();

                            oldText = string.IsNullOrWhiteSpace(existingCorrected) ? rawText : existingCorrected;
                        }
                    }

                    await using (var historyCmd = new SqlCommand(historySql, conn, (SqlTransaction)tx))
                    {
                        historyCmd.Parameters.AddWithValue("@documentId", documentId);
                        historyCmd.Parameters.AddWithValue("@pageNumber", pageNumber);
                        historyCmd.Parameters.AddWithValue("@wordOrder", correction.WordOrder);
                        historyCmd.Parameters.AddWithValue("@oldText", (object?)oldText ?? DBNull.Value);
                        historyCmd.Parameters.AddWithValue("@newText", (object?)correction.CorrectedText ?? DBNull.Value);
                        historyCmd.Parameters.AddWithValue("@correctedBy", (object?)correction.CorrectedBy ?? DBNull.Value);

                        await historyCmd.ExecuteNonQueryAsync();
                    }

                    await using (var updateCmd = new SqlCommand(updateSql, conn, (SqlTransaction)tx))
                    {
                        updateCmd.Parameters.AddWithValue("@documentId", documentId);
                        updateCmd.Parameters.AddWithValue("@pageNumber", pageNumber);
                        updateCmd.Parameters.AddWithValue("@wordOrder", correction.WordOrder);
                        updateCmd.Parameters.AddWithValue("@correctedText", (object?)correction.CorrectedText ?? DBNull.Value);

                        await updateCmd.ExecuteNonQueryAsync();
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
    }
}