// Payment iş kuralları.
// AŞAMA 2 NOTU: mock external servisler henüz yok. POST geldiğinde
// Amount = 100 TL placeholder, Status = Success yazılıyor.
// Aşama 3'te DebtInquiryService Amount'u, PaymentGatewayService Status'u verecek.
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Api.Data;
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Models.Entities;

namespace SubscriptionTracker.Api.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;

    public PaymentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentCreateResult> CreateAsync(PaymentCreateDto dto)
    {
        // 1) Abonelik var mı?
        var subscription = await _db.Subscriptions.FindAsync(dto.SubscriptionId);
        if (subscription is null)
        {
            return new PaymentCreateResult(PaymentCreateOutcome.SubscriptionNotFound, null);
        }

        // 2) Aynı dönem için Success bir ödeme zaten var mı?
        //    (CLAUDE.md kritik iş kuralı — DB index yok, burada zorluyoruz.)
        var alreadyPaid = await _db.Payments.AnyAsync(p =>
            p.SubscriptionId == dto.SubscriptionId &&
            p.PeriodYear == dto.PeriodYear &&
            p.PeriodMonth == dto.PeriodMonth &&
            p.Status == PaymentStatus.Success);

        if (alreadyPaid)
        {
            return new PaymentCreateResult(PaymentCreateOutcome.PeriodAlreadyPaid, null);
        }

        // 3) Ödemeyi kaydet — Aşama 2'de placeholder değerler.
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = dto.SubscriptionId,
            Amount = 100m, // TODO Aşama 3: DebtInquiryService'ten gelecek
            PaymentDate = DateTime.UtcNow,
            PeriodYear = dto.PeriodYear,
            PeriodMonth = dto.PeriodMonth,
            Status = PaymentStatus.Success, // TODO Aşama 3: PaymentGatewayService'ten gelecek
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return new PaymentCreateResult(PaymentCreateOutcome.Success, ToDto(payment));
    }

    public async Task<List<PaymentResponseDto>> GetAllAsync(Guid? subscriptionId, Guid? customerId)
    {
        // Filtre: subscriptionId verilirse direkt, customerId verilirse
        // o müşterinin tüm aboneliklerinin ödemeleri.
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
