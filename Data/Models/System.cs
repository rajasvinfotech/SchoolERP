using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Data.Models;

public class AppSetting
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public int? SchoolId { get; set; } // null = global setting
}

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Login, Logout, CreateStudent, CollectFee, etc.

    [MaxLength(100)]
    public string? EntityType { get; set; }

    public int? EntityId { get; set; }

    [MaxLength(1000)]
    public string? Details { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class AcademicYear
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, MaxLength(20)]
    public string Year { get; set; } = string.Empty; // 2024-25

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; } = false;
}

public class BackupLog
{
    public int Id { get; set; }

    [MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [MaxLength(20)]
    public string BackupType { get; set; } = "Manual"; // Auto, Manual, PenDrive

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;
}

public class ExportImportLog
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string Type { get; set; } = "Export"; // Export, Import

    [MaxLength(300)]
    public string? FileName { get; set; }

    [MaxLength(20)]
    public string? ExportType { get; set; } // Full, SchoolWise, DateRange, StudentsOnly

    public int? SchoolId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public int StudentsCount { get; set; }
    public int FeeRecordsCount { get; set; }
    public int SchoolsCount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? PerformedBy { get; set; }

    public int PerformedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string Status { get; set; } = "Success"; // Success, Failed
}
