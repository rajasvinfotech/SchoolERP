using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int userId, string username, string action, string? entityType, int? entityId, string? details)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            CreatedAt = DateTime.Now
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetLogsAsync(int? userId = null, string? action = null, int page = 1, int pageSize = 50)
    {
        var query = _db.AuditLogs.Include(a => a.User).AsQueryable();
        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
        return await query.OrderByDescending(a => a.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }
}
