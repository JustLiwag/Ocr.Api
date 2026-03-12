using Ocr.Api.Services;
using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.ImageProcessing;
using Ocr.Api.Services.Implementations;
using Ocr.Api.Services.Implementations.Ocr.Api.Services.Implementations;
using Ocr.Api.Services.Interfaces;
using Ocr.Api.Services.Ocr;
using Ocr.Api.Services.Pdf;
using Ocr.Api.Services.Rendering;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Existing Dependency Injection
builder.Services.AddScoped<ITempFileService, TempFileService>();
builder.Services.AddScoped<IPdfTextDetector, PdfPigTextDetector>();
builder.Services.AddScoped<IPdfRenderService, GhostscriptRenderService>();
builder.Services.AddScoped<ITesseractService, TesseractService>();
builder.Services.AddScoped<IImagePreprocessingService, ImagePreprocessingService>();
builder.Services.AddScoped<IPdfMergeService, GhostscriptMergeService>();

// New OCR Platform Services
builder.Services.AddScoped<IOcrProcessingService, OcrProcessingService>();
builder.Services.AddScoped<IOcrBatchService, OcrBatchService>();
builder.Services.AddScoped<IOcrDocumentService, OcrDocumentService>();
builder.Services.AddScoped<IOcrSuggestionService, OcrSuggestionService>();
builder.Services.AddScoped<IOcrReviewService, OcrReviewService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OCR API v1");
    });
}

app.UseAuthorization();
app.MapControllers();
app.Run();