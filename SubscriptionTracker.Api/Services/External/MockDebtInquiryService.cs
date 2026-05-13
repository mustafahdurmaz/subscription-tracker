// IDebtInquiryService mock implementasyonu.
// Random tutar (50-500 TL), 200-500 ms gecikme — gerçek API hissi vermek için.
// Dönem = şu anki UTC ay/yıl. Son ödeme tarihi = bugünden 30 gün sonra.
using SubscriptionTracker.Api.Models.Entities;

namespace SubscriptionTracker.Api.Services.External;

public class MockDebtInquiryService : IDebtInquiryService
{
    // Thread-safe random — tek instance herkes için.
    private static readonly Random _random = Random.Shared;

    public async Task<DebtInfo> GetDebtAsync(string subscriptionNumber, ServiceType serviceType)
    {
        // Network gecikmesi simülasyonu
        var delay = _random.Next(200, 501);
        await Task.Delay(delay);

        // Tutar 50.00 - 500.00 TL arası, 2 ondalık
        var amount = Math.Round((decimal)(_random.NextDouble() * 450 + 50), 2);

        var now = DateTime.UtcNow;
        var dueDate = now.AddDays(30);

        return new DebtInfo(
            Amount: amount,
            DueDate: dueDate,
            PeriodYear: now.Year,
            PeriodMonth: now.Month);
    }
}
