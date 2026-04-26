using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class SchoolService
{
    private readonly AppDbContext _db;

    public SchoolService(AppDbContext db) { _db = db; }

    public async Task<List<School>> GetAllAsync() =>
        await _db.Schools.OrderBy(s => s.Name).ToListAsync();

    public async Task<List<School>> GetByUserAsync(int userId, string role)
    {
        if (role == "SuperAdmin")
            return await _db.Schools.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

        var ids = await _db.UserSchoolAccess.Where(a => a.UserId == userId).Select(a => a.SchoolId).ToListAsync();
        return await _db.Schools.Where(s => ids.Contains(s.Id) && s.IsActive).OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<School?> GetByIdAsync(int id) =>
        await _db.Schools.FirstOrDefaultAsync(s => s.Id == id);

    public async Task<School> CreateAsync(School school)
    {
        _db.Schools.Add(school);
        await _db.SaveChangesAsync();
        return school;
    }

    public async Task UpdateAsync(School school)
    {
        _db.Schools.Update(school);
        await _db.SaveChangesAsync();
    }

    public async Task<string> GetNextReceiptNumberAsync(int schoolId)
    {
        var school = await _db.Schools.FindAsync(schoolId);
        if (school == null) return "ERR-000";
        school.LastReceiptNumber++;
        await _db.SaveChangesAsync();
        return $"{school.ReceiptPrefix}-{DateTime.Now.Year}-{school.LastReceiptNumber:D4}";
    }

    public async Task<string> GetNextAdmissionNumberAsync(int schoolId)
    {
        var school = await _db.Schools.FindAsync(schoolId);
        if (school == null) return "ERR-000";
        school.LastAdmissionNumber++;
        await _db.SaveChangesAsync();
        return $"{school.AdmissionPrefix}-{DateTime.Now.Year}-{school.LastAdmissionNumber:D4}";
    }
}
