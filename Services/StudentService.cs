using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class StudentService
{
    private readonly AppDbContext _db;
    private readonly SchoolService _schoolService;

    public StudentService(AppDbContext db, SchoolService schoolService)
    {
        _db = db;
        _schoolService = schoolService;
    }

    public IQueryable<Student> QueryStudents(int schoolId) =>
        _db.Students
            .Include(s => s.Class)
            .Include(s => s.Section)
            .Include(s => s.ConcessionCategory)
            .Where(s => s.SchoolId == schoolId);

    public async Task<(List<Student> Items, int Total)> GetPagedAsync(
        int schoolId, string? search, int? classId, int? sectionId,
        string? status, int page, int pageSize)
    {
        var q = QueryStudents(schoolId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            q = q.Where(x => x.FullName.ToLower().Contains(s)
                || x.AdmissionNumber.ToLower().Contains(s)
                || (x.FatherName != null && x.FatherName.ToLower().Contains(s))
                || (x.FatherPhone != null && x.FatherPhone.Contains(s)));
        }

        if (classId.HasValue) q = q.Where(x => x.ClassId == classId);
        if (sectionId.HasValue) q = q.Where(x => x.SectionId == sectionId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(x => x.Status == status);

        var total = await q.CountAsync();
        var items = await q.OrderBy(x => x.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, total);
    }

    public async Task<Student?> GetByIdAsync(int id) =>
        await _db.Students
            .Include(s => s.School)
            .Include(s => s.Class)
            .Include(s => s.Section)
            .Include(s => s.ConcessionCategory)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<Student>> SearchAsync(int schoolId, string term) =>
        await _db.Students
            .Include(s => s.Class)
            .Include(s => s.Section)
            .Where(s => s.SchoolId == schoolId && s.Status == "Active"
                && (s.FullName.ToLower().Contains(term.ToLower())
                    || s.AdmissionNumber.ToLower().Contains(term.ToLower())))
            .Take(10)
            .ToListAsync();

    public async Task<Student> CreateAsync(Student student)
    {
        if (string.IsNullOrEmpty(student.AdmissionNumber))
            student.AdmissionNumber = await _schoolService.GetNextAdmissionNumberAsync(student.SchoolId);

        student.CreatedAt = DateTime.Now;
        student.UpdatedAt = DateTime.Now;
        _db.Students.Add(student);
        await _db.SaveChangesAsync();
        return student;
    }

    public async Task UpdateAsync(Student student)
    {
        student.UpdatedAt = DateTime.Now;
        _db.Students.Update(student);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> AdmissionNumberExistsAsync(int schoolId, string admNo, int? excludeId = null)
    {
        var q = _db.Students.Where(s => s.SchoolId == schoolId && s.AdmissionNumber == admNo);
        if (excludeId.HasValue) q = q.Where(s => s.Id != excludeId.Value);
        return await q.AnyAsync();
    }

    public async Task<List<Student>> GetBirthdaysThisMonthAsync(int schoolId)
    {
        var month = DateTime.Today.Month;
        return await _db.Students
            .Include(s => s.Class)
            .Where(s => s.SchoolId == schoolId && s.Status == "Active"
                && s.DateOfBirth != null && s.DateOfBirth.Value.Month == month)
            .OrderBy(s => s.DateOfBirth!.Value.Day)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync(int schoolId, string status = "Active") =>
        await _db.Students.CountAsync(s => s.SchoolId == schoolId && s.Status == status);
}
