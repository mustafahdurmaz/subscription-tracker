// Program.cs: uygulamanın giriş noktası. Tüm DI register'ları,
// HTTP pipeline yapılandırması ve DB oluşturma burada.
// Servis kayıtları (mock servisler, AppDbContext, Controllers, Swagger).
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Api.Data;
using SubscriptionTracker.Api.Middleware;
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
}

app.Run();
