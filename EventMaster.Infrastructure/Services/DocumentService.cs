using EventMaster.Application.Common;
using EventMaster.Application.DTOs.Tickets;
using EventMaster.Application.Services;
using EventMaster.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EventMaster.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly IWebHostEnvironment _env;
    private readonly DocumentStorageOptions _options;

    public DocumentService(IWebHostEnvironment env, IOptions<DocumentStorageOptions> options)
    {
        _env = env;
        _options = options.Value;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<Result<string>> SaveEventPosterAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default)
    {
        if (content.Length == 0)
            return Result<string>.Fail("Dosya boş.");

        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(ext) || ext.Length > 10)
            ext = ".bin";

        var safeName = $"{Guid.NewGuid():N}{ext}";
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

        var postersPath = string.IsNullOrWhiteSpace(_options.PostersRelativePath)
            ? Path.Combine("uploads", "posters")
            : _options.PostersRelativePath;

        var dir = Path.Combine(webRoot, postersPath);
        Directory.CreateDirectory(dir);
        var full = Path.Combine(dir, safeName);

        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, cancellationToken);

        var relative = Path.Combine(postersPath, safeName).Replace("\\", "/");
        return Result<string>.Ok(relative);
    }

    public Task<Result<byte[]>> GenerateTicketPdfAsync(TicketDto ticket, CancellationToken cancellationToken = default)
    {
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("EventMaster — Bilet").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                page.Content().Column(c =>
                {
                    c.Spacing(8);
                    c.Item().Text($"Etkinlik: {ticket.EventTitle}");
                    c.Item().Text($"Koltuk: {ticket.SeatNumber}");
                    c.Item().Text($"Fiyat: {ticket.Price:C}");
                    c.Item().Text($"Alıcı: {ticket.BuyerName}");
                    c.Item().Text($"Satın alma: {ticket.PurchaseDate:u}");
                    c.Item().Text($"Bilet Id: {ticket.Id}");
                });
                page.Footer().AlignRight().Text(t =>
                {
                    t.Span("PDF örnek çıktı — ");
                    t.Span(DateTime.UtcNow.ToString("u"));
                });
            });
        }).GeneratePdf();

        return Task.FromResult(Result<byte[]>.Ok(pdf));
    }
}
