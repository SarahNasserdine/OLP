using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OLP.Api.Services
{
    public class CertificatePdfBuilder
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        public CertificatePdfBuilder(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        public async Task<byte[]> BuildAsync(string studentName, string courseTitle, DateTime completionDate)
        {
            var logoBytes = await TryLoadLogoAsync();
            var dateText = completionDate.ToString("MMM dd, yyyy");

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(text => text.FontFamily("Georgia").FontSize(14).FontColor("#0b1f3a"));
                    page.Content().Element(content => BuildCertificate(content, logoBytes, studentName, courseTitle, dateText));
                });
            }).GeneratePdf();
        }

        private static void BuildCertificate(IContainer container, byte[]? logoBytes, string studentName, string courseTitle, string dateText)
        {
            container
                .Border(6)
                .BorderColor("#c9a227")
                .Padding(10)
                .Element(inner =>
                {
                    inner.Border(2)
                        .BorderColor("#1e3a8a")
                        .Padding(24)
                        .Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Element(el =>
                                {
                                    if (logoBytes is { Length: > 0 })
                                    {
                                        el.Height(60).Image(logoBytes, ImageScaling.FitHeight);
                                    }
                                    else
                                    {
                                        el.Text("OLP").FontSize(20).Bold().FontColor("#1e3a8a");
                                    }
                                });
                                row.RelativeItem().AlignRight().Text("CERTIFICATE").FontSize(12).FontColor("#1e3a8a");
                            });

                            column.Item().PaddingTop(20).AlignCenter()
                                .Text("Certificate of Completion")
                                .FontSize(36)
                                .Bold()
                                .FontColor("#1e3a8a");

                            column.Item().PaddingTop(8).AlignCenter()
                                .Text("This certifies that")
                                .FontSize(16)
                                .FontColor("#0b1f3a");

                            column.Item().PaddingTop(10).AlignCenter()
                                .Text(studentName)
                                .FontSize(42)
                                .Bold()
                                .FontColor("#c9a227");

                            column.Item().PaddingTop(8).AlignCenter()
                                .Text("has successfully completed the course")
                                .FontSize(16)
                                .FontColor("#0b1f3a");

                            column.Item().PaddingTop(8).AlignCenter()
                                .Text(courseTitle)
                                .FontSize(22)
                                .SemiBold()
                                .FontColor("#1e3a8a");

                            column.Item().PaddingTop(30).Row(row =>
                            {
                                row.RelativeItem().AlignLeft().Text($"Date: {dateText}").FontSize(12);
                                row.RelativeItem().AlignRight().Column(sig =>
                                {
                                    sig.Item().AlignRight().Width(200).BorderBottom(1).BorderColor(Colors.Grey.Medium);
                                    sig.Item().AlignRight().Text("Issued by OLP").FontSize(12);
                                });
                            });
                        });
                });
        }

        private async Task<byte[]?> TryLoadLogoAsync()
        {
            var logoUrl = _configuration["Certificate:LogoUrl"];
            if (string.IsNullOrWhiteSpace(logoUrl))
                return null;

            try
            {
                var client = _clientFactory.CreateClient();
                return await client.GetByteArrayAsync(logoUrl);
            }
            catch
            {
                return null;
            }
        }
    }
}
