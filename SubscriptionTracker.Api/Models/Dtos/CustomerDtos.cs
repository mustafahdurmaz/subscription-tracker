// Customer için request/response DTO'ları.
// CLAUDE.md endpoint listesinde Customer için PUT yok — sadece Create/Response.
// Update istenirse buraya CustomerUpdateDto eklenir.
using System.ComponentModel.DataAnnotations;

namespace SubscriptionTracker.Api.Models.Dtos;

public class CustomerCreateDto
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}

public class CustomerResponseDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Müşteri özet ekranı için aggregate DTO.
// CustomerService.GetSummaryAsync üretir, /api/customers/{id}/summary döner.
public class CustomerSummaryDto
{
    public Guid CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public int ActiveSubscriptionCount { get; set; }
    public List<SubscriptionResponseDto> ActiveSubscriptions { get; set; } = new();

    // Bu ay (UTC yıl+ay) için Status=Success ödemesi olmayan AKTİF abonelikler.
    public List<SubscriptionResponseDto> UnpaidThisMonth { get; set; } = new();

    // Son 10 ödeme — PaymentDate desc.
    public List<PaymentResponseDto> RecentPayments { get; set; } = new();
}
