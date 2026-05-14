// SubscriptionService kontrat arayüzü.
// CreateAsync Result pattern döner — customer yok veya duplicate abonelik durumlarını ayırt eder.
// UpdateAsync null dönerse: subscription bulunamadı.
// GetDebtInquiryAsync null dönerse: subscription bulunamadı.
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Services.External;

namespace SubscriptionTracker.Api.Services;

// Çok-sonuçlu CreateAsync için Result pattern.
public enum SubscriptionCreateOutcome { Success, CustomerNotFound, DuplicateSubscriptionNumber }
public record SubscriptionCreateResult(SubscriptionCreateOutcome Outcome, SubscriptionResponseDto? Subscription);

public interface ISubscriptionService
{
    Task<SubscriptionCreateResult> CreateAsync(SubscriptionCreateDto dto);
    Task<List<SubscriptionResponseDto>> GetAllAsync(Guid? customerId);
    Task<SubscriptionResponseDto?> GetByIdAsync(Guid id);
    Task<SubscriptionResponseDto?> UpdateAsync(Guid id, SubscriptionUpdateDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<DebtInfo?> GetDebtInquiryAsync(Guid subscriptionId);
}
