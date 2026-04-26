using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Components;
using SchoolERP.Data;
using SchoolERP.Services;

var builder = WebApplication.CreateBuilder(args);

// Windows Service support
builder.Host.UseWindowsService();

// Razor Components + Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SchoolERP.Auth";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Database
var dbPath = Path.Combine(AppContext.BaseDirectory, "SchoolERP.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"), ServiceLifetime.Scoped);

// Application Services
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SchoolService>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<ClassService>();
builder.Services.AddScoped<FeeService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<WhatsAppService>();
builder.Services.AddScoped<PrintService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<ExportImportService>();
builder.Services.AddScoped<AppState>();

// Background service for auto backup
builder.Services.AddSingleton<BackupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BackupService>());

// Blazored Toast
builder.Services.AddBlazoredToast();

// Listen on all interfaces for LAN access
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// Auto-migrate and seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Receipt PDF download endpoint
app.MapGet("/api/receipt/{id:int}", async (int id, AppDbContext db, PrintService print) =>
{
    var payment = await db.FeePayments
        .Include(p => p.Details).ThenInclude(d => d.FeeHead)
        .Include(p => p.Student).ThenInclude(s => s!.Class)
        .Include(p => p.Student).ThenInclude(s => s!.Section)
        .Include(p => p.School)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (payment == null) return Results.NotFound();
    var pdf = print.GenerateReceipt(payment);
    return Results.File(pdf, "application/pdf", $"Receipt_{payment.ReceiptNumber}.pdf");
});

app.MapGet("/api/receipt/{id:int}/duplicate", async (int id, AppDbContext db, PrintService print) =>
{
    var payment = await db.FeePayments
        .Include(p => p.Details).ThenInclude(d => d.FeeHead)
        .Include(p => p.Student).ThenInclude(s => s!.Class)
        .Include(p => p.Student).ThenInclude(s => s!.Section)
        .Include(p => p.School)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (payment == null) return Results.NotFound();
    var pdf = print.GenerateReceipt(payment, isDuplicate: true);
    return Results.File(pdf, "application/pdf", $"Receipt_{payment.ReceiptNumber}_Duplicate.pdf");
});

app.MapGet("/api/export/students/{schoolId:int}", async (int schoolId, AppDbContext db, ExcelService excel) =>
{
    var students = await db.Students.Include(s => s.Class).Include(s => s.Section)
        .Where(s => s.SchoolId == schoolId).ToListAsync();
    var school = await db.Schools.FindAsync(schoolId);
    var bytes = excel.ExportStudents(students, school?.Name ?? "");
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Students.xlsx");
});

app.MapGet("/api/export/students/template", (ExcelService excel) =>
{
    var bytes = excel.GetStudentImportTemplate();
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StudentImportTemplate.xlsx");
});

app.MapGet("/api/backup/download/{id:int}", async (int id, AppDbContext db) =>
{
    var log = await db.BackupLogs.FindAsync(id);
    if (log == null || !File.Exists(log.FilePath)) return Results.NotFound();
    var bytes = await File.ReadAllBytesAsync(log.FilePath);
    return Results.File(bytes, "application/octet-stream", log.FileName);
});

app.Run();
