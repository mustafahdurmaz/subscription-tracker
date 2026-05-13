// Payment entity'si: bir abonelik için belirli bir döneme ait ödeme kaydı.
// Tutar decimal(18,2) — para tipi asla double/float olmaz.
// "Aynı (SubscriptionId + PeriodYear + PeriodMonth) için tek Success" kuralı
// service katmanında zorlanır; DB'de unique index yoktur.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubscriptionTracker.Api.Models.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid SubscriptionId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public int PeriodYear { get; set; }

    [Range(1, 12)]
    public int PeriodMonth { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Subscription Subscription { get; set; } = null!;
}
