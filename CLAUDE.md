# Subscription Tracker — Claude Code Rehberi

Bu dosya Claude Code'un projemde nasıl davranacağını belirler.
**Her yeni oturumda bu dosyayı bütünüyle oku, sonra istenen göreve geç.**

---

## 1. Proje Bağlamı

Bu proje bir bankacılık case study'sidir: **Abonelik & Otomatik Ödeme Hatırlatma Uygulaması**.
Müşteriler aboneliklerini (elektrik, su, internet, GSM vb.) sisteme tanımlar, borç sorgular, ödeme yapar.

**Kullanıcı hakkında:**
- C# / .NET tecrübesi YOK. Sıfırdan öğreniyor.
- Yarın sabah 9-12 arası sunum yapacak.
- Basitlik > best practice. Açıklanabilirlik > özellik çeşitliliği.

---

## 2. Davranış Kuralları

### 2.1 Hız modu
- Plan ekranı kısa olsun (3-5 madde), kullanıcı onayını al, yap.
- Hata gelirse durup tartışma; hatayı göster, önerini söyle, uygula.
- Her aşama sonunda DUR ve "Aşama X bitti, devam edeyim mi?" diye sor.

### 2.2 Adım büyüklüğü
- Bir mesajda en fazla 8-10 dosya oluştur.
- Her aşama sonunda `dotnet build` çalıştır. Başarısızsa devam etme, düzelt.

### 2.3 Açıklama disiplini
- HER dosya yaratıldığında 2-3 cümlelik kısa Türkçe özet ekle (ne işe yarıyor).
- Detaylı satır satır açıklama YAPMA — kullanıcı sonradan kendi okuyacak.

### 2.4 Açıklanabilirlik > best practice
- AutoMapper, MediatR, FluentValidation, CQRS KULLANMA.
- Manuel mapping yap (DTO ↔ Entity).
- Data Annotations ile validate et.
- Repository pattern KULLANMA — DbContext doğrudan service'lerde kullanılsın.
- Tek satırlık sihirli LINQ yerine 3 satırlık açık LINQ.

---

## 3. Teknik Kararlar (DEĞİŞMEZ)

- **Framework:** .NET 10 (zaten kurulu)
- **Database:** PostgreSQL 17 (zaten kurulu, çalışıyor)
- **ORM:** Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **DB oluşturma:** `EnsureCreated()` — Migrations YOK
- **API tipi:** REST, Controllers (Minimal API DEĞİL)
- **Swagger:** Built-in (Swashbuckle)
- **Mimari:** Tek proje, klasör tabanlı katman
- **Frontend:** Tek `index.html` + vanilla JS + Tailwind CDN. React YOK.
- **Para tipi:** `decimal` — ASLA double/float
- **Tarihler:** `DateTime` UTC
- **JSON:** System.Text.Json (default)
- **CORS:** Gerekmiyor (aynı origin)

---

## 4. Veritabanı Bağlantısı

`appsettings.json` ve `appsettings.Development.json` içine:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=subscription_tracker;Username=app_user;Password=app_pass_123"
  }
}
```

Program.cs içinde:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}
```

---

## 5. Domain Modeli

```
Customer (1) ──< (N) Subscription (1) ──< (N) Payment
```

### Customer
- `Id` (Guid), `FullName` (string, required, max 200), `Email` (string, required, max 200)
- `PhoneNumber` (string, max 20), `CreatedAt` (DateTime UTC)
- Navigation: `ICollection<Subscription> Subscriptions`

### Subscription
- `Id` (Guid), `CustomerId` (Guid, FK)
- `ServiceType` (enum: Electricity, Water, Internet, Gsm, NaturalGas, Other)
- `ProviderName` (string, required, max 200), `SubscriptionNumber` (string, required, max 100)
- `Status` (enum: Active, Inactive), `CreatedAt` (DateTime UTC)
- Navigation: `Customer Customer`, `ICollection<Payment> Payments`

### Payment
- `Id` (Guid), `SubscriptionId` (Guid, FK)
- `Amount` (decimal(18,2)), `PaymentDate` (DateTime UTC)
- `PeriodYear` (int), `PeriodMonth` (int, 1-12)
- `Status` (enum: Success, Failed), `CreatedAt` (DateTime UTC)
- Navigation: `Subscription Subscription`

**KRİTİK İŞ KURALI:** Aynı `SubscriptionId` + `PeriodYear` + `PeriodMonth` için
`Status = Success` olan SADECE BİR Payment olabilir. Service'te check ile zorla.

---

## 6. API Endpoint'leri

### Customers
- `POST /api/customers`
- `GET /api/customers`
- `GET /api/customers/{id}`
- `DELETE /api/customers/{id}` (cascade)

### Subscriptions
- `POST /api/subscriptions`
- `GET /api/subscriptions` (filtre: `?customerId=`)
- `GET /api/subscriptions/{id}`
- `PUT /api/subscriptions/{id}`
- `DELETE /api/subscriptions/{id}`

