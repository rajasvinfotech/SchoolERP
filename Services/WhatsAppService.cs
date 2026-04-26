using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class WhatsAppService
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;

    public WhatsAppService(AppDbContext db, SettingsService settings) { _db = db; _settings = settings; }

    public async Task<List<MessageTemplate>> GetTemplatesAsync(int schoolId) =>
        await _db.MessageTemplates.Where(t => t.SchoolId == schoolId && t.IsActive).ToListAsync();

    public async Task SaveTemplateAsync(MessageTemplate template)
    {
        if (template.Id == 0) _db.MessageTemplates.Add(template);
        else _db.MessageTemplates.Update(template);
        await _db.SaveChangesAsync();
    }

    public string BuildMessage(string template, Dictionary<string, string> vars)
    {
        var msg = template;
        foreach (var kv in vars)
            msg = msg.Replace($"{{{kv.Key}}}", kv.Value);
        return msg;
    }

    public string GetWhatsAppUrl(string phone, string message)
    {
        var normalized = phone.Replace("+", "").Replace("-", "").Replace(" ", "");
        if (normalized.Length == 10) normalized = "91" + normalized;
        var encoded = Uri.EscapeDataString(message);
        return $"https://wa.me/{normalized}?text={encoded}";
    }

    public async Task LogMessageAsync(MessageLog log)
    {
        _db.MessageLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<List<MessageLog>> GetLogsAsync(int schoolId, int page = 1, int pageSize = 20) =>
        await _db.MessageLogs
            .Where(l => l.SchoolId == schoolId)
            .OrderByDescending(l => l.SentAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

    public async Task SeedDefaultTemplatesAsync(int schoolId)
    {
        var exists = await _db.MessageTemplates.AnyAsync(t => t.SchoolId == schoolId);
        if (exists) return;

        var templates = new List<MessageTemplate>
        {
            new() {
                SchoolId = schoolId, Name = "Fee Due Reminder", Category = "FeeDue", IsDefault = true,
                Body = "Namaskar {father_name} ji,\nAapke bachche {student_name} ki {month} ki fees ₹{amount} abhi tak nahi aayi hai.\nKripya jald se jald fees jama karein.\n- {school_name}"
            },
            new() {
                SchoolId = schoolId, Name = "Fee Receipt", Category = "FeeReceipt", IsDefault = true,
                Body = "Namaskar {father_name} ji,\n{student_name} ki fees receipt:\nReceipt No: {receipt_no}\nAmount: ₹{amount}\nDate: {date}\nMonth: {month}\nThank you!\n- {school_name}"
            },
            new() {
                SchoolId = schoolId, Name = "Holiday Notice", Category = "Holiday", IsDefault = true,
                Body = "Namaskar Parents,\n{school_name} mein kal {date} ko {reason} ke kaaran CHUTTI rahegi.\n- {school_name} Management"
            },
            new() {
                SchoolId = schoolId, Name = "General Notice", Category = "General", IsDefault = true,
                Body = "Namaskar {father_name} ji,\n{message}\n- {school_name} Management"
            }
        };

        _db.MessageTemplates.AddRange(templates);
        await _db.SaveChangesAsync();
    }
}
