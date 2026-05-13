// SubscriptionService kontrat arayüzü.
// CreateAsync null dönerse: customer bulunamadı.
// UpdateAsync null dönerse: subscription bulunamadı.
// GetDebtInquiryAsync null dönerse: subscription bulunamadı.
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Services.External;

namespace SubscriptionTracker.Api.Services;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto?> CreateAsync(SubscriptionCreateDto dto);
    Task<List<SubscriptionResponseDto>> GetAllAsync(Guid? customerId);
    Task<SubscriptionResponseDto?> GetByIdAsync(Guid id);
    Task<SubscriptionResponseDto?> UpdateAsync(Guid id, SubscriptionUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<DebtInfo?> GetDebtInquiryAsync(Guid subscriptionId);
}
