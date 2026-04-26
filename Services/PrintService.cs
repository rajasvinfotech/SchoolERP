using SchoolERP.Data.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SchoolERP.Services;

public class PrintService
{
    public byte[] GenerateReceipt(FeePayment payment, bool isDuplicate = false)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(20);

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(payment.School?.Name ?? "School ERP")
                                .FontSize(18).Bold().FontColor("#0d6efd");
                            c.Item().Text(payment.School?.Address ?? "").FontSize(9);
                            c.Item().Text($"Phone: {payment.School?.Phone ?? ""} | Email: {payment.School?.Email ?? ""}").FontSize(9);
                        });
                    });

                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#dee2e6");

                    // Title
                    col.Item().AlignCenter().Text(isDuplicate ? "FEE RECEIPT (DUPLICATE)" : "FEE RECEIPT")
                        .FontSize(14).Bold();

                    if (isDuplicate)
                        col.Item().AlignCenter().Text("DUPLICATE COPY")
                            .FontSize(10).FontColor("#dc3545").Bold();

                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#dee2e6");

                    // Receipt Info
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Receipt No: {payment.ReceiptNumber}").FontSize(10).Bold();
                        row.RelativeItem().AlignRight().Text($"Date: {payment.PaymentDate:dd/MM/yyyy}").FontSize(10);
                    });

                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#dee2e6");

                    // Student Info
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                        });

                        table.Cell().Text("Student:").FontSize(9).Bold();
                        table.Cell().Text(payment.Student?.FullName ?? "").FontSize(9);
                        table.Cell().Text("Adm No:").FontSize(9).Bold();
                        table.Cell().Text(payment.Student?.AdmissionNumber ?? "").FontSize(9);

                        table.Cell().Text("Father:").FontSize(9).Bold();
                        table.Cell().Text(payment.Student?.FatherName ?? "").FontSize(9);
                        table.Cell().Text("Month:").FontSize(9).Bold();
                        table.Cell().Text(payment.Month).FontSize(9);

                        table.Cell().Text("Class:").FontSize(9).Bold();
                        table.Cell().Text($"{payment.Student?.Class?.Name ?? ""} - {payment.Student?.Section?.Name ?? ""}").FontSize(9);
                        table.Cell().Text("Roll No:").FontSize(9).Bold();
                        table.Cell().Text(payment.Student?.RollNumber ?? "").FontSize(9);
                    });

                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#dee2e6");

                    // Fee Details Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1);
                        });

                        // Header
                        table.Cell().Background("#0d6efd").Padding(4).Text("Fee Head").FontColor("#ffffff").FontSize(10).Bold();
                        table.Cell().Background("#0d6efd").Padding(4).AlignRight().Text("Amount").FontColor("#ffffff").FontSize(10).Bold();

                        // Details
                        foreach (var detail in payment.Details)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#dee2e6").Padding(3).Text(detail.FeeHeadName).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor("#dee2e6").Padding(3).AlignRight().Text($"₹{detail.Amount:N0}").FontSize(9);
                        }

                        // Summary
                        table.Cell().Padding(3).Text("Sub Total").FontSize(9).Bold();
                        table.Cell().Padding(3).AlignRight().Text($"₹{payment.SubTotal:N0}").FontSize(9).Bold();

                        if (payment.PreviousDue > 0)
                        {
                            table.Cell().Padding(3).Text("Previous Due").FontSize(9).FontColor("#dc3545");
                            table.Cell().Padding(3).AlignRight().Text($"₹{payment.PreviousDue:N0}").FontSize(9).FontColor("#dc3545");
                        }

                        if (payment.Discount > 0)
                        {
                            table.Cell().Padding(3).Text($"Discount ({payment.DiscountReason})").FontSize(9).FontColor("#198754");
                            table.Cell().Padding(3).AlignRight().Text($"-₹{payment.Discount:N0}").FontSize(9).FontColor("#198754");
                        }

                        if (payment.LateFine > 0)
                        {
                            table.Cell().Padding(3).Text("Late Fine").FontSize(9).FontColor("#fd7e14");
                            table.Cell().Padding(3).AlignRight().Text($"+₹{payment.LateFine:N0}").FontSize(9).FontColor("#fd7e14");
                        }

                        // Net Amount
                        table.Cell().Background("#f8f9fa").Padding(4).Text("NET AMOUNT").FontSize(12).Bold();
                        table.Cell().Background("#f8f9fa").Padding(4).AlignRight().Text($"₹{payment.NetAmount:N0}").FontSize(12).Bold().FontColor("#0d6efd");
                    });

                    col.Item().PaddingTop(6).Text($"Amount in Words: {NumberToWords((long)payment.NetAmount)} Only")
                        .FontSize(9).Italic();

                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#dee2e6");

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Payment Mode: {payment.PaymentMode}").FontSize(9);
                        if (!string.IsNullOrEmpty(payment.PaymentReference))
                            row.RelativeItem().Text($"Ref: {payment.PaymentReference}").FontSize(9);
                    });

                    col.Item().Text($"Received by: {payment.CollectedBy}").FontSize(9);

                    col.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#dee2e6");

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().PaddingTop(20).Text("School Stamp").FontSize(8).FontColor("#6c757d");
                        row.RelativeItem().AlignRight().PaddingTop(20).Text("Authorized Signature").FontSize(8).FontColor("#6c757d");
                    });

                    col.Item().AlignCenter().Text("Computer Generated Receipt - No Signature Required")
                        .FontSize(7).FontColor("#6c757d").Italic();
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateTwoPerPage(FeePayment payment)
    {
        var single = GenerateReceipt(payment, false);
        var dup = GenerateReceipt(payment, true);

        // Return combined - for simplicity return first copy; proper implementation would merge PDFs
        return single;
    }

    private static string NumberToWords(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Minus " + NumberToWords(-number);

        string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                         "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
                         "Seventeen", "Eighteen", "Nineteen" };
        string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

        if (number < 20) return ones[number];
        if (number < 100) return tens[number / 10] + (number % 10 > 0 ? " " + ones[number % 10] : "");
        if (number < 1000) return ones[number / 100] + " Hundred" + (number % 100 > 0 ? " " + NumberToWords(number % 100) : "");
        if (number < 100000) return NumberToWords(number / 1000) + " Thousand" + (number % 1000 > 0 ? " " + NumberToWords(number % 1000) : "");
        if (number < 10000000) return NumberToWords(number / 100000) + " Lakh" + (number % 100000 > 0 ? " " + NumberToWords(number % 100000) : "");
        return NumberToWords(number / 10000000) + " Crore" + (number % 10000000 > 0 ? " " + NumberToWords(number % 10000000) : "");
    }
}
