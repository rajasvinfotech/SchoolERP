using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Data.Models;

public class MessageTemplate
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Category { get; set; } = "General"; // FeeDue, FeeReceipt, Holiday, Exam, General

    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class MessageLog
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int TemplateId { get; set; }

    [MaxLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    public int TotalRecipients { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }

    [MaxLength(100)]
    public string SentBy { get; set; } = string.Empty;

    public int SentByUserId { get; set; }
    public DateTime SentAt { get; set; } = DateTime.Now;

    [MaxLength(20)]
    public string Method { get; set; } = "WhatsAppLink"; // WhatsAppLink, API
}
