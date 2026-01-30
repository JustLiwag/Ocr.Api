

namespace Ocr.Api.Services.FileStorage
{
    public class TempFileService : ITempFileService
    {
        private readonly string _tempRoot;

        public TempFileService(IConfiguration config)
        {
            _tempRoot = config.GetValue<string>("OcrSettings:TempRoot") ?? Path.GetTempPath();
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var tempRoot = _tempRoot ?? Path.GetTempPath();
            Directory.CreateDirectory(tempRoot);

            var filePath = Path.Combine(tempRoot, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            return filePath;
        }
    }

}
