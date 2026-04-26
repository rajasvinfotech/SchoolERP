using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class ClassService
{
    private readonly AppDbContext _db;
    public ClassService(AppDbContext db) { _db = db; }

    public async Task<List<Class>> GetClassesAsync(int schoolId) =>
        await _db.Classes.Include(c => c.Sections)
            .Where(c => c.SchoolId == schoolId && c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();

    public async Task<Class?> GetClassByIdAsync(int id) =>
        await _db.Classes.Include(c => c.Sections).FirstOrDefaultAsync(c => c.Id == id);

    public async Task SaveClassAsync(Class cls)
    {
        if (cls.Id == 0) _db.Classes.Add(cls);
        else _db.Classes.Update(cls);
        await _db.SaveChangesAsync();
    }

    public async Task SaveSectionAsync(Section section)
    {
        if (section.Id == 0) _db.Sections.Add(section);
        else _db.Sections.Update(section);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteSectionAsync(int id)
    {
        var s = await _db.Sections.FindAsync(id);
        if (s != null) { s.IsActive = false; await _db.SaveChangesAsync(); }
    }

    public async Task<List<Section>> GetSectionsAsync(int classId) =>
        await _db.Sections.Where(s => s.ClassId == classId && s.IsActive).ToListAsync();
}
