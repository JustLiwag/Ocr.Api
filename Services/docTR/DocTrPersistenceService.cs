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

            string pagePath = Path.Combine(
                docDir,
                $"page_{pageRecord.PageNumber:000}_page.json"
            );

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

            string wordsPath = Path.Combine(
                docDir,
                $"page_{pageNumber:000}_words.json"
            );

            string json = JsonSerializer.Serialize(wordList, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(wordsPath, json);
        }

        private string GetDocumentDirectory(string documentId)
        {
            var safeDocumentId = string.Concat(
                documentId.Split(Path.GetInvalidFileNameChars())
            );

            return Path.Combine(_persistenceRoot, safeDocumentId);
        }
    }
}