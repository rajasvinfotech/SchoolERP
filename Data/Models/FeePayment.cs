using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Data.Models;

public class FeePayment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int SchoolId { get; set; }
    public School School { get; set; } = null!;

    [Required, MaxLength(30)]
    public string ReceiptNumber { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [MaxLength(20)]
    public string Month { get; set; } = string.Empty; // e.g., "Jan 2025"

    [MaxLength(20)]
    public string AcademicYear { get; set; } = string.Empty;

    public decimal SubTotal { get; set; } = 0;
    public decimal PreviousDue { get; set; } = 0;
    public decimal Discount { get; set; } = 0;

    [MaxLength(200)]
    public string? DiscountReason { get; set; }

    public decimal LateFine { get; set; } = 0;
    public decimal NetAmount { get; set; } = 0;

    [MaxLength(20)]
    public string PaymentMode { get; set; } = "Cash"; // Cash, UPI, Cheque, DD

    [MaxLength(100)]
    public string? PaymentReference { get; set; } // UPI ref, cheque no, etc.

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    [MaxLength(100)]
    public string? CollectedBy { get; set; }

    public int CollectedByUserId { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "Active"; // Active, Cancelled

    [MaxLength(500)]
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    [MaxLength(100)]
    public string? CancelledBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<FeePaymentDetail> Details { get; set; } = new List<FeePaymentDetail>();
}

public class FeePaymentDetail
{
    public int Id { get; set; }
    public int FeePaymentId { get; set; }
    public FeePayment FeePayment { get; set; } = null!;
    public int FeeHeadId { get; set; }
    public FeeHead FeeHead { get; set; } = null!;

    [MaxLength(100)]
    public string FeeHeadName { get; set; } = string.Empty; // snapshot at time of payment

    public decimal Amount { get; set; } = 0;
}
