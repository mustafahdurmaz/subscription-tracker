// 3rd-party borç sorgulama servisi kontratı.
// Gerçek hayatta elektrik/su/internet sağlayıcısının API'sine HTTP çağrı olur.
// Burada mock implementasyon var (MockDebtInquiryService).
using SubscriptionTracker.Api.Models.Entities;

namespace SubscriptionTracker.Api.Services.External;

public interface IDebtInquiryService
{
    Task<DebtInfo> GetDebtAsync(string subscriptionNumber, ServiceType serviceType);
}

public record DebtInfo(decimal Amount, DateTime DueDate, int PeriodYear, int PeriodMonth);
