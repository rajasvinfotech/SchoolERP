using Microsoft.EntityFrameworkCore;
using SchoolERP.Data.Models;

namespace SchoolERP.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSchoolAccess> UserSchoolAccess => Set<UserSchoolAccess>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<FeeHead> FeeHeads => Set<FeeHead>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<ConcessionCategory> ConcessionCategories => Set<ConcessionCategory>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<FeePaymentDetail> FeePaymentDetails => Set<FeePaymentDetail>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<MessageLog> MessageLogs => Set<MessageLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<BackupLog> BackupLogs => Set<BackupLog>();
    public DbSet<ExportImportLog> ExportImportLogs => Set<ExportImportLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserSchoolAccess>()
            .HasIndex(x => new { x.UserId, x.SchoolId })
            .IsUnique();

        modelBuilder.Entity<FeeStructure>()
            .HasIndex(x => new { x.ClassId, x.FeeHeadId, x.AcademicYear, x.SectionId })
            .IsUnique();

        modelBuilder.Entity<FeePayment>()
            .HasIndex(x => new { x.SchoolId, x.ReceiptNumber })
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasIndex(x => new { x.SchoolId, x.AdmissionNumber })
            .IsUnique();

        // Seed Super Admin
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            FullName = "Super Administrator",
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "SuperAdmin",
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = new DateTime(2024, 1, 1)
        });

        // Seed default settings
        modelBuilder.Entity<AppSetting>().HasData(
            new AppSetting { Id = 1, Key = "AppName", Value = "School ERP", Description = "Application Name" },
            new AppSetting { Id = 2, Key = "CurrencySymbol", Value = "₹", Description = "Currency Symbol" },
            new AppSetting { Id = 3, Key = "DateFormat", Value = "dd/MM/yyyy", Description = "Date Format" },
            new AppSetting { Id = 4, Key = "SessionTimeoutHours", Value = "8", Description = "Session timeout in hours" },
            new AppSetting { Id = 5, Key = "LateFinePerDay", Value = "10", Description = "Late fine per day (₹)" },
            new AppSetting { Id = 6, Key = "GracePeriodDays", Value = "5", Description = "Grace period before late fine" },
            new AppSetting { Id = 7, Key = "AutoBackupEnabled", Value = "true", Description = "Enable auto backup" },
            new AppSetting { Id = 8, Key = "AutoBackupTime", Value = "23:00", Description = "Auto backup time" },
            new AppSetting { Id = 9, Key = "BackupKeepDays", Value = "30", Description = "Keep backups for N days" },
            new AppSetting { Id = 10, Key = "BackupLocation", Value = @"C:\SchoolERP\Backups", Description = "Backup folder path" },
            new AppSetting { Id = 11, Key = "PenDriveAutoBackup", Value = "false", Description = "Auto backup to pen drive" },
            new AppSetting { Id = 12, Key = "PrintCopies", Value = "2", Description = "Number of receipt copies" },
            new AppSetting { Id = 13, Key = "PaperSize", Value = "A5", Description = "Receipt paper size" },
            new AppSetting { Id = 14, Key = "ReceiptFooter", Value = "Thank you for your payment", Description = "Receipt footer text" },
            new AppSetting { Id = 15, Key = "DuplicateWatermark", Value = "DUPLICATE", Description = "Watermark for duplicate receipt" },
            new AppSetting { Id = 16, Key = "WhatsAppProvider", Value = "None", Description = "WhatsApp API provider" },
            new AppSetting { Id = 17, Key = "WhatsAppApiKey", Value = "", Description = "WhatsApp API key" },
            new AppSetting { Id = 18, Key = "WhatsAppDelaySec", Value = "3", Description = "Delay between WhatsApp messages" },
            new AppSetting { Id = 19, Key = "AutoCarryForwardDue", Value = "true", Description = "Auto carry forward dues" },
            new AppSetting { Id = 20, Key = "BackupPassword", Value = "SchoolERP2024", Description = "Default backup encryption password" }
        );
    }
}
