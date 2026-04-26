using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class SettingsService
{
    private readonly AppDbContext _db;
    private Dictionary<string, string>? _cache;

    public SettingsService(AppDbContext db) { _db = db; }

    private async Task EnsureCacheAsync()
    {
        if (_cache == null)
        {
            var settings = await _db.AppSettings.ToListAsync();
            _cache = settings.ToDictionary(s => s.Key, s => s.Value ?? string.Empty);
        }
    }

    public async Task<string> GetAsync(string key, string defaultValue = "")
    {
        await EnsureCacheAsync();
        return _cache!.TryGetValue(key, out var val) ? val : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var val = await GetAsync(key);
        return string.IsNullOrEmpty(val) ? defaultValue : val.ToLower() == "true";
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var val = await GetAsync(key);
        return int.TryParse(val, out var i) ? i : defaultValue;
    }

    public async Task SetAsync(string key, string value, int? schoolId = null)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key && s.SchoolId == schoolId);
        if (setting == null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value, SchoolId = schoolId });
        }
        else
        {
            setting.Value = value;
            _db.AppSettings.Update(setting);
        }
        await _db.SaveChangesAsync();
        _cache = null; // invalidate cache
    }

    public async Task<Dictionary<string, string>> GetAllAsync()
    {
        _cache = null; // force reload
        await EnsureCacheAsync();
        return new Dictionary<string, string>(_cache!);
    }
}
