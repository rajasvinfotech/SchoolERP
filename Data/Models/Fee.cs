using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Data.Models;

public class FeeHead
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // Tuition Fee, Transport Fee, etc.

    [MaxLength(20)]
    public string Type { get; set; } = "Monthly"; // Monthly, Quarterly, Yearly, OneTime

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();
}

public class FeeStructure
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;
    public int FeeHeadId { get; set; }
    public FeeHead FeeHead { get; set; } = null!;

    public decimal Amount { get; set; } = 0;

    [MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;

    public int? SectionId { get; set; }
    public Section? Section { get; set; }
}

public class ConcessionCategory
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty; // Staff Ward, Sibling, Merit

    public decimal DiscountPercent { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
