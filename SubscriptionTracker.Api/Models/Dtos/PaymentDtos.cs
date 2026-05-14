// Payment için Create/Response DTO'ları.
// CLAUDE.md: POST body = { subscriptionId, periodYear, periodMonth }.
// Tutar ve durum mock servislerden gelir (Aşama 3). Aşama 2'de placeholder.
using System.ComponentModel.DataAnnotations;
using SubscriptionTracker.Api.Models.Entities;

namespace SubscriptionTracker.Api.Models.Dtos;

public class PaymentCreateDto
{
    [Required]
    public Guid SubscriptionId { get; set; }

    [Range(2000, 2100)]
    public int PeriodYear { get; set; }

    [Range(1, 12)]
    public int PeriodMonth { get; set; }
}

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string CustomerName { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public string SubscriptionNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