### Payments
- `POST /api/payments` — body: `{ subscriptionId, periodYear, periodMonth }`
- `GET /api/payments` (filtre: `?subscriptionId=` veya `?customerId=`)
- `GET /api/payments/{id}`

### Özel
- `GET /api/subscriptions/{id}/debt-inquiry` — Mock borç sorgulama
- `GET /api/customers/{id}/summary` — Aktif abonelikler + bu ay ödenmemişler + ödeme geçmişi

---

## 7. Mock 3rd Party Servisler (en az 2)

### IDebtInquiryService
```csharp
public interface IDebtInquiryService
{
    Task<DebtInfo> GetDebtAsync(string subscriptionNumber, ServiceType serviceType);
}
public record DebtInfo(decimal Amount, DateTime DueDate, int PeriodYear, int PeriodMonth);
```
- Mock: rastgele tutar (50-500 TL), son ödeme tarihi (gelecek 30 gün), dönem = şu anki ay.
- 200-500 ms Task.Delay.

### IPaymentGatewayService
```csharp
public interface IPaymentGatewayService
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(decimal amount, string subscriptionNumber);
}
public record PaymentGatewayResult(bool IsSuccess, string? TransactionId, string? ErrorMessage);
```
- Mock: %90 başarı, %10 başarısız. 300-800 ms Task.Delay.

Program.cs'de `AddScoped<IDebtInquiryService, MockDebtInquiryService>()` şeklinde kayıt.

---

## 8. Ödeme Akışı

```
POST /api/payments { subscriptionId, periodYear, periodMonth }
  ↓
1. Subscription'ı bul (404 değilse)
2. Aynı period için Success Payment var mı? → 409 Conflict
3. DebtInquiryService.GetDebtAsync → tutar
4. PaymentGatewayService.ProcessPaymentAsync → gateway sonucu
5. Payment kaydet (Success / Failed)
6. PaymentResponseDto döndür
```

---

## 9. Frontend (tek dosya)

`wwwroot/index.html` — hepsi tek dosyada:
- HTML + Tailwind CDN: `<script src="https://cdn.tailwindcss.com"></script>`
- Vanilla JS (fetch API)
- 3 sekme/sayfa: **Müşteriler**, **Abonelikler**, **Ödemeler**
- Her sekmede: liste + "yeni ekle" formu
- Abonelik kartında "Borç Sorgula" + "Öde" butonları
- Müşteri kartında "Özet" butonu
- Görsel: gri + mavi accent, minimal, profesyonel

Program.cs:
```csharp
app.UseDefaultFiles();
app.UseStaticFiles();
```

---

## 10. Klasör Yapısı

```
SubscriptionTracker.Api/
├── Controllers/
│   ├── CustomersController.cs
│   ├── SubscriptionsController.cs
│   └── PaymentsController.cs
├── Services/
│   ├── ICustomerService.cs + CustomerService.cs
│   ├── ISubscriptionService.cs + SubscriptionService.cs
│   ├── IPaymentService.cs + PaymentService.cs
│   └── External/
│       ├── IDebtInquiryService.cs + MockDebtInquiryService.cs
│       └── IPaymentGatewayService.cs + MockPaymentGatewayService.cs
├── Models/
│   ├── Entities/
│   │   ├── Customer.cs
│   │   ├── Subscription.cs
│   │   ├── Payment.cs
│   │   └── Enums.cs
│   └── Dtos/
│       ├── CustomerDtos.cs
│       ├── SubscriptionDtos.cs
│       └── PaymentDtos.cs
├── Data/
│   └── AppDbContext.cs
├── Middleware/
│   └── ExceptionHandlerMiddleware.cs
├── wwwroot/
│   └── index.html
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

Solution root: `C:\dev\subscription-tracker\`
Proje root: `C:\dev\subscription-tracker\SubscriptionTracker.Api\`

---

## 11. README.md Başlıkları (sunum dokümanı)

1. Proje Tanımı (1 paragraf)
2. Teknoloji Seçimleri ve Gerekçeleri
3. Kurulum & Çalıştırma (adım adım komutlar)
4. API Endpoint Listesi (tablo)
5. Veri Modeli & ER Diagram (Mermaid)
6. Ödeme Akışı Diyagramı (Mermaid sequence)
7. Mimari Kararlar
8. AI Kullanımı (zorunlu)
9. Eksikler ve İyileştirme Fikirleri

---

## 12. Yapma Listesi

- ❌ Multi-project çözüm
- ❌ MediatR, AutoMapper, FluentValidation, Serilog
- ❌ Repository / Unit of Work
- ❌ Authentication / authorization
- ❌ Docker
- ❌ Background services
- ❌ EF Migrations
- ❌ React / Vue / Angular
- ❌ Try-catch'i her yere (global ExceptionHandlerMiddleware)

---

## 13. Commit Disiplini

Her aşama sonunda commit komutunu kullanıcıya öner:
```
git add . && git commit -m "feat: customer crud"
```
Kullanıcı çalıştırsın.
