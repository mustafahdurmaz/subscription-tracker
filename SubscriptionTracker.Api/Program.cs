// Program.cs: uygulamanın giriş noktası. Tüm DI register'ları,
// HTTP pipeline yapılandırması ve DB oluşturma burada.
// Servis kayıtları (mock servisler, AppDbContext, Controllers, Swagger).
using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.Services.AddControllers();

// Swashbuckle Swagger (her ortamda açık — sunum için)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL bağlantısı
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// --- Pipeline ---

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
