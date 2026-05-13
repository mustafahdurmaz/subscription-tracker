// AppDbContext: EF Core'un veritabanı oturumu — 3 DbSet (Customer/Subscription/Payment).
// OnModelCreating'de cascade delete davranışları ve enum->int dönüşümleri tanımlanır.
// Migration kullanılmaz; tablolar Program.cs'te EnsureCreated() ile yaratılır.
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Api.Models.Entities;

namespace SubscriptionTracker.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer -> Subscriptions (1-N), customer silinince abonelikleri de silinsin
        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Customer)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Subscription -> Payments (1-N), abonelik silinince ödemeler de silinsin
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enum -> int (PostgreSQL'de int sütun olarak tutulur; varsayılan davranış zaten bu,
        // açıkça yazıyoruz ki ileride okuyan "bu kasten int" görsün)
        modelBuilder.Entity<Subscription>()
            .Property(s => s.ServiceType)
            .HasConversion<int>();

        modelBuilder.Entity<Subscription>()
            .Property(s => s.Status)
            .HasConversion<int>();

        modelBuilder.Entity<Payment>()
            .Property(p => p.Status)
            .HasConversion<int>();

        // PostgreSQL: tüm DateTime kolonlarını "timestamp with time zone" yap.
        // Aksi halde DateTime.UtcNow (Kind=Utc) yazarken Npgsql runtime hatası verir.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamp with time zone");
                }
            }
        }
    }
}
