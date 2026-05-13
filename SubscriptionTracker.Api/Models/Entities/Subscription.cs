// Subscription entity'si: bir müşteriye ait tek bir abonelik kaydı (elektrik, su, internet vb.).
// Customer ile N-1, Payment ile 1-N ilişki içerir.
// ServiceType ve Status int olarak kolonlanır (Enums.cs).
using System.ComponentModel.DataAnnotations;

namespace SubscriptionTracker.Api.Models.Entities;

public class Subscription
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public ServiceType ServiceType { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SubscriptionNumber { get; set; } = string.Empty;

    public SubscriptionStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Customer Customer { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
