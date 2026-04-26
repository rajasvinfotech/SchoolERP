using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;
using System.Security.Claims;

namespace SchoolERP.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public AuthService(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(bool Success, string Message, User? User)> ValidateLoginAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return (false, "Invalid username or password.", null);

        if (!user.IsActive)
            return (false, "Account is disabled. Contact administrator.", null);

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.Now)
        {
            var remaining = (user.LockoutEnd.Value - DateTime.Now).Minutes + 1;
            return (false, $"Account locked. Try after {remaining} minutes.", null);
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(30);
                user.FailedLoginAttempts = 0;
                await _db.SaveChangesAsync();
                return (false, "Too many failed attempts. Account locked for 30 minutes.", null);
            }
            await _db.SaveChangesAsync();
            return (false, $"Invalid password. {5 - user.FailedLoginAttempts} attempts remaining.", null);
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.Now;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(user.Id, user.Username, "Login", null, null, "User logged in");

        return (true, "Login successful.", user);
    }

    public async Task<List<Claim>> GetClaimsAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("FullName", user.FullName),
            new(ClaimTypes.Role, user.Role),
            new("MustChangePassword", user.MustChangePassword.ToString())
        };

        if (user.Role == "SuperAdmin")
        {
            // SuperAdmin gets access to all schools
            var schoolIds = await _db.Schools.Where(s => s.IsActive).Select(s => s.Id).ToListAsync();
            foreach (var id in schoolIds)
                claims.Add(new Claim("SchoolAccess", id.ToString()));
        }
        else
        {
            var schoolIds = await _db.UserSchoolAccess
                .Where(a => a.UserId == user.Id)
                .Select(a => a.SchoolId)
                .ToListAsync();
            foreach (var id in schoolIds)
                claims.Add(new Claim("SchoolAccess", id.ToString()));
        }

        return claims;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return false;
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ResetPasswordAsync(int userId, string newPassword, int performedByUserId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = true;
        await _db.SaveChangesAsync();

        var by = await _db.Users.FindAsync(performedByUserId);
        await _audit.LogAsync(performedByUserId, by?.Username ?? "?", "ResetPassword", "User", userId, $"Reset password for {user.Username}");
    }
}
