using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Data.Models;

public class Class
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty; // Nursery, LKG, 1, 2, ... 12, Morning Batch, etc.

    public int SortOrder { get; set; } = 0;

    [MaxLength(100)]
    public string? ClassTeacher { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();
}

public class Section
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public Class Class { get; set; } = null!;

    [Required, MaxLength(10)]
    public string Name { get; set; } = string.Empty; // A, B, C, etc.

    [MaxLength(100)]
    public string? ClassTeacher { get; set; }

    public int Capacity { get; set; } = 40;
    public bool IsActive { get; set; } = true;

    public ICollection<Student> Students { get; set; } = new List<Student>();
}
