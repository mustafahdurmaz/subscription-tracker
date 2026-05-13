// 3rd-party ödeme gateway servisi kontratı.
// Gerçek hayatta İyzico/Garanti POS gibi bir sağlayıcıya HTTP çağrı olur.
// Burada mock implementasyon var (MockPaymentGatewayService).
namespace SubscriptionTracker.Api.Services.External;

public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(decimal amount, string subscriptionNumber);
}

public record PaymentGatewayResult(bool IsSuccess, string? TransactionId, string? ErrorMessage);
