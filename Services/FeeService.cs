using Microsoft.EntityFrameworkCore;
using SchoolERP.Data;
using SchoolERP.Data.Models;

namespace SchoolERP.Services;

public class FeeService
{
    private readonly AppDbContext _db;
    private readonly SchoolService _schoolService;
    private readonly AuditService _audit;
    private readonly SettingsService _settings;

    public FeeService(AppDbContext db, SchoolService schoolService, AuditService audit, SettingsService settings)
    {
        _db = db;
        _schoolService = schoolService;
        _audit = audit;
        _settings = settings;
    }

    // Fee Heads
    public async Task<List<FeeHead>> GetFeeHeadsAsync(int schoolId) =>
        await _db.FeeHeads.Where(f => f.SchoolId == schoolId && f.IsActive)
            .OrderBy(f => f.SortOrder).ThenBy(f => f.Name).ToListAsync();

    public async Task SaveFeeHeadAsync(FeeHead head)
    {
        if (head.Id == 0) _db.FeeHeads.Add(head);
        else _db.FeeHeads.Update(head);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteFeeHeadAsync(int id)
    {
        var fh = await _db.FeeHeads.FindAsync(id);
        if (fh != null) { fh.IsActive = false; await _db.SaveChangesAsync(); }
    }

    // Fee Structures
    public async Task<List<FeeStructure>> GetFeeStructureAsync(int classId, string academicYear) =>
        await _db.FeeStructures
            .Include(f => f.FeeHead)
            .Where(f => f.ClassId == classId && f.AcademicYear == academicYear)
            .ToListAsync();

    public async Task SaveFeeStructureAsync(FeeStructure structure)
    {
        var existing = await _db.FeeStructures.FirstOrDefaultAsync(f =>
            f.ClassId == structure.ClassId && f.FeeHeadId == structure.FeeHeadId
            && f.AcademicYear == structure.AcademicYear && f.SectionId == structure.SectionId);

        if (existing != null)
        {
            existing.Amount = structure.Amount;
            _db.FeeStructures.Update(existing);
        }
        else
        {
            _db.FeeStructures.Add(structure);
        }
        await _db.SaveChangesAsync();
    }

    // Fee Collection
    public async Task<decimal> GetPreviousDueAsync(int studentId)
    {
        var totalPaid = await _db.FeePayments
            .Where(p => p.StudentId == studentId && p.Status == "Active")
            .SumAsync(p => p.NetAmount);

        // This is a simplified due calculation - in real scenario you'd calculate expected vs paid
        return 0; // TODO: implement proper due calculation based on fee structure
    }

    public async Task<List<FeePayment>> GetPaymentHistoryAsync(int studentId) =>
        await _db.FeePayments
            .Include(p => p.Details).ThenInclude(d => d.FeeHead)
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

    public async Task<FeePayment?> GetPaymentByIdAsync(int id) =>
        await _db.FeePayments
            .Include(p => p.Details).ThenInclude(d => d.FeeHead)
            .Include(p => p.Student).ThenInclude(s => s.Class)
            .Include(p => p.Student).ThenInclude(s => s.Section)
            .Include(p => p.School)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<FeePayment> CollectFeeAsync(FeePayment payment, List<FeePaymentDetail> details, int userId, string username)
    {
        payment.ReceiptNumber = await _schoolService.GetNextReceiptNumberAsync(payment.SchoolId);
        payment.CollectedByUserId = userId;
        payment.CollectedBy = username;
        payment.CreatedAt = DateTime.Now;
        payment.SubTotal = details.Sum(d => d.Amount);
        payment.NetAmount = payment.SubTotal + payment.PreviousDue + payment.LateFine - payment.Discount;

        _db.FeePayments.Add(payment);
        await _db.SaveChangesAsync();

        foreach (var d in details)
        {
            d.FeePaymentId = payment.Id;
            var head = await _db.FeeHeads.FindAsync(d.FeeHeadId);
            d.FeeHeadName = head?.Name ?? d.FeeHeadName;
        }
        _db.FeePaymentDetails.AddRange(details);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, username, "CollectFee", "FeePayment", payment.Id,
            $"Collected ₹{payment.NetAmount} from student {payment.StudentId}, Receipt: {payment.ReceiptNumber}");

        return payment;
    }

