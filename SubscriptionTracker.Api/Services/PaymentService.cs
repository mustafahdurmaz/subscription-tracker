// Payment iş kuralları — Aşama 3: mock external servisler bağlandı.
// Akış: Subscription bul -> period check -> DebtInquiry (Amount) ->
// Gateway (IsSuccess) -> Payment kaydet (Success/Failed) -> DTO döndür.
// Gateway başarısız olsa bile Payment audit kaydı için yazılır.
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Api.Data;
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Models.Entities;
using SubscriptionTracker.Api.Services.External;

namespace SubscriptionTracker.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IDebtInquiryService _debtInquiry;
    private readonly IPaymentGatewayService _gateway;

    public PaymentService(
        AppDbContext db,
        IDebtInquiryService debtInquiry,
        IPaymentGatewayService gateway)
    {
        _db = db;
        _debtInquiry = debtInquiry;
        _gateway = gateway;
    }

    public async Task<PaymentCreateResult> CreateAsync(PaymentCreateDto dto)
    {
        // 1) Abonelik var mı?
        var subscription = await _db.Subscriptions.FindAsync(dto.SubscriptionId);
        if (subscription is null)
        {
            return new PaymentCreateResult(PaymentCreateOutcome.SubscriptionNotFound, null);
        }

        // 2) Gelecek dönem kontrolü — henüz gelmemiş ay için ödeme yapılamaz.
        var now = DateTime.UtcNow;
        if (dto.PeriodYear > now.Year || (dto.PeriodYear == now.Year && dto.PeriodMonth > now.Month))
            return new PaymentCreateResult(PaymentCreateOutcome.FuturePeriodNotAllowed, null);

        // 3) Aynı dönem için Success bir ödeme zaten var mı?
        //    (CLAUDE.md kritik iş kuralı — DB index yok, burada zorluyoruz.
        //     Failed kayıtlar yeniden denemeyi engellemez, sadece Success bloklar.)
        var alreadyPaid = await _db.Payments.AnyAsync(p =>
            p.SubscriptionId == dto.SubscriptionId &&
            p.PeriodYear == dto.PeriodYear &&
            p.PeriodMonth == dto.PeriodMonth &&
            p.Status == PaymentStatus.Success);

        if (alreadyPaid)
        {
            return new PaymentCreateResult(PaymentCreateOutcome.PeriodAlreadyPaid, null);
        }

        // 3) Borç sorgula — tutarı external servisten al.
        var debt = await _debtInquiry.GetDebtAsync(subscription.SubscriptionNumber, subscription.ServiceType);

        // 4) Gateway'e gönder.
        var gatewayResult = await _gateway.ProcessPaymentAsync(debt.Amount, subscription.SubscriptionNumber);

        // 5) Sonucu yaz — başarılı da olsa başarısız da olsa Payment kaydedilir
        //    (audit trail). Status gateway'den, Amount borç sorgusundan gelir.
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = dto.SubscriptionId,
            Amount = debt.Amount,
            PaymentDate = DateTime.UtcNow,
            PeriodYear = dto.PeriodYear,
            PeriodMonth = dto.PeriodMonth,
            Status = gatewayResult.IsSuccess ? PaymentStatus.Success : PaymentStatus.Failed,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return new PaymentCreateResult(PaymentCreateOutcome.Success, ToDto(payment));
    }

    public async Task<List<PaymentResponseDto>> GetAllAsync(Guid? subscriptionId, Guid? customerId)
    {
        var query = _db.Payments.AsQueryable();

        if (subscriptionId.HasValue)
        {
            query = query.Where(p => p.SubscriptionId == subscriptionId.Value);
        }
        else if (customerId.HasValue)
        {
            query = query.Where(p => p.Subscription.CustomerId == customerId.Value);
        }

        var list = await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(Guid id)
    {
        var payment = await _db.Payments.FindAsync(id);
        if (payment is null) return null;
        return ToDto(payment);
    }

    private static PaymentResponseDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        SubscriptionId = p.SubscriptionId,
        Amount = p.Amount,
        PaymentDate = p.PaymentDate,
        PeriodYear = p.PeriodYear,
        PeriodMonth = p.PeriodMonth,
        Status = p.Status,
        CreatedAt = p.CreatedAt
    };
}
