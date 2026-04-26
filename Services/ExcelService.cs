using ClosedXML.Excel;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class ExcelService
{
    public byte[] ExportStudents(List<Student> students, string schoolName)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Students");

        // Header row
        var headers = new[] { "Adm No", "Name", "Class", "Section", "Roll No", "DOB", "Gender",
            "Father Name", "Father Phone", "Mother Name", "Mother Phone", "Address", "City", "Status", "Admission Date" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var s in students)
        {
            ws.Cell(row, 1).Value = s.AdmissionNumber;
            ws.Cell(row, 2).Value = s.FullName;
            ws.Cell(row, 3).Value = s.Class?.Name ?? "";
            ws.Cell(row, 4).Value = s.Section?.Name ?? "";
            ws.Cell(row, 5).Value = s.RollNumber ?? "";
            ws.Cell(row, 6).Value = s.DateOfBirth?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(row, 7).Value = s.Gender ?? "";
            ws.Cell(row, 8).Value = s.FatherName ?? "";
            ws.Cell(row, 9).Value = s.FatherPhone ?? "";
            ws.Cell(row, 10).Value = s.MotherName ?? "";
            ws.Cell(row, 11).Value = s.MotherPhone ?? "";
            ws.Cell(row, 12).Value = s.Address ?? "";
            ws.Cell(row, 13).Value = s.City ?? "";
            ws.Cell(row, 14).Value = s.Status;
            ws.Cell(row, 15).Value = s.AdmissionDate.ToString("dd/MM/yyyy");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportFeeReport(List<FeePayment> payments, string title)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Fee Report");

        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 8).Merge();

        var headers = new[] { "Receipt No", "Date", "Student", "Class", "Month", "Amount", "Mode", "Collected By" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(2, i + 1).Value = headers[i];
            ws.Cell(2, i + 1).Style.Font.Bold = true;
            ws.Cell(2, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0d6efd");
            ws.Cell(2, i + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 3;
        foreach (var p in payments)
        {
            ws.Cell(row, 1).Value = p.ReceiptNumber;
            ws.Cell(row, 2).Value = p.PaymentDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = p.Student?.FullName ?? "";
            ws.Cell(row, 4).Value = p.Student?.Class?.Name ?? "";
            ws.Cell(row, 5).Value = p.Month;
            ws.Cell(row, 6).Value = p.NetAmount;
            ws.Cell(row, 6).Style.NumberFormat.Format = "₹#,##0";
            ws.Cell(row, 7).Value = p.PaymentMode;
            ws.Cell(row, 8).Value = p.CollectedBy ?? "";
            row++;
        }

        // Total row
        ws.Cell(row, 5).Value = "TOTAL";
        ws.Cell(row, 5).Style.Font.Bold = true;
        ws.Cell(row, 6).Value = payments.Sum(p => p.NetAmount);
        ws.Cell(row, 6).Style.Font.Bold = true;
        ws.Cell(row, 6).Style.NumberFormat.Format = "₹#,##0";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public List<StudentImportRow> ParseStudentImport(Stream fileStream)
    {
        var rows = new List<StudentImportRow>();
        using var wb = new XLWorkbook(fileStream);
        var ws = wb.Worksheets.First();
        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            string Get(int col) => ws.Cell(r, col).GetString().Trim();

            var fullName = Get(1);
            if (string.IsNullOrEmpty(fullName)) continue;

            var row = new StudentImportRow
            {
                RowNumber = r,
                FullName = fullName,
                FatherName = Get(2),
                FatherPhone = Get(3),
                MotherName = Get(4),
                MotherPhone = Get(5),
                Gender = Get(7),
                BloodGroup = Get(8),
                Address = Get(9),
                City = Get(10),
                Pincode = Get(11),
                RollNumber = Get(12),
                PreviousSchool = Get(13),
                TransportRoute = Get(15),
                TransportStop = Get(16)
            };

            var dobStr = Get(6);
            if (DateTime.TryParseExact(dobStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var dob))
                row.DateOfBirth = dob;
            else if (!string.IsNullOrEmpty(dobStr))
                row.Errors.Add($"Invalid DOB: {dobStr}");

            var transport = Get(14).ToLower();
            row.TransportRequired = transport == "yes" || transport == "true" || transport == "1";

            if (string.IsNullOrEmpty(row.FullName)) row.Errors.Add("Full Name is required");
            if (string.IsNullOrEmpty(row.FatherName)) row.Errors.Add("Father Name is required");
            if (string.IsNullOrEmpty(row.FatherPhone)) row.Errors.Add("Father Phone is required");

            rows.Add(row);
        }

        return rows;
    }

    public byte[] GetStudentImportTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Students");

        var headers = new[] { "Full Name*", "Father Name*", "Father Phone*", "Mother Name",
            "Mother Phone", "Date of Birth (dd/MM/yyyy)", "Gender (Male/Female/Other)",
            "Blood Group", "Address", "City", "Pincode", "Roll Number",
            "Previous School", "Transport Required (Yes/No)", "Transport Route", "Transport Stop" };

        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#198754");
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        // Sample row
        ws.Cell(2, 1).Value = "Rahul Kumar";
        ws.Cell(2, 2).Value = "Suresh Kumar";
        ws.Cell(2, 3).Value = "9876543210";

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}

public class StudentImportRow
{
    public int RowNumber { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public string? FatherPhone { get; set; }
    public string? MotherName { get; set; }
    public string? MotherPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? BloodGroup { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Pincode { get; set; }
    public string? RollNumber { get; set; }
    public string? PreviousSchool { get; set; }
    public bool TransportRequired { get; set; }
    public string? TransportRoute { get; set; }
    public string? TransportStop { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool IsValid => Errors.Count == 0;
}
