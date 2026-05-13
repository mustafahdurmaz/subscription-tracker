// Customer iş kuralları: yaratma, listeleme, tek getirme, silme.
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

    // Entity -> DTO manuel mapping
    private static CustomerResponseDto ToDto(Customer c) => new()
    {
        Id = c.Id,
        FullName = c.FullName,
        Email = c.Email,
        PhoneNumber = c.PhoneNumber,
        CreatedAt = c.CreatedAt
    };
}
