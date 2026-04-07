using Microsoft.Extensions.Configuration;
using Ocr.Api.Models;
using System.Text.Json;

namespace Ocr.Api.Services.Ocr
{
    public class DocTrPersistenceService : IDocTrPersistenceService
    {
        private readonly string _persistenceRoot;

        public DocTrPersistenceService(IConfiguration config)
        {
            _persistenceRoot = config["DocTr:PersistenceRoot"]
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "OCR Test Data",
                    "doctr_persistence"
                );

            Directory.CreateDirectory(_persistenceRoot);
        }

        public async Task SavePageAsync(DocTrPageRecord pageRecord)
        {
            string docDir = GetDocumentDirectory(pageRecord.DocumentId);
            Directory.CreateDirectory(docDir);

            string pagePath = GetPagePath(pageRecord.DocumentId, pageRecord.PageNumber);

            string json = JsonSerializer.Serialize(pageRecord, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(pagePath, json);
        }

        public async Task SaveWordsAsync(IEnumerable<DocTrWordRecord> wordRecords)
        {
            var wordList = wordRecords.ToList();
            if (wordList.Count == 0)
                return;

            string documentId = wordList[0].DocumentId;
            int pageNumber = wordList[0].PageNumber;

            string docDir = GetDocumentDirectory(documentId);
            Directory.CreateDirectory(docDir);

            string wordsPath = GetWordsPath(documentId, pageNumber);

            string json = JsonSerializer.Serialize(wordList, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(wordsPath, json);
        }

        public async Task<DocTrPageRecord?> GetPageAsync(string documentId, int pageNumber)
        {
            string pagePath = GetPagePath(documentId, pageNumber);

            if (!File.Exists(pagePath))
                return null;

            string json = await File.ReadAllTextAsync(pagePath);

            return JsonSerializer.Deserialize<DocTrPageRecord>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<List<DocTrWordRecord>> GetWordsAsync(string documentId, int pageNumber)
        {
            string wordsPath = GetWordsPath(documentId, pageNumber);

            if (!File.Exists(wordsPath))
                return new List<DocTrWordRecord>();

            string json = await File.ReadAllTextAsync(wordsPath);

            return JsonSerializer.Deserialize<List<DocTrWordRecord>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<DocTrWordRecord>();
        }

        public async Task SaveCorrectionsAsync(string documentId, int pageNumber, IEnumerable<DocTrWordCorrectionRequest> corrections)
        {
            var correctionList = corrections.ToList();
            if (correctionList.Count == 0)
                return;

            var words = await GetWordsAsync(documentId, pageNumber);
            if (words.Count == 0)
                throw new FileNotFoundException("No persisted words found for the specified document/page.");

            var correctionMap = correctionList.ToDictionary(c => c.WordOrder, c => c.CorrectedText);

            foreach (var word in words)
            {
                if (correctionMap.TryGetValue(word.WordOrder, out var correctedText))
                    word.CorrectedText = correctedText;
            }

            await SaveWordsAsync(words);
        }

        private string GetDocumentDirectory(string documentId)
        {
            var safeDocumentId = string.Concat(
                documentId.Split(Path.GetInvalidFileNameChars())
            );

            return Path.Combine(_persistenceRoot, safeDocumentId);
        }

        private string GetPagePath(string documentId, int pageNumber)
        {
            return Path.Combine(
                GetDocumentDirectory(documentId),
                $"page_{pageNumber:000}_page.json"
            );
        }

        private string GetWordsPath(string documentId, int pageNumber)
        {
            return Path.Combine(
                GetDocumentDirectory(documentId),
                $"page_{pageNumber:000}_words.json"
            );
        }

        public Task<List<int>> GetPageNumbersAsync(string documentId)
        {
            string docDir = GetDocumentDirectory(documentId);

            if (!Directory.Exists(docDir))
                return Task.FromResult(new List<int>());

            var pageNumbers = Directory.GetFiles(docDir, "page_*_page.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name =>
                {
                    var parts = name!.Split('_');
                    return int.TryParse(parts[1], out var pageNo) ? pageNo : -1;
                })
                .Where(n => n > 0)
                .OrderBy(n => n)
                .ToList();

            return Task.FromResult(pageNumbers);
        }
    }
}