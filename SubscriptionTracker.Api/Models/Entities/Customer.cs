// Customer entity'si: sistemdeki müşteri (abone) kaydı.
// Bir müşterinin birden çok aboneliği olabilir (1-N navigation).
// Validasyon Data Annotations ile yapılır; ekstra FluentValidation yok.
using System.ComponentModel.DataAnnotations;

namespace SubscriptionTracker.Api.Models.Entities;

public class Customer
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation: müşterinin tüm abonelikleri (cascade delete DbContext'te ayarlanır)
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
