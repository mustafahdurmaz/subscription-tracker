// IPaymentGatewayService mock implementasyonu.
// %90 başarı, %10 başarısız. 300-800 ms gecikme.
// Başarılıda TransactionId üretir; başarısızda ErrorMessage döner.
namespace SubscriptionTracker.Api.Services.External;

public class MockPaymentGatewayService : IPaymentGatewayService
{
    private static readonly Random _random = Random.Shared;

    public async Task<PaymentGatewayResult> ProcessPaymentAsync(decimal amount, string subscriptionNumber)
    {
        var delay = _random.Next(300, 801);
        await Task.Delay(delay);

        // 0-99 arası sayı çek; 0-89 (90 değer) success, 90-99 (10 değer) fail.
        var roll = _random.Next(0, 100);
        if (roll < 90)
        {
            var transactionId = $"TXN-{Guid.NewGuid():N}".Substring(0, 16).ToUpper();
            return new PaymentGatewayResult(IsSuccess: true, TransactionId: transactionId, ErrorMessage: null);
        }

        return new PaymentGatewayResult(
            IsSuccess: false,
            TransactionId: null,
            ErrorMessage: "Banka tarafından reddedildi (mock).");
    }
}
