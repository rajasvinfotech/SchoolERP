using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SchoolERP.Services;

public class BackupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupService> _logger;

    public BackupService(IServiceScopeFactory scopeFactory, ILogger<BackupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            await CheckAutoBackupAsync();
        }
    }

    private async Task CheckAutoBackupAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<SettingsService>();

            var autoEnabled = await settings.GetBoolAsync("AutoBackupEnabled", true);
            if (!autoEnabled) return;

            var timeStr = await settings.GetAsync("AutoBackupTime", "23:00");
            if (!TimeOnly.TryParse(timeStr, out var backupTime)) return;

            var now = TimeOnly.FromDateTime(DateTime.Now);
            if (Math.Abs((now - backupTime).TotalMinutes) > 5) return;

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var lastAuto = await db.BackupLogs
                .Where(b => b.BackupType == "Auto")
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastAuto != null && lastAuto.CreatedAt.Date == DateTime.Today) return;

            await TakeBackupAsync("Auto", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto backup failed");
        }
    }

    public async Task<BackupLog> TakeBackupAsync(string type = "Manual", string? performedBy = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<SettingsService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var backupDir = type == "Manual"
            ? Path.Combine(await settings.GetAsync("BackupLocation", @"C:\SchoolERP\Backups"), "Manual")
            : Path.Combine(await settings.GetAsync("BackupLocation", @"C:\SchoolERP\Backups"), "Daily");

        Directory.CreateDirectory(backupDir);

        var dbPath = Path.Combine(AppContext.BaseDirectory, "SchoolERP.db");
        var fileName = $"SchoolERP_{DateTime.Now:yyyy_MM_dd_HHmm}_{type}.db";
        var destPath = Path.Combine(backupDir, fileName);

        // SQLite file copy (WAL mode safe)
        File.Copy(dbPath, destPath, true);

        var fileInfo = new FileInfo(destPath);
        var log = new BackupLog
        {
            FileName = fileName,
            FilePath = destPath,
            FileSizeBytes = fileInfo.Length,
            BackupType = type,
            CreatedBy = performedBy,
            CreatedAt = DateTime.Now
        };

        db.BackupLogs.Add(log);
        await db.SaveChangesAsync();

        // Cleanup old backups
        await CleanupOldBackupsAsync(backupDir, await settings.GetIntAsync("BackupKeepDays", 30), db);

        return log;
    }

    private async Task CleanupOldBackupsAsync(string dir, int keepDays, AppDbContext db)
    {
        var cutoff = DateTime.Now.AddDays(-keepDays);
        var oldLogs = await db.BackupLogs
            .Where(b => b.CreatedAt < cutoff && !b.IsDeleted)
            .ToListAsync();

        foreach (var log in oldLogs)
        {
            if (File.Exists(log.FilePath))
                File.Delete(log.FilePath);
            log.IsDeleted = true;
        }
        await db.SaveChangesAsync();
    }

    public async Task<List<BackupLog>> GetBackupHistoryAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.BackupLogs
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<BackupLog?> GetLastBackupAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.BackupLogs
            .Where(b => !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public List<(string Letter, string Label, long FreeBytes)> GetAvailableDrives()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Removable)
            .Select(d => (d.RootDirectory.FullName.TrimEnd('\\'), $"{d.VolumeLabel} ({d.RootDirectory.FullName})", d.AvailableFreeSpace))
            .ToList();
        return drives;
    }

    public async Task BackupToPenDriveAsync(string drivePath, string? performedBy = null)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "SchoolERP.db");
        var destDir = Path.Combine(drivePath, "SchoolERP_Backup");
        Directory.CreateDirectory(destDir);

        var fileName = $"SchoolERP_{DateTime.Now:yyyy_MM_dd_HHmm}_PenDrive.db";
        var destPath = Path.Combine(destDir, fileName);
        File.Copy(dbPath, destPath, true);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var fileInfo = new FileInfo(destPath);

        db.BackupLogs.Add(new BackupLog
        {
            FileName = fileName,
            FilePath = destPath,
            FileSizeBytes = fileInfo.Length,
            BackupType = "PenDrive",
            CreatedBy = performedBy,
            CreatedAt = DateTime.Now
        });
        await db.SaveChangesAsync();
    }

    public async Task RestoreFromFileAsync(string filePath)
    {
        // Take safety backup first
        await TakeBackupAsync("Manual", "System (before restore)");

        var dbPath = Path.Combine(AppContext.BaseDirectory, "SchoolERP.db");
        File.Copy(filePath, dbPath, true);
    }
}

