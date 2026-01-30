using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.Ocr;
using Ocr.Api.Services.Pdf;
using Ocr.Api.Services.Rendering;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection
builder.Services.AddScoped<ITempFileService, TempFileService>();
builder.Services.AddScoped<IPdfTextDetector, PdfPigTextDetector>();
builder.Services.AddScoped<IPdfRenderService, GhostscriptRenderService>();
builder.Services.AddScoped<ITesseractService, TesseractService>();

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
