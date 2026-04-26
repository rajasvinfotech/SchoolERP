using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Data.Models;

public class School
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Type { get; set; } = "School"; // School, Coaching, Both

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [MaxLength(10)]
    public string? Pincode { get; set; }

    [MaxLength(200)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(100)]
    public string? PrincipalName { get; set; }

    [MaxLength(200)]
    public string? LogoPath { get; set; }

    [MaxLength(100)]
    public string? AffiliationNumber { get; set; }

    [MaxLength(50)]
    public string? Board { get; set; } // CBSE, ICSE, UP Board, Other

    [MaxLength(20)]
    public string? AcademicYear { get; set; } // e.g., 2024-25

    [MaxLength(20)]
    public string? ColorTheme { get; set; } = "#0d6efd";

    [MaxLength(20)]
    public string ReceiptPrefix { get; set; } = "RCP";
    public int LastReceiptNumber { get; set; } = 0;

    [MaxLength(20)]
    public string AdmissionPrefix { get; set; } = "ADM";
    public int LastAdmissionNumber { get; set; } = 0;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Class> Classes { get; set; } = new List<Class>();
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<FeeHead> FeeHeads { get; set; } = new List<FeeHead>();
    public ICollection<UserSchoolAccess> UserAccess { get; set; } = new List<UserSchoolAccess>();
}