// Export/Import Service
public class ExportImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ExportImportService> _logger;

    public ExportImportService(AppDbContext db, ILogger<ExportImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(int Students, int FeeRecords, int Schools, long EstimatedBytes)> PreviewExportAsync(
        int? schoolId = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var studentQ = _db.Students.AsQueryable();
        var feeQ = _db.FeePayments.AsQueryable();
        var schoolQ = _db.Schools.AsQueryable();

        if (schoolId.HasValue)
        {
            studentQ = studentQ.Where(s => s.SchoolId == schoolId);
            feeQ = feeQ.Where(f => f.SchoolId == schoolId);
            schoolQ = schoolQ.Where(s => s.Id == schoolId);
        }
        if (dateFrom.HasValue) feeQ = feeQ.Where(f => f.PaymentDate >= dateFrom);
        if (dateTo.HasValue) feeQ = feeQ.Where(f => f.PaymentDate <= dateTo);

        int students = await studentQ.CountAsync();
        int fees = await feeQ.CountAsync();
        int schools = await schoolQ.CountAsync();
        long estimated = (students * 500L) + (fees * 300L) + (schools * 200L);

        return (students, fees, schools, estimated);
    }

    public async Task<byte[]> ExportAsync(string password, int? schoolId = null,
        DateTime? dateFrom = null, DateTime? dateTo = null, bool includeStudents = true,
        bool includePayments = true, bool includeStructures = true, bool includeSettings = true)
    {
        var exportData = new Dictionary<string, object>();

        // Manifest
        exportData["manifest"] = new
        {
            version = "1.0",
            exportDate = DateTime.Now.ToString("o"),
            exportedBy = "SchoolERP",
            appVersion = "1.0",
            schoolId,
            dateFrom,
            dateTo
        };

        // Schools
        var schoolQ = _db.Schools.AsQueryable();
        if (schoolId.HasValue) schoolQ = schoolQ.Where(s => s.Id == schoolId);
        exportData["schools"] = await schoolQ.ToListAsync();

        if (includeStudents)
        {
            var studentQ = _db.Students.AsQueryable();
            if (schoolId.HasValue) studentQ = studentQ.Where(s => s.SchoolId == schoolId);
            exportData["students"] = await studentQ.ToListAsync();
        }

        if (includePayments)
        {
            var feeQ = _db.FeePayments.Include(f => f.Details).AsQueryable();
            if (schoolId.HasValue) feeQ = feeQ.Where(f => f.SchoolId == schoolId);
            if (dateFrom.HasValue) feeQ = feeQ.Where(f => f.PaymentDate >= dateFrom);
            if (dateTo.HasValue) feeQ = feeQ.Where(f => f.PaymentDate <= dateTo);
            exportData["fee_payments"] = await feeQ.ToListAsync();
        }

        if (includeStructures)
        {
            var classQ = _db.Classes.Include(c => c.Sections).AsQueryable();
            if (schoolId.HasValue) classQ = classQ.Where(c => c.SchoolId == schoolId);
            exportData["classes"] = await classQ.ToListAsync();

            var feeHeadQ = _db.FeeHeads.AsQueryable();
            if (schoolId.HasValue) feeHeadQ = feeHeadQ.Where(f => f.SchoolId == schoolId);
            exportData["fee_heads"] = await feeHeadQ.ToListAsync();
            exportData["fee_structures"] = await _db.FeeStructures.ToListAsync();
        }

        if (includeSettings)
        {
            exportData["settings"] = await _db.AppSettings.ToListAsync();
            exportData["concession_categories"] = await _db.ConcessionCategories.ToListAsync();
        }

        exportData["message_templates"] = await _db.MessageTemplates.ToListAsync();

        // Serialize to JSON
        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = false });
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        // Compute checksum
        var checksum = Convert.ToHexString(SHA256.HashData(jsonBytes));

        // Create ZIP
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            // Add data
            var dataEntry = zip.CreateEntry("data.json", CompressionLevel.Optimal);
            using (var entryStream = dataEntry.Open())
                await entryStream.WriteAsync(jsonBytes);

            // Add manifest with checksum
            var manifestEntry = zip.CreateEntry("manifest.json", CompressionLevel.Optimal);
            var manifestBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { checksum, exportDate = DateTime.Now }));
            using (var entryStream = manifestEntry.Open())
                await entryStream.WriteAsync(manifestBytes);
        }

        var zipBytes = ms.ToArray();

        // Encrypt
        return EncryptBytes(zipBytes, password);
    }

    public async Task<(bool Valid, string Info)> VerifyImportFileAsync(byte[] fileBytes, string password)
    {
        try
        {
            var decrypted = DecryptBytes(fileBytes, password);
            using var ms = new MemoryStream(decrypted);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

            var manifestEntry = zip.GetEntry("manifest.json");
            var dataEntry = zip.GetEntry("data.json");

            if (manifestEntry == null || dataEntry == null)
                return (false, "Invalid file format.");

            using var dataStream = dataEntry.Open();
            var dataBytes = new byte[dataEntry.Length];
            await dataStream.ReadExactlyAsync(dataBytes);

            var checksum = Convert.ToHexString(SHA256.HashData(dataBytes));

            using var manifestStream = manifestEntry.Open();
            var manifestBytes = new byte[manifestEntry.Length];
            await manifestStream.ReadExactlyAsync(manifestBytes);
            var manifestJson = JsonSerializer.Deserialize<JsonElement>(manifestBytes);

            var storedChecksum = manifestJson.GetProperty("checksum").GetString();
            if (storedChecksum != checksum)
                return (false, "File integrity check failed. File may be corrupted.");

            return (true, "File verified successfully.");
        }
        catch (CryptographicException)
        {
            return (false, "Invalid password.");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    public async Task<ImportResult> ImportFreshAsync(byte[] fileBytes, string password, int performedByUserId)
    {
        var result = new ImportResult();
        try
        {
            var decrypted = DecryptBytes(fileBytes, password);
            using var ms = new MemoryStream(decrypted);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

            var dataEntry = zip.GetEntry("data.json")!;
            using var dataStream = dataEntry.Open();
            var dataBytes = new byte[dataEntry.Length];
            await dataStream.ReadExactlyAsync(dataBytes);

            var data = JsonSerializer.Deserialize<JsonElement>(dataBytes);

            // Import Schools
            if (data.TryGetProperty("schools", out var schoolsEl))
            {
                var schools = JsonSerializer.Deserialize<List<School>>(schoolsEl.GetRawText()) ?? [];
                foreach (var school in schools)
                {
                    var existing = await _db.Schools.FindAsync(school.Id);
                    if (existing == null)
                    {
                        _db.Schools.Add(school);
                        result.SchoolsImported++;
                    }
                }
                await _db.SaveChangesAsync();
            }

            // Import Students
            if (data.TryGetProperty("students", out var studentsEl))
            {
                var students = JsonSerializer.Deserialize<List<Student>>(studentsEl.GetRawText()) ?? [];
                foreach (var student in students)
                {
                    student.ClassId = null;
                    student.SectionId = null;
                    student.ConcessionCategoryId = null;
                    var exists = await _db.Students.AnyAsync(s => s.SchoolId == student.SchoolId && s.AdmissionNumber == student.AdmissionNumber);
                    if (!exists)
                    {
                        _db.Students.Add(student);
                        result.StudentsImported++;
                    }
                    else result.StudentsSkipped++;
                }
                await _db.SaveChangesAsync();
            }

            // Import Fee Payments
            if (data.TryGetProperty("fee_payments", out var feesEl))
            {
                var payments = JsonSerializer.Deserialize<List<FeePayment>>(feesEl.GetRawText()) ?? [];
                foreach (var payment in payments)
                {
                    var exists = await _db.FeePayments.AnyAsync(p => p.SchoolId == payment.SchoolId && p.ReceiptNumber == payment.ReceiptNumber);
                    if (!exists)
                    {
                        var details = payment.Details.ToList();
                        payment.Details.Clear();
                        _db.FeePayments.Add(payment);
                        await _db.SaveChangesAsync();

                        foreach (var d in details)
                        {
                            d.FeePaymentId = payment.Id;
                            _db.FeePaymentDetails.Add(d);
                        }
                        await _db.SaveChangesAsync();
                        result.FeeRecordsImported++;
                    }
                }
            }

            result.Success = true;
            result.Message = "Import completed successfully.";

            _db.ExportImportLogs.Add(new ExportImportLog
            {
                Type = "Import",
                StudentsCount = result.StudentsImported,
                FeeRecordsCount = result.FeeRecordsImported,
                SchoolsCount = result.SchoolsImported,
                PerformedByUserId = performedByUserId,
                Status = "Success",
                Notes = $"Fresh import. Skipped {result.StudentsSkipped} duplicate students."
            });
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Import failed: {ex.Message}";
            result.Errors.Add(ex.Message);
        }
        return result;
    }

    private static byte[] EncryptBytes(byte[] data, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var key = DeriveKey(password, salt);
        var iv = RandomNumberGenerator.GetBytes(16);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream();
        ms.Write(salt, 0, 16);
        ms.Write(iv, 0, 16);

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            cs.Write(data, 0, data.Length);

        return ms.ToArray();
    }

    private static byte[] DecryptBytes(byte[] data, string password)
    {
        var salt = data[..16];
        var iv = data[16..32];
        var encrypted = data[32..];

        var key = DeriveKey(password, salt);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream(encrypted);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }

    private static byte[] DeriveKey(string password, byte[] salt) =>
        new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256).GetBytes(32);
}

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SchoolsImported { get; set; }
    public int StudentsImported { get; set; }
    public int StudentsSkipped { get; set; }
    public int FeeRecordsImported { get; set; }
    public List<string> Errors { get; set; } = new();
}
