// CustomerService kontrat arayüzü. Controller bu interface üzerinden konuşur,
// böylece ileride implementasyon değişebilir (test mock vs).
using SubscriptionTracker.Api.Models.Dtos;

namespace SubscriptionTracker.Api.Services;

public interface ICustomerService
{
    Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto);
    Task<List<CustomerResponseDto>> GetAllAsync();
    Task<CustomerResponseDto?> GetByIdAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
    Task<CustomerSummaryDto?> GetSummaryAsync(Guid id);
}
