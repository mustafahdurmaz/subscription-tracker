// Program.cs: uygulamanın giriş noktası. Tüm DI register'ları,
// HTTP pipeline yapılandırması ve DB oluşturma burada.
// Servis kayıtları (mock servisler, AppDbContext, Controllers, Swagger).
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Api.Data;
using SubscriptionTracker.Api.Middleware;
using SubscriptionTracker.Api.Models.Entities;
using SubscriptionTracker.Api.Services;
using SubscriptionTracker.Api.Services.External;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
// Enum'lar JSON'da int yerine string olarak görünsün (örn: "Success" / "Active").
// Hem response'larda hem request body'lerinde otomatik dönüşüm yapar.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Swashbuckle Swagger (her ortamda açık — sunum için)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL bağlantısı
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Domain servisleri — DI registration. Scoped: her HTTP isteği için yeni instance.
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Mock 3rd-party servisler. Production'da gerçek HTTP client implementasyonları gelir.
builder.Services.AddScoped<IDebtInquiryService, MockDebtInquiryService>();
builder.Services.AddScoped<IPaymentGatewayService, MockPaymentGatewayService>();

var app = builder.Build();

// --- Pipeline ---

// Global exception handler EN ÜSTTE — sonraki tüm middleware'lerden gelen
// beklenmedik hataları yakalar.
app.UseMiddleware<ExceptionHandlerMiddleware>();

// Swagger her ortamda açık
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Subscription Tracker API v1");
});

app.UseHttpsRedirection();

// wwwroot/index.html'i kök URL'de sunmak için
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

// DB'yi otomatik oluştur (migration yok — sunum için yeterli)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // --- Seed Data ---
    // Sadece Customers tablosu boşsa eklenir → her run'da tekrar etmez.
    // Sunumda hazır veriyle açılış: 3 müşteri, 7 abonelik, 3 ödeme.
    if (!db.Customers.Any())
    {
        var now = DateTime.UtcNow;

        // Müşteriler
        var ahmet = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Ahmet Yılmaz",
            Email = "ahmet@email.com",
            PhoneNumber = "5551234567",
            CreatedAt = now
        };
        var elif = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Elif Kaya",
            Email = "elif@email.com",
            PhoneNumber = "5559876543",
            CreatedAt = now
        };
        var mehmet = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Mehmet Demir",
            Email = "mehmet@email.com",
            PhoneNumber = "5554567890",
            CreatedAt = now
        };
        db.Customers.AddRange(ahmet, elif, mehmet);

        // Abonelikler
        var ahmetElektrik = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = ahmet.Id,
            ServiceType = ServiceType.Electricity,
            ProviderName = "Boğaziçi Elektrik",
            SubscriptionNumber = "BE-12345",
            Status = SubscriptionStatus.Active,
            CreatedAt = now
        };
        var ahmetInternet = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = ahmet.Id,
            ServiceType = ServiceType.Internet,
            ProviderName = "Türk Telekom",
            SubscriptionNumber = "TT-99887",
            Status = SubscriptionStatus.Active,
            CreatedAt = now
        };
        var ahmetSu = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = ahmet.Id,
            ServiceType = ServiceType.Water,
            ProviderName = "İSKİ",
            SubscriptionNumber = "ISK-44556",
            Status = SubscriptionStatus.Active,
            CreatedAt = now
        };
        var elifGsm = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = elif.Id,
            ServiceType = ServiceType.Gsm,
            ProviderName = "Vodafone",
            SubscriptionNumber = "VF-11223",
            Status = SubscriptionStatus.Active,
            CreatedAt = now
        };
        var elifGaz = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = elif.Id,
            ServiceType = ServiceType.NaturalGas,
            ProviderName = "İGDAŞ",
            SubscriptionNumber = "IG-77889",
            Status = SubscriptionStatus.Active,
            CreatedAt = now
        };
        var mehmetElektrik = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = mehmet.Id,
            ServiceType = ServiceType.Electricity,
            ProviderName = "Ayedaş",
            SubscriptionNumber = "AY-33445",
            Status = SubscriptionStatus.Active,
            CreatedAt = now
        };
        var mehmetInternet = new Subscription
        {
            Id = Guid.NewGuid(),
            CustomerId = mehmet.Id,
            ServiceType = ServiceType.Internet,
            ProviderName = "Superonline",
            SubscriptionNumber = "SO-66778",
            Status = SubscriptionStatus.Inactive,
            CreatedAt = now
        };
        db.Subscriptions.AddRange(
            ahmetElektrik, ahmetInternet, ahmetSu,
            elifGsm, elifGaz,
            mehmetElektrik, mehmetInternet);

        // Ödemeler — DateTime'lar Kind=Utc (timestamptz kolonlar bunu bekler).
        db.Payments.AddRange(
            new Payment
            {
                Id = Guid.NewGuid(),
                SubscriptionId = ahmetElektrik.Id,
                Amount = 245.50m,
                PaymentDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                PeriodYear = 2026,
                PeriodMonth = 1,
                Status = PaymentStatus.Success,
                CreatedAt = now
            },
            new Payment
            {
                Id = Guid.NewGuid(),
                SubscriptionId = ahmetElektrik.Id,
                Amount = 312.80m,
                PaymentDate = new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc),
                PeriodYear = 2026,
                PeriodMonth = 2,
                Status = PaymentStatus.Success,
                CreatedAt = now
            },
            new Payment
            {
                Id = Guid.NewGuid(),
                SubscriptionId = elifGsm.Id,
                Amount = 189.90m,
                PaymentDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                PeriodYear = 2026,
                PeriodMonth = 1,
                Status = PaymentStatus.Success,
                CreatedAt = now
            });

        db.SaveChanges();
    }
}

app.Run();
