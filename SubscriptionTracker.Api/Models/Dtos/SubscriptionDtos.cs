// Subscription için Create/Update/Response DTO'ları.
// Update'de CustomerId'yi değiştirmeye izin vermiyoruz — bir abonelik
// hep aynı müşteriye ait kalır, gerekirse silinip yeniden açılır.
using System.ComponentModel.DataAnnotations;
using SubscriptionTracker.Api.Models.Entities;

namespace SubscriptionTracker.Api.Models.Dtos;

public class SubscriptionCreateDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    public ServiceType ServiceType { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SubscriptionNumber { get; set; } = string.Empty;
}

public class SubscriptionUpdateDto
{
    [Required]
    public ServiceType ServiceType { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProviderName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SubscriptionNumber { get; set; } = string.Empty;

    [Required]
    public SubscriptionStatus Status { get; set; }
}

public class SubscriptionResponseDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public ServiceType ServiceType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string SubscriptionNumber { get; set; } = string.Empty;
    public SubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
