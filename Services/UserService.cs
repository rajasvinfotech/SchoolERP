using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public UserService(AppDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public async Task<List<User>> GetAllAsync(string? search = null)
    {
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            var s = search.ToLower();
            q = q.Where(u => u.FullName.ToLower().Contains(s) || u.Username.ToLower().Contains(s));
        }
        return await q.OrderBy(u => u.FullName).ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id) =>
        await _db.Users.Include(u => u.SchoolAccess).ThenInclude(a => a.School).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User> CreateAsync(User user, List<int> schoolIds, int performedBy)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        foreach (var sid in schoolIds)
            _db.UserSchoolAccess.Add(new UserSchoolAccess { UserId = user.Id, SchoolId = sid });
        await _db.SaveChangesAsync();

        var by = await _db.Users.FindAsync(performedBy);
        await _audit.LogAsync(performedBy, by?.Username ?? "?", "CreateUser", "User", user.Id, $"Created user {user.Username}");
        return user;
    }

    public async Task UpdateAsync(User user, List<int> schoolIds, int performedBy)
    {
        _db.Users.Update(user);

        var existing = _db.UserSchoolAccess.Where(a => a.UserId == user.Id);
        _db.UserSchoolAccess.RemoveRange(existing);

        foreach (var sid in schoolIds)
            _db.UserSchoolAccess.Add(new UserSchoolAccess { UserId = user.Id, SchoolId = sid });

        await _db.SaveChangesAsync();

        var by = await _db.Users.FindAsync(performedBy);
        await _audit.LogAsync(performedBy, by?.Username ?? "?", "UpdateUser", "User", user.Id, $"Updated user {user.Username}");
    }

    public async Task<bool> UsernameExistsAsync(string username, int? excludeId = null)
    {
        var q = _db.Users.Where(u => u.Username == username);
        if (excludeId.HasValue) q = q.Where(u => u.Id != excludeId.Value);
        return await q.AnyAsync();
    }
}
