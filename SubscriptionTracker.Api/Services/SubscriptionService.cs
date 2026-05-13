// Subscription iş kuralları: yaratma (customer'ı doğrula), listeleme (opsiyonel customer filtresi),
// tekil getirme, güncelleme, silme, borç sorgulama (debt inquiry).
// Yeni abonelik default olarak Active.
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Api.Data;
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Models.Entities;
using SubscriptionTracker.Api.Services.External;

namespace SubscriptionTracker.Api.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IDebtInquiryService _debtInquiry;

    public SubscriptionService(AppDbContext db, IDebtInquiryService debtInquiry)
    {
        _db = db;
        _debtInquiry = debtInquiry;
    }

    public async Task<SubscriptionResponseDto?> CreateAsync(SubscriptionCreateDto dto)
    {
        // Customer var mı? Yoksa null dön → controller 404 verir.
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == dto.CustomerId);
        if (!customerExists) return null;

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = dto.CustomerId,
            ServiceType = dto.ServiceType,
            ProviderName = dto.ProviderName,
            SubscriptionNumber = dto.SubscriptionNumber,
            Status = SubscriptionStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        return ToDto(subscription);
    }

    public async Task<List<SubscriptionResponseDto>> GetAllAsync(Guid? customerId)
    {
        // Filtre opsiyonel: customerId verilirse o müşterinin abonelikleri,
        // verilmezse hepsi.
        var query = _db.Subscriptions.AsQueryable();
        if (customerId.HasValue)
        {
            query = query.Where(s => s.CustomerId == customerId.Value);
        }

        var list = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<SubscriptionResponseDto?> GetByIdAsync(Guid id)
    {
        var subscription = await _db.Subscriptions.FindAsync(id);
        if (subscription is null) return null;
        return ToDto(subscription);
    }

    public async Task<SubscriptionResponseDto?> UpdateAsync(Guid id, SubscriptionUpdateDto dto)
    {
        var subscription = await _db.Subscriptions.FindAsync(id);
        if (subscription is null) return null;

        subscription.ServiceType = dto.ServiceType;
        subscription.ProviderName = dto.ProviderName;
        subscription.SubscriptionNumber = dto.SubscriptionNumber;
        subscription.Status = dto.Status;

        await _db.SaveChangesAsync();
        return ToDto(subscription);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var subscription = await _db.Subscriptions.FindAsync(id);
        if (subscription is null) return false;

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DebtInfo?> GetDebtInquiryAsync(Guid subscriptionId)
    {
        var subscription = await _db.Subscriptions.FindAsync(subscriptionId);
        if (subscription is null) return null;

        // Mock external servise sor — abonelik numarası + servis tipi gönderiyoruz.
        var debt = await _debtInquiry.GetDebtAsync(subscription.SubscriptionNumber, subscription.ServiceType);
        return debt;
    }

    private static SubscriptionResponseDto ToDto(Subscription s) => new()
    {
        Id = s.Id,
        CustomerId = s.CustomerId,
        ServiceType = s.ServiceType,
        ProviderName = s.ProviderName,
        SubscriptionNumber = s.SubscriptionNumber,
        Status = s.Status,
        CreatedAt = s.CreatedAt
    };
}