    public async Task<bool> CancelReceiptAsync(int paymentId, string reason, int userId, string username)
    {
        var payment = await _db.FeePayments.FindAsync(paymentId);
        if (payment == null) return false;

        payment.Status = "Cancelled";
        payment.CancelReason = reason;
        payment.CancelledAt = DateTime.Now;
        payment.CancelledBy = username;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, username, "CancelReceipt", "FeePayment", paymentId,
            $"Cancelled receipt {payment.ReceiptNumber}: {reason}");

        return true;
    }

    // Due List
    public async Task<List<Student>> GetStudentsWithDueAsync(int schoolId, int? classId, string? month)
    {
        // Students who have no payment for the given month or have outstanding dues
        var q = _db.Students.Include(s => s.Class).Include(s => s.Section)
            .Where(s => s.SchoolId == schoolId && s.Status == "Active");

        if (classId.HasValue) q = q.Where(s => s.ClassId == classId);

        var students = await q.ToListAsync();

        if (!string.IsNullOrEmpty(month))
        {
            var paidIds = await _db.FeePayments
                .Where(p => p.SchoolId == schoolId && p.Month == month && p.Status == "Active")
                .Select(p => p.StudentId).Distinct().ToListAsync();

            students = students.Where(s => !paidIds.Contains(s.Id)).ToList();
        }

        return students;
    }

    // Dashboard stats
    public async Task<decimal> GetTodayCollectionAsync(int schoolId)
    {
        var today = DateTime.Today;
        return await _db.FeePayments
            .Where(p => p.SchoolId == schoolId && p.Status == "Active" && p.PaymentDate.Date == today)
            .SumAsync(p => p.NetAmount);
    }

    public async Task<decimal> GetMonthlyCollectionAsync(int schoolId)
    {
        var firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var lastDay = firstDay.AddMonths(1);
        return await _db.FeePayments
            .Where(p => p.SchoolId == schoolId && p.Status == "Active"
                && p.PaymentDate >= firstDay && p.PaymentDate < lastDay)
            .SumAsync(p => p.NetAmount);
    }

    public async Task<List<FeePayment>> GetRecentPaymentsAsync(int schoolId, int count = 10) =>
        await _db.FeePayments
            .Include(p => p.Student)
            .Where(p => p.SchoolId == schoolId && p.Status == "Active")
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<(string Month, decimal Amount)>> GetMonthlyChartDataAsync(int schoolId)
    {
        var result = new List<(string, decimal)>();
        for (int i = 5; i >= 0; i--)
        {
            var dt = DateTime.Today.AddMonths(-i);
            var first = new DateTime(dt.Year, dt.Month, 1);
            var last = first.AddMonths(1);
            var amount = await _db.FeePayments
                .Where(p => p.SchoolId == schoolId && p.Status == "Active"
                    && p.PaymentDate >= first && p.PaymentDate < last)
                .SumAsync(p => p.NetAmount);
            result.Add((dt.ToString("MMM yy"), amount));
        }
        return result;
    }

    // Reports
    public async Task<List<FeePayment>> GetDailyReportAsync(int schoolId, DateTime date) =>
        await _db.FeePayments
            .Include(p => p.Student)
            .Include(p => p.Details).ThenInclude(d => d.FeeHead)
            .Where(p => p.SchoolId == schoolId && p.Status == "Active" && p.PaymentDate.Date == date.Date)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<FeePayment>> GetMonthlyReportAsync(int schoolId, int year, int month) =>
        await _db.FeePayments
            .Include(p => p.Student).ThenInclude(s => s.Class)
            .Include(p => p.Details).ThenInclude(d => d.FeeHead)
            .Where(p => p.SchoolId == schoolId && p.Status == "Active"
                && p.PaymentDate.Year == year && p.PaymentDate.Month == month)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();
}
