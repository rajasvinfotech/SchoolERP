using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Data.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Receptionist"; // SuperAdmin, SchoolAdmin, Accountant, Receptionist

    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }

    public ICollection<UserSchoolAccess> SchoolAccess { get; set; } = new List<UserSchoolAccess>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
