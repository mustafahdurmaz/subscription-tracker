// CustomerService kontrat arayüzü. Controller bu interface üzerinden konuşur,
// böylece ileride implementasyon değişebilir (test mock vs).
using SubscriptionTracker.Api.Models.Dtos;

namespace SubscriptionTracker.Api.Services;

// Çok-sonuçlu CreateAsync için Result pattern (PaymentService'teki gibi).
public enum CustomerCreateOutcome { Success, EmailAlreadyExists }
public record CustomerCreateResult(CustomerCreateOutcome Outcome, CustomerResponseDto? Customer);

public interface ICustomerService
{
    Task<CustomerCreateResult> CreateAsync(CustomerCreateDto dto);
    Task<List<CustomerResponseDto>> GetAllAsync();
    Task<CustomerResponseDto?> GetByIdAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    Task<CustomerSummaryDto?> GetSummaryAsync(Guid id);
}
