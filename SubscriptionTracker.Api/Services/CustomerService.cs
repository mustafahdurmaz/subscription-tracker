// Customer iş kuralları: yaratma, listeleme, tek getirme, silme, özet.
// DbContext doğrudan kullanılır (repository pattern yok).
// Mapping Entity ↔ DTO manuel — AutoMapper yok.
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Api.Data;
using SubscriptionTracker.Api.Models.Dtos;
using SubscriptionTracker.Api.Models.Entities;

namespace SubscriptionTracker.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CustomerResponseDto> CreateAsync(CustomerCreateDto dto)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return ToDto(customer);
    }

    public async Task<List<CustomerResponseDto>> GetAllAsync()
    {
        var customers = await _db.Customers
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerResponseDto?> GetByIdAsync(Guid id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return null;
        return ToDto(customer);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return false;

        // Cascade delete OnModelCreating'te ayarlandı:
        // customer silinince Subscription -> Payment de silinir.
        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<CustomerSummaryDto?> GetSummaryAsync(Guid id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer is null) return null;

        // 1) Bu müşterinin tüm abonelikleri
        var allSubscriptions = await _db.Subscriptions
            .Where(s => s.CustomerId == id)
            .ToListAsync();

        // 2) Aktif olanlar
        var activeSubscriptions = allSubscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .ToList();

        // 3) Bu ay (UTC yıl+ay) için Success ödemesi olmayan aktif abonelikler.
        //    Önce bu müşterinin bu aydaki Success ödemelerinin SubscriptionId'lerini al;
        //    sonra aktif abonelikler arasında o set'te olmayanları seç.
        var now = DateTime.UtcNow;
        var paidSubIdsThisMonth = await _db.Payments
            .Where(p => p.Subscription.CustomerId == id
                     && p.PeriodYear == now.Year
                     && p.PeriodMonth == now.Month
                     && p.Status == PaymentStatus.Success)
            .Select(p => p.SubscriptionId)
            .ToListAsync();

        var unpaidThisMonth = activeSubscriptions
            .Where(s => !paidSubIdsThisMonth.Contains(s.Id))
            .ToList();

        // 4) Son 10 ödeme — bu müşterinin tüm aboneliklerinden, PaymentDate desc.
        var recentPayments = await _db.Payments
            .Where(p => p.Subscription.CustomerId == id)
            .OrderByDescending(p => p.PaymentDate)
            .Take(10)
            .ToListAsync();

        return new CustomerSummaryDto
        {
            CustomerId = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            ActiveSubscriptionCount = activeSubscriptions.Count,
            ActiveSubscriptions = activeSubscriptions.Select(SubscriptionToDto).ToList(),
            UnpaidThisMonth = unpaidThisMonth.Select(SubscriptionToDto).ToList(),
            RecentPayments = recentPayments.Select(PaymentToDto).ToList()
        };
    }

    // Entity -> DTO manuel mapping
    private static CustomerResponseDto ToDto(Customer c) => new()
    {
        Id = c.Id,
        FullName = c.FullName,
        Email = c.Email,
        PhoneNumber = c.PhoneNumber,
        CreatedAt = c.CreatedAt
    };

    // Summary içinde kullanılan yardımcı mapper'lar — diğer servislerle aynı şekil.
    private static SubscriptionResponseDto SubscriptionToDto(Subscription s) => new()
    {
        Id = s.Id,
        CustomerId = s.CustomerId,
        ServiceType = s.ServiceType,
        ProviderName = s.ProviderName,
        SubscriptionNumber = s.SubscriptionNumber,
        Status = s.Status,
        CreatedAt = s.CreatedAt
    };

    private static PaymentResponseDto PaymentToDto(Payment p) => new()
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
