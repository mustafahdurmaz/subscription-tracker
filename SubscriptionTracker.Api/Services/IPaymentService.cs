// PaymentService kontrat arayüzü.
// CreateAsync 3 farklı sonuç döner: başarı, abonelik yok, dönem zaten ödenmiş.
// Bunu açıklamak için Outcome enum'lı bir record kullanıyoruz (exception fırlatmıyoruz).
using SubscriptionTracker.Api.Models.Dtos;

namespace SubscriptionTracker.Api.Services;

public enum PaymentCreateOutcome
{
    Success,
    SubscriptionNotFound,
    FuturePeriodNotAllowed,
    PeriodAlreadyPaid
}

public record PaymentCreateResult(PaymentCreateOutcome Outcome, PaymentResponseDto? Payment);

public interface IPaymentService
{
    Task<PaymentCreateResult> CreateAsync(PaymentCreateDto dto);
    Task<List<PaymentResponseDto>> GetAllAsync(Guid? subscriptionId, Guid? customerId);
    Task<PaymentResponseDto?> GetByIdAsync(Guid id);
}
