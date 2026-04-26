using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Data.Models;

public class Student
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;

    [Required, MaxLength(30)]
    public string AdmissionNumber { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(10)]
    public string? Gender { get; set; } // Male, Female, Other

    [MaxLength(200)]
    public string? PhotoPath { get; set; }

    [MaxLength(5)]
    public string? BloodGroup { get; set; }

    [MaxLength(20)]
    public string? AadharNumber { get; set; }

    public int? ClassId { get; set; }
    public Class? Class { get; set; }

    public int? SectionId { get; set; }
    public Section? Section { get; set; }

    [MaxLength(10)]
    public string? RollNumber { get; set; }

    public DateTime AdmissionDate { get; set; } = DateTime.Today;

    [MaxLength(20)]
    public string? AcademicYear { get; set; }

    [MaxLength(200)]
    public string? PreviousSchool { get; set; }

    // Parent Info
    [MaxLength(100)]
    public string? FatherName { get; set; }

    [MaxLength(15)]
    public string? FatherPhone { get; set; }

    [MaxLength(100)]
    public string? FatherOccupation { get; set; }

    [MaxLength(100)]
    public string? MotherName { get; set; }

    [MaxLength(15)]
    public string? MotherPhone { get; set; }

    [MaxLength(100)]
    public string? GuardianName { get; set; }

    [MaxLength(15)]
    public string? GuardianPhone { get; set; }

    [MaxLength(50)]
    public string? GuardianRelation { get; set; }

    // Address
    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(10)]
    public string? Pincode { get; set; }

    // Transport
    public bool TransportRequired { get; set; } = false;

    [MaxLength(100)]
    public string? TransportRoute { get; set; }

    [MaxLength(100)]
    public string? TransportStop { get; set; }

    // Status
    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Inactive, TransferOut, PassedOut

    public bool TcIssued { get; set; } = false;
    public DateTime? TcIssuedDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Concession
    public int? ConcessionCategoryId { get; set; }
    public ConcessionCategory? ConcessionCategory { get; set; }

    public ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();
}
