namespace Ocr.Api.Services.Pdf
{
    using System.Diagnostics;

    public class GhostscriptMergeService : IPdfMergeService
    {
        public Task<string> MergeAsync(
            IEnumerable<string> pdfPaths,
            string baseDir,
            string outputFileName)
        {
            Directory.CreateDirectory(baseDir);

            var output = Path.Combine(baseDir, $"{outputFileName}.pdf");
            return MergeChunkAsync(pdfPaths, output);
        }

        public async Task<string> MergeChunkAsync(
            IEnumerable<string> pdfPaths,
            string outputPdfPath)
        {
            var pdfList = pdfPaths.Where(File.Exists).ToList();

            if (pdfList.Count == 0)
                throw new InvalidOperationException("No PDF files to merge.");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath)!);

            var inputs = string.Join(" ", pdfList.Select(p => $"\"{p}\""));

            var args =
                $"-dBATCH -dNOPAUSE -q " +
                $"-sDEVICE=pdfwrite " +
                $"-sOutputFile=\"{outputPdfPath}\" {inputs}";

            var psi = new ProcessStartInfo("gswin64c", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var process = Process.Start(psi)!;

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"Ghostscript merge failed.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");

            if (!File.Exists(outputPdfPath))
                throw new FileNotFoundException("Merged PDF was not created.", outputPdfPath);

            return outputPdfPath;
        }

        public async Task<string> MergeInChunksAsync(
            IEnumerable<string> pdfPaths,
            string baseDir,
            string outputFileName,
            int chunkSize = 25)
        {
            var allPdfPaths = pdfPaths.Where(File.Exists).ToList();

            if (allPdfPaths.Count == 0)
                throw new InvalidOperationException("No PDF files to merge.");

            if (allPdfPaths.Count <= chunkSize)
                return await MergeAsync(allPdfPaths, baseDir, outputFileName);

            Directory.CreateDirectory(baseDir);

            var tempChunkDir = Path.Combine(baseDir, "_merge_chunks");
            Directory.CreateDirectory(tempChunkDir);

            var chunkOutputs = new List<string>();
            int chunkIndex = 0;

            for (int i = 0; i < allPdfPaths.Count; i += chunkSize)
            {
                var chunk = allPdfPaths.Skip(i).Take(chunkSize).ToList();
                var chunkOutput = Path.Combine(tempChunkDir, $"chunk_{chunkIndex:000}.pdf");

                await MergeChunkAsync(chunk, chunkOutput);
                chunkOutputs.Add(chunkOutput);

                chunkIndex++;
            }

            var finalOutput = Path.Combine(baseDir, $"{outputFileName}.pdf");
            await MergeChunkAsync(chunkOutputs, finalOutput);

            foreach (var chunkFile in chunkOutputs)
            {
                try
                {
                    if (File.Exists(chunkFile))
                        File.Delete(chunkFile);
                }
                catch
                {
                }
            }

            try
            {
                if (Directory.Exists(tempChunkDir) &&
                    !Directory.EnumerateFileSystemEntries(tempChunkDir).Any())
                {
                    Directory.Delete(tempChunkDir);
                }
            }
            catch
            {
            }

            return finalOutput;
        }
    }
}