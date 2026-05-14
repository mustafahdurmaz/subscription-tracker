# SUNUM REHBERİ — Subscription Tracker

> Otobüste 1 saatte oku. Sonra sun. Hiçbir şey ezberlemene gerek yok — bu döküman senin "ben bunu anlıyorum" hissini vermek için yazıldı.

---

## BÖLÜM 1: PROJE NE YAPIYOR? (Basit Anlatım)

Düşün ki Ahmet adında bir banka müşterimiz var. Ahmet hayatında her ay düzenli olarak fatura ödüyor: elektrik, su, internet, GSM... Her ay her birini ayrı ayrı hatırlamak, ayrı ayrı sitelere girmek zor.

Bizim uygulamamız Ahmet'e diyor ki: "Bana bir kez aboneliklerini tanıt, gerisini ben hallederim."

### Ahmet'in Yolculuğu

**1. Adım — Kayıt:**
Ahmet uygulamaya giriyor (bizim örnekte tarayıcıda açıyor) ve "Yeni Müşteri" formunu dolduruyor. Ad, e-posta, telefon. Sistem onu kaydediyor — artık Ahmet bizim sistemimizde tanımlı bir müşteri.

**2. Adım — Aboneliklerini Tanıtma:**
Ahmet "Yeni Abonelik" diyor. Diyor ki: "Ben Boğaziçi Elektrik'ten elektrik kullanıyorum, abonelik numaram BE-12345." Bu bilgiyi sisteme kaydediyor. Aynı şeyi internet, su, GSM için de yapıyor. Bunlar **bir kez** kaydediliyor — her ay tekrar yazmasına gerek yok.

> Burası önemli: Abonelik = kalıcı tanım. Her ay ayrı bir abonelik **değil**, aynı aboneliğe ayrı ayrı ödemeler yapılır.

**3. Adım — Borç Sorgulama:**
Ay sonu geldi. Ahmet düşünüyor: "Acaba bu ay elektrik faturam ne kadar?" Aboneliğin yanındaki "Borç Sorgula" butonuna basıyor. Sistem arka planda Boğaziçi Elektrik'in (mış gibi olan, yani sahte/mock) servisine soruyor: "BE-12345 numaralı abonenin bu ay borcu ne?" Cevap geliyor: "287,50 TL, son ödeme tarihi 15 Mayıs."

> Bu sorgu *gerçek* bir Boğaziçi Elektrik API'sine gitmiyor. Gerçek hayatta giderdi. Biz mock (taklit) bir servis yazdık — rastgele bir tutar üretiyor, gecikme bile koyuyor ki gerçekçi gözüksün. Mantığı aynı, sadece arkasındaki sistem sahte.

**4. Adım — Ödeme:**
Ahmet "Öde" butonuna basıyor. Şimdi sistem 4 şey yapıyor:
1. Aboneliğin gerçekten Ahmet'in olup olmadığını kontrol ediyor
2. Bu ay için zaten ödeme yapılmış mı diye bakıyor (yapılmışsa "zaten ödedin kardeşim" diyor)
3. Borç sorgulayıp tutarı alıyor
4. Mock bir ödeme gateway'ine (banka POS sistemine) gönderiyor

Gateway %90 ihtimalle "tamam, ödeme alındı" diyor. %10 ihtimalle "reddedildi" diyor (gerçek hayatta bankalar zaman zaman reddeder — kart limiti, network hatası vs.). Her iki durumda da biz **kayıt tutuyoruz** — başarılı ödeme için "Success", başarısız için "Failed". Çünkü banka dünyasında "denedi ama olmadı" bilgisi de değerli.

**5. Adım — Tekrar Deneme Engeli:**
Ahmet gün içinde uygulamayı tekrar açtı, "acaba ödedim miydi" diye unutmuş. Tekrar "Öde" basıyor. Sistem diyor ki: "**409 hatası — bu ay için zaten başarılı ödemen var.**" Çift ödeme yapılmıyor. İşte bu, projenin **en kritik iş kuralı**. Mülakatçı bunu kesin soracak.

> İnce detay: Eğer önceki deneme **Failed** olduysa, tekrar denemeye izin var. Sadece **Success** kayıt varsa engelliyoruz. Çünkü banka Failed verince Ahmet'in tekrar denemesi gerek.

**6. Adım — Özet Görme:**
Ahmet ay ortasında "Ben hangi faturalarımı ödedim, hangileri kaldı?" diye merak ediyor. "Özet" butonuna basıyor. Karşısına şu çıkıyor:
- Aktif aboneliklerin (3 tane)
- Bu ay henüz ödenmemiş olanlar (kırmızı listede — internet ve su)
- Son 10 ödemen (tablo halinde — tarih, tutar, durum)

Bu ekran banka mobil uygulamalarındaki "Faturalarım" sayfasının özetinin aynısı.

### Bütün Senaryo Tek Cümle

> "Müşteri aboneliklerini tek seferde tanıtır, sistem her ay borç sorgulayıp ödeme yapmasına izin verir, çift ödemeyi engeller, tüm geçmişi gösterir."

İşte mülakatta "ne yaptın?" diye sorulduğunda söyleyeceğin cümle bu.

---

## BÖLÜM 2: BİR REQUEST'İN YOLCULUĞU

Diyelim ki Ahmet tarayıcıda "Yeni Müşteri" formunu doldurdu, "Ekle" butonuna bastı. Bu noktadan itibaren NE OLUYOR?

### 1. Tarayıcı (Frontend) — index.html

Tarayıcı senin yazdığın `wwwroot/index.html` dosyasındaki JavaScript kodunu çalıştırıyor. Form bilgilerini topluyor (FullName, Email, PhoneNumber). Bunları bir JSON paketine (yani `{ "fullName": "Ahmet", "email": "..." }` formatında metin) çeviriyor. `fetch()` adında bir fonksiyon kullanarak HTTP isteği (request) gönderiyor:

> POST https://localhost:7220/api/customers
> Body: { "fullName": "Ahmet", ... }

POST = "yeni bir şey oluştur" demek. Adres `/api/customers` — yani "müşteriler endpoint'i".

### 2. ASP.NET Pipeline (Boru Hattı)

İstek sunucumuza ulaşır. ASP.NET (yani .NET'in web framework'ü) bunu bir **pipeline**'dan (boru hattından) geçiriyor. Bu boru hattının adımları sırasıyla `Program.cs`'te tanımlı:

- **ExceptionHandlerMiddleware**: "Bu istek işlenirken bir patlama olursa, ben yakalarım, kullanıcıya 500 hatası dönerim." Boru hattının ilk filtresi. (Şu an patlama yok, geçiyoruz.)
- **UseSwagger**: Eğer URL `/swagger` olsaydı buraya sapardı. Bizim URL `/api/customers`, devam ediyoruz.
- **UseHttpsRedirection**: HTTP gelmişse HTTPS'e yönlendir. Zaten HTTPS'tey iz, geçiyoruz.
- **UseStaticFiles**: Eğer URL `/index.html` veya `/style.css` olsaydı, `wwwroot` klasöründen dosyayı bulup gönderirdi. Bizim URL `/api/...`, geçiyoruz.
- **MapControllers**: İşte burası "API" çağrılarının yönlendirildiği yer. Adres `/api/customers` ve method POST → kim bununla ilgileniyor diye bakıyor.

### 3. Controller'a Ulaşıyoruz — CustomersController.cs

`Controllers/CustomersController.cs` dosyasında en üstte yazıyor:
- `[Route("api/customers")]` — "Ben /api/customers ile başlayan tüm istekleri alırım."
- `[HttpPost]` etiketli method var: `Create([FromBody] CustomerCreateDto dto)`.

POST isteği geldiği için bu method tetikleniyor. ASP.NET, gelen JSON'u otomatik olarak `CustomerCreateDto` adlı C# nesnesine çeviriyor (deserialization). Yani artık elimizde `dto.FullName = "Ahmet"`, `dto.Email = "..."` olan bir nesne var.

### 4. Validation (Doğrulama) — Otomatik

`CustomerCreateDto.cs` dosyasında alanların yanında `[Required]`, `[MaxLength(200)]`, `[EmailAddress]` etiketleri var. ASP.NET bunları görüyor ve **otomatik** doğruluyor. Email formatı yanlışsa, FullName boşsa, daha controller'a girmeden 400 BadRequest dönüyor. Sen tek satır kod yazmıyorsun — `[ApiController]` attribute'u sayesinde.

### 5. Controller Service'i Çağırıyor

Validation geçtiyse Controller şunu yapıyor:
> `var created = await _customerService.CreateAsync(dto);`

Yani "Ben işin teknik tarafını bilmem, sen halledersin" diyor `_customerService`'e. Burada `_customerService`, constructor (yapıcı method) üzerinden gelmiş bir `ICustomerService` nesnesi. Bu nesneyi kim sağlıyor? **Dependency Injection sistemi** — `Program.cs`'te `AddScoped<ICustomerService, CustomerService>()` diye kayıt yaptığımız yer.

### 6. Service İş Mantığını Yapıyor — CustomerService.cs

`CustomerService.cs` dosyasındaki `CreateAsync` method'u:
1. Yeni bir `Customer` nesnesi yaratıyor (yeni Guid ID, DateTime.UtcNow tarihi ile)
2. `_db.Customers.Add(customer)` diyor — **EF Core**'a "bunu kaydedeceğim, hazır ol" diyor
3. `await _db.SaveChangesAsync()` — Şimdi gerçekten DB'ye INSERT SQL'i gönderiliyor

### 7. EF Core SQL Üretiyor — AppDbContext

`Data/AppDbContext.cs` bu işin perde arkasındaki tercüman. `_db.Customers.Add(customer)` C# kodunu görünce, arka planda PostgreSQL'e şu komutu gönderiyor:

> INSERT INTO "Customers" ("Id", "FullName", "Email", ...) VALUES (...)

Sen bu SQL'i yazmıyorsun. EF Core (Entity Framework Core) yazıyor. Bu yüzden ORM (Object-Relational Mapper) deniyor — nesneleri tablolarla eşleştiriyor.

### 8. PostgreSQL Yazıyor

PostgreSQL veritabanı INSERT'ü çalıştırıyor, kaydı diske yazıyor, "tamam yazdım" diye geri dönüyor.

### 9. Service Cevap Hazırlıyor

`CustomerService.CreateAsync` artık ToDto adlı küçük bir helper method ile Customer nesnesini `CustomerResponseDto`'ya çeviriyor (kimlik kartından kartvizit yapmak gibi — bunu Bölüm 4'te detaylı anlatacağım). Bu DTO'yu return ediyor.

### 10. Controller HTTP Response Hazırlıyor

Controller `return CreatedAtAction(nameof(GetById), new { id = created.Id }, created)` diyor. Bu da:
- HTTP status: **201 Created**
- Header: `Location: /api/customers/{id}` (yeni kaynağın adresi)
- Body: yeni müşterinin JSON'u

### 11. JSON'a Geri Çevrim

`Program.cs`'te `AddJsonOptions` ile yapılandırılmış serializer (özellikle `JsonStringEnumConverter`), DTO nesnesini JSON metnine çeviriyor. Enum'lar otomatik olarak string oluyor — yani sayısal `0` yerine `"Active"` görünüyor.

### 12. Pipeline'dan Geri Yolculuk

Response yine pipeline'dan geriye doğru çıkıyor (her middleware "işin bittiyse ben de görmem gerek mi?" diye bakıyor). En son tarayıcıya ulaşıyor.

### 13. Frontend Cevabı İşliyor

`index.html`'deki JavaScript `await fetch(...)` ile beklediği cevabı alıyor. Status 201 olduğu için:
- Form sıfırlanıyor (`e.target.reset()`)
- Yeşil bir toast (bildirim) gösteriliyor: "Müşteri eklendi."
- `loadCustomers()` çağrılıyor → liste yenileniyor → yeni Ahmet kartı belirip duruyor

### Toplam Süre

Tüm bu yolculuk **ortalama 50-150 milisaniye**. Yani göz kırpmadan kısa.

> Bu yolculuğu kafanda canlandırabiliyorsan, projenin %70'ini anlamış oldun. Geri kalan %30'u sadece detay.

---

## BÖLÜM 3: KLASÖR VE DOSYA REHBERİ

Projenin kök klasöründe ne var? Bir tek `SubscriptionTracker.Api` klasörü, bir `SubscriptionTracker.slnx` (solution dosyası — Visual Studio'nun "bu projeleri açacaksın" listesi), bir `README.md`, bir `.gitignore`. Hepsi bu. Tek proje yapısı.

İçeriye giriyoruz: `SubscriptionTracker.Api/` altında klasörler var. Şimdi tek tek geziyoruz.

### 1. Program.cs (PROJENİN KALBİ)

**Ne yapıyor:** Uygulama açıldığında çalışan ilk dosya. Burada her şey kuruluyor: hangi servisler var, hangi pipeline adımları var, veritabanı bağlantısı, seed (örnek) veri.

**İçinde ne var:**
- En üstte `using` satırları — başka dosyalardaki sınıfları kullanabilmek için import
- `var builder = WebApplication.CreateBuilder(args)` — uygulama "inşaatçısını" yarat
- `builder.Services.AddControllers()` ile başlayan kısım: hangi servisleri DI container'ına (sipariş sistemine) kaydedeceğimiz
- `var app = builder.Build()` ile uygulama nesnesi oluşturulur
- `app.UseMiddleware<ExceptionHandlerMiddleware>()` ile başlayan kısım: pipeline (boru hattı) sırası
- En altta `using (var scope = ...)` bloğu: DB tablolarını oluştur (EnsureCreated) ve eğer boşsa seed (örnek) veriyi koy
- `app.Run()` — "her şey hazır, şimdi gelen istekleri dinle"

**Somut örnek:** Sen `dotnet run` yazdığın anda Program.cs en yukarıdan başlayıp aşağı doğru çalışır, app.Run()'a gelince orada durup HTTP istekleri beklemeye başlar.

### 2. Models/Entities/ — Veritabanı Tablolarının C# Karşılığı

Bu klasördeki dosyalar veritabanındaki **tabloları** temsil eden sınıflardır. EF Core bunlara bakıp "ben bu yapıda bir tablo yapacağım" diyor.

#### Enums.cs

**Ne yapıyor:** Sabit değer setlerini tanımlar. ServiceType (Elektrik/Su/...), SubscriptionStatus (Aktif/Pasif), PaymentStatus (Başarılı/Başarısız).

**İçinde ne var:** 3 tane `enum`. Her birinin sabit sayısal karşılığı var (Active=0, Inactive=1 gibi). Bu sayılar veritabanına yazılıyor; ileride yeni bir değer eklersen eskiler bozulmasın diye sabit tuttuk.

**Somut örnek:** Veritabanında bir Subscription kaydının `Status` kolonu `0` görürsen "bu Active" anlamına geliyor.

#### Customer.cs

**Ne yapıyor:** Müşteri tablosunun yapısı.

**İçinde ne var:** `Id` (Guid — global benzersiz ID), `FullName`, `Email`, `PhoneNumber`, `CreatedAt`. En altta `ICollection<Subscription> Subscriptions` var — bu navigation property (yön gösterici): "bir müşterinin abonelikleri var, ihtiyaç olursa otomatik gel" diyor EF'e. Alanların üstünde `[Required]`, `[MaxLength(200)]`, `[EmailAddress]` etiketleri — validation kuralları.

**Somut örnek:** Yeni müşteri eklendiğinde EF Core bu sınıfa bakıp `Customers` tablosuna karşılık gelen INSERT SQL'i üretiyor.

#### Subscription.cs

**Ne yapıyor:** Bir aboneliği temsil ediyor. Bir müşteri birden fazla aboneliğe sahip olabilir.

**İçinde ne var:** Id, CustomerId (hangi müşteriye ait — foreign key), ServiceType (Elektrik mi Su mu), ProviderName ("Boğaziçi Elektrik"), SubscriptionNumber ("BE-12345"), Status (Aktif/Pasif), CreatedAt. İki navigation: `Customer Customer` (geriye doğru) ve `ICollection<Payment> Payments` (ileri doğru).

**Somut örnek:** Ahmet'in elektrik aboneliği oluşturulduğunda CustomerId Ahmet'in ID'siyle dolduruluyor — DB'de FOREIGN KEY ilişkisi var.

#### Payment.cs

**Ne yapıyor:** Bir ödeme kaydını temsil ediyor. **Çok önemli**: aynı abonelik için her ay yeni bir Payment kaydı atılır.

**İçinde ne var:** Id, SubscriptionId, **Amount (decimal — para tipi, asla double değil)**, PaymentDate, **PeriodYear ve PeriodMonth** (hangi döneme ait olduğu — örn: 2026 Mayıs için PeriodYear=2026, PeriodMonth=5), Status (Success/Failed), CreatedAt. `[Column(TypeName = "decimal(18,2)")]` etiketi sayesinde DB'de `numeric(18,2)` tipinde saklanıyor — yani 18 hane, 2'si ondalık.

**Somut örnek:** Ahmet 2026 Mayıs için elektrik ödediğinde bir Payment satırı oluşur. 2026 Haziran için ödediğinde başka bir satır. Bunlar farklı döneme ait farklı ödemeler.

### 3. Models/Dtos/ — Dış Dünyaya Açılan Yüz

DTO = Data Transfer Object (veri taşıma nesnesi). Entity'ler veritabanı dünyasındaki şekil; DTO'lar API'mizden dışarı çıkıp giren şekil. Neden ayrılar? Çünkü dışarıya her şeyi göstermek istemeyebilirsin (parola gibi), ya da dışardan her şeyi kabul etmek istemezsin (Id gibi — Id'yi sen üretirsin, kullanıcı göndermesin).

#### CustomerDtos.cs

**Ne yapıyor:** Müşteri için 3 farklı şekil tutuyor:
- `CustomerCreateDto`: dışarıdan "yeni müşteri yarat" isteğinde gelen şekil (FullName, Email, PhoneNumber)
- `CustomerResponseDto`: dışarıya cevap olarak gönderdiğimiz şekil (Id ve CreatedAt da var)
- `CustomerSummaryDto`: özet endpoint'inin döndüğü zengin şekil (aktif abonelikler listesi, bu ay ödenmemişler, son 10 ödeme)

**Somut örnek:** Frontend POST isterken sadece FullName/Email/PhoneNumber göndermeli — Id'yi göndermemeli. CustomerCreateDto bunu garantiliyor.

#### SubscriptionDtos.cs

**Ne yapıyor:** Abonelik için 3 şekil:
- `SubscriptionCreateDto`: yeni abonelik için (CustomerId zorunlu)
- `SubscriptionUpdateDto`: güncelleme için (CustomerId YOK — bir abonelik müşterisini değiştiremez, sadece servis tipi/sağlayıcı/no/durumu)
- `SubscriptionResponseDto`: cevap olarak

#### PaymentDtos.cs

**Ne yapıyor:** İki şekil:
- `PaymentCreateDto`: sadece üç alan (SubscriptionId, PeriodYear, PeriodMonth). Tutar dışarıdan gelmez — sistem borç sorgudan alır. Status da dışarıdan gelmez — gateway belirler.
- `PaymentResponseDto`: tüm bilgilerle (tutar, tarih, dönem, durum)

### 4. Data/AppDbContext.cs — Veritabanı Tercümanı

**Ne yapıyor:** EF Core'un veritabanına nasıl bağlanacağını ve Entity'leri nasıl eşleştireceğini söylüyor. "DbContext" kelimesi "veritabanı bağlamı" demek — sanki DB ile aramızdaki açık bir telefon hattı.

**İçinde ne var:**
- `DbSet<Customer> Customers`, `DbSet<Subscription> Subscriptions`, `DbSet<Payment> Payments` — her tabloyu temsil eden koleksiyon
- `OnModelCreating` method'u — veritabanı şemasının ince ayarları:
  - Cascade delete kuralları (müşteri silinince abonelikleri silinsin, abonelik silinince ödemeleri silinsin)
  - Enum'lar int olarak saklansın
  - Tüm DateTime kolonları `timestamp with time zone` (timestamptz) tipinde olsun — bu PostgreSQL ile UTC tarih uyumu için kritik

**Somut örnek:** Eğer biri DELETE /api/customers/{id} isterse, o müşterinin tüm abonelikleri ve onların altındaki ödemeleri tek seferde silinir. Sen tek satır SQL yazmıyorsun — bu konfigürasyon sayesinde otomatik.

### 5. Services/ — İş Mantığı Bölgesi

İş kurallarının yaşadığı yer. Controller'lar buradaki servisleri çağırıyor.

#### ICustomerService.cs ve CustomerService.cs

**Ne yapıyor:** ICustomerService bir interface (sözleşme — "şu method'ları yapacaksın diyor"). CustomerService o sözleşmeyi yerine getirien sınıf.

**İçinde ne var:** 5 method:
- `CreateAsync` — yeni müşteri yarat
- `GetAllAsync` — tüm müşterileri getir (en yeni en üstte)
- `GetByIdAsync` — Id'ye göre getir (yoksa null)
- `DeleteAsync` — sil (cascade DB'de zaten ayarlı)
- `GetSummaryAsync` — müşteri özeti (aktif abonelikler + bu ay ödenmemiş + son 10 ödeme)

GetSummaryAsync'in içinde 4 ayrı sorgu var: önce müşteri, sonra aboneliklerinin hepsi, sonra bu ay yapılmış başarılı ödemeler (Subscription ID'leri), sonra son 10 ödeme. Bu listeleri DTO'lara map ederek döndürüyor.

**Somut örnek:** "Özet" butonuna basınca GetSummaryAsync çalışıyor — 4 SQL sorgusu gidiyor PostgreSQL'e, sonuçları birleştiriyor, kullanıcıya tek JSON nesnesi olarak dönüyor.

#### ISubscriptionService.cs ve SubscriptionService.cs

**Ne yapıyor:** Abonelik CRUD + borç sorgulama orchestration'ı.

**İçinde ne var:** 6 method (CRUD + GetDebtInquiryAsync). En ilginç olanı `CreateAsync`: önce müşterinin var olup olmadığını kontrol ediyor (`AnyAsync`), yoksa null dönüyor (controller bunu 404'e çeviriyor). `GetDebtInquiryAsync` aboneliği bulup `IDebtInquiryService`'i çağırıyor.

**Somut örnek:** "Borç Sorgula" butonu → SubscriptionsController.DebtInquiry → SubscriptionService.GetDebtInquiryAsync → MockDebtInquiryService.GetDebtAsync → random tutar geri.

#### IPaymentService.cs ve PaymentService.cs

**Ne yapıyor:** Bütün ödeme akışının patronu. Bu projenin EN ÖNEMLİ dosyası.

**İçinde ne var:**
- `PaymentCreateOutcome` adında bir enum: Success, SubscriptionNotFound, PeriodAlreadyPaid (3 farklı sonuç durumu)
- `PaymentCreateResult` adında bir record: hem outcome hem (varsa) PaymentResponseDto'yu içeriyor
- `CreateAsync` method'u: ödeme akışının kendisi (Bölüm 6'da satır satır anlatılacak)
- `GetAllAsync`, `GetByIdAsync` — listeleme/detay

Constructor'da 3 bağımlılık var: AppDbContext, IDebtInquiryService, IPaymentGatewayService. Bunlar DI sayesinde otomatik geliyor.

### 6. Services/External/ — Mock 3rd-Party Servisler

#### IDebtInquiryService.cs ve MockDebtInquiryService.cs

**Ne yapıyor:** Borç sorgulama servisi. Gerçek hayatta Boğaziçi Elektrik'in API'sine HTTP çağrı atılır. Burada sahte (mock) bir cevap veriyor.

**İçinde ne var:** Interface bir method tanımlıyor (`GetDebtAsync(subscriptionNumber, serviceType)`) ve `DebtInfo` adlı bir record (Amount, DueDate, PeriodYear, PeriodMonth). Mock implementation: 200-500ms gecikme (gerçek API hissi versin), 50-500 TL arası rastgele tutar, son ödeme tarihi 30 gün sonra.

**Somut örnek:** Aynı butona 3 kez basarsan 3 farklı tutar gelir — çünkü random.

#### IPaymentGatewayService.cs ve MockPaymentGatewayService.cs

**Ne yapıyor:** Ödeme gateway servisi. Gerçek hayatta İyzico veya banka POS API'si.

**İçinde ne var:** Interface (`ProcessPaymentAsync(amount, subscriptionNumber)`) ve `PaymentGatewayResult` record (IsSuccess, TransactionId, ErrorMessage). Mock: 300-800ms gecikme. 0-99 arası random sayı çekiyor; 0-89 (yani %90) → success, 90-99 (yani %10) → fail. Success durumunda fake bir TransactionId üretiyor ("TXN-A1B2C3D4..."). Fail durumunda "Banka tarafından reddedildi (mock)" hata mesajı dönüyor.

**Somut örnek:** 10 kez "Öde" basarsan ortalama 9'u Success, 1'i Failed gelir.

### 7. Controllers/ — HTTP Karşılayıcıları

Hiç iş mantığı içermez. Sadece HTTP'yi servis çağrısına çevirir.

#### CustomersController.cs

**Ne yapıyor:** /api/customers altındaki 5 endpoint'i karşılıyor (Create, GetAll, GetById, Delete, Summary).

**İçinde ne var:** Her method'un başında `[HttpPost]`, `[HttpGet]` gibi etiketler var — hangi HTTP method'una karşılık geleceğini söylüyor. `[Route("api/customers")]` controller'ın URL prefix'i. `[ApiController]` etiketi otomatik validation, otomatik 400 BadRequest sağlıyor.

**Somut örnek:** Tüm method'lar 5-7 satır. Servis çağırılıyor, null gelirse 404, yoksa 200/201/204.

#### SubscriptionsController.cs

**Ne yapıyor:** /api/subscriptions altındaki 6 endpoint (CRUD + DebtInquiry).

**İçinde ne var:** Standart CRUD + `[HttpGet("{id:guid}/debt-inquiry")]` endpoint'i. `{id:guid}` — Id'nin Guid formatında olması gerektiğini ASP.NET'e söylüyor.

#### PaymentsController.cs

**Ne yapıyor:** /api/payments altında 3 endpoint.

**İçinde ne var:** En ilginç method: `Create`. Servis bir `PaymentCreateResult` dönüyor; controller `switch expression` (anahtar ifadesi) ile bunu HTTP koduna çeviriyor:
- `Success` → 201 Created
- `SubscriptionNotFound` → 404
- `PeriodAlreadyPaid` → 409 Conflict

**Somut örnek:** Bu pattern'in adı "Result Pattern". Mülakatta sorulursa: "Çoklu sonucu enum ile gösterip controller'da switch yaptık, exception fırlatmadık."

### 8. Middleware/ExceptionHandlerMiddleware.cs

**Ne yapıyor:** Pipeline'ın en üstünde duran "global hata yakalayıcı". Kodun herhangi bir yerinde patlama olursa burada yakalanıyor.

**İçinde ne var:** `InvokeAsync` method'u içinde try-catch. Patlama olunca:
- Hatayı log'a yazıyor (ileride Application Insights / Seq vb. tutacak)
- HTTP 500 dönüyor
- Body'de `{ error, detail }` formatında JSON

**Somut örnek:** Eğer service'te elle bug çıkarıp `throw new Exception("test")` yaparsan, kullanıcı yine güzel bir JSON cevap alır, sayfa patlamaz. Bu sayede service ve controller'larda try-catch yazmamıza gerek kalmıyor.

### 9. wwwroot/index.html — Tek Dosya Frontend

**Ne yapıyor:** Tüm tarayıcı arayüzü tek bir HTML dosyasının içinde. HTML + Tailwind CSS (CDN'den çekilen stil framework'ü) + vanilla JavaScript (yani React/Vue gibi framework yok).

**İçinde ne var:**
- Üstte 3 sekme (tab): Müşteriler, Abonelikler, Ödemeler
- Her sekmenin sol tarafında bir "yeni ekle" formu, sağ tarafında liste
- Bir modal pencere (özet ve borç sorgu sonucu için)
- Bir toast (sağ üstte beliren bildirim) sistemi
- JavaScript fonksiyonları: `loadCustomers`, `loadSubscriptions`, `loadPayments`, form submit handler'ları, `inquireDebt`, `payNow`, `showSummary`

**Somut örnek:** Sayfa açıldığında `init()` fonksiyonu çalışıyor, üç listeyi de yüklüyor. Her butona basıldığında ilgili API çağrısı yapılıyor, sonuç toast olarak gösteriliyor, listeler yenileniyor.

### 10. appsettings.json

**Ne yapıyor:** Konfigürasyon değerlerini tutuyor — özellikle veritabanı connection string'i.

**İçinde ne var:** `ConnectionStrings.DefaultConnection` altında `Host=localhost;Port=5432;Database=subscription_tracker;Username=app_user;Password=app_pass_123`. Logging seviyeleri.

**Somut örnek:** Program.cs'te `builder.Configuration.GetConnectionString("DefaultConnection")` ile bu değer okunuyor. Eğer DB başka makinede olsaydı sadece burası değişirdi.

`appsettings.Development.json` da var — Development ortamında bunu daha önce okuyor (override).

---

## BÖLÜM 4: 7 TEMEL KAVRAM (ASP.NET BİLMEYENE)

### 1. Controller — Kapıdaki Resepsiyon

Bir otele girdiğinde resepsiyona geliyorsun: "Oda istiyorum." Resepsiyon seninle konuşur, ihtiyacını anlar, ama odayı kendisi hazırlamaz — temizlik ekibine söyler. **Controller** aynı işi yapar: HTTP isteğini alır, ne istendiğini anlar, **service**'e havale eder, gelen cevabı sana iletir.

Controller'da iş mantığı YOKTUR. "Şuraya 50 TL'den fazla geçmesin" gibi kontroller controller'da değil, service'te yapılır. Controller sadece tercüman.

### 2. Service — Arka Ofis Çalışanı

Müdürün makamına geldin: "Ben bu evraka onay almak istiyorum." Müdür alır, kontrol eder, mührü vurur, geri verir. Yetkili odur, kuralları o uygular. **Service** aynı şey: iş kurallarının yaşadığı yer. "Aynı dönem için ikinci başarılı ödeme yapılamaz" kuralı `PaymentService` içinde. "Müşteri yoksa abonelik açma" kuralı `SubscriptionService` içinde.

Servisler veritabanına da bakar, dış servisleri de çağırır. Yani işin orkestrasını yöneten kişi.

### 3. Entity vs DTO — Kimlik Kartı vs Kartvizit

**Entity** = TC kimlik kartın. Üzerinde her şey var: TC kimlik no, kan grubun, anne baba adı, fotoğraf. Devlete (yani veritabanına) verirsin.

**DTO** = Kartvizitin. İş yerinde dağıttığın. Üzerinde sadece ad, telefon, e-posta var. Kan grubunu yazmazsın.

`Customer` (entity) → DB tablosunun aynısı. `CustomerResponseDto` → API'den dışarıya gösterdiğin. `CustomerCreateDto` → API'ye gelen "yeni yarat" isteğindeki şekil. Hepsi ayrı sınıflar çünkü her birinin görevi farklı.

> Birçok projede AutoMapper diye bir kütüphane kullanılır — Entity ile DTO arasında otomatik dönüşüm yapsın diye. Biz kullanmadık. Servislerin altındaki `private static ToDto()` metoduyla 6-7 satır manuel mapping yazdık. Sebep: kim ne yapıyor görsünüz, kara kutu olmasın.

### 4. DbContext — Veritabanı Tercümanı

İki dil bilen bir tercüman düşün. C# dilini biliyorsun, PostgreSQL SQL dilini bilmiyorsun. Tercüman aralarında çeviri yapıyor: "Müşterileri istiyorum" deyince SQL'e çeviriyor, sana C# nesneleri olarak geri getiriyor.

`AppDbContext` bu tercüman. `_db.Customers.ToListAsync()` yazınca arkada `SELECT * FROM "Customers"` çalışıyor ve sonuç `List<Customer>` olarak geri geliyor.

DbContext **per-request** kullanılır — yani her HTTP isteği için yeni bir DbContext açılıyor, iş bittikten sonra kapatılıyor. Bu yüzden DI'da `AddScoped` ile kayıt ediyoruz ("Scope" = HTTP request demek).

### 5. Dependency Injection — Sipariş Sistemi

Bir restoranda mutfak elemanı oluşmadan önce kahve makinesini elinde getirmen istenmiyor — restoran sahibi seni alırken "sen kahve makinesi kullanacaksın, biz hazır vereceğiz" diyor.

Yazılımda **Dependency Injection (DI)** budur. `CustomerService` çalışmak için `AppDbContext`'e ihtiyaç duyar. `CustomerService` kendisi `new AppDbContext()` yazmıyor; ASP.NET ona constructor üzerinden hazır bir DbContext veriyor.

Program.cs'teki `builder.Services.AddScoped<ICustomerService, CustomerService>()` satırı şu demek: "Birisi `ICustomerService` isterse, sen `CustomerService` ver, her HTTP isteği için yeni bir tane yarat."

Faydaları: Test yazmak kolaylaşıyor (gerçek DbContext yerine sahte verebilirsin), gevşek bağlılık (loose coupling) sağlanıyor.

### 6. Middleware — Güvenlik Kapısı

Havalimanına gitmişsindir. Sırayla geçtiğin kapılar var: bilet kontrolü, X-ray, pasaport kontrolü, gate. Her kapı bir filtre. Kapı seni geçirebilir, geri çevirebilir, ya da senin üstüne bir damga vurup gönderebilir.

**Middleware** bu kapılar. Her HTTP isteği bu zinciri sırayla geçiyor:
1. ExceptionHandlerMiddleware (hata yakalayıcı)
2. UseSwagger (dokümantasyon endpoint'leri)
3. UseHttpsRedirection (HTTP→HTTPS yönlendirme)
4. UseStaticFiles (HTML/CSS/JS dosyalarını sun)
5. UseAuthorization (yetki kontrolü — bizde aktif değil)
6. MapControllers (API endpoint'lerine yönlendir)

Sıra önemli! Mesela ExceptionHandler en başta olmazsa, sonraki kapılarda patlayan hatayı yakalayamaz.

### 7. async / await — Sıra Beklemeden İş Yaptırma

Diyelim ki kahveni yapmak için 30 saniye bekliyorsun. O 30 saniyede ne yapıyorsun? Telefonuna bakıyorsun, e-mail'lere yazıyorsun. Yani **bekleme süresinde başka işler hallediyorsun**.

Programlamada veritabanı sorguları, ağ çağrıları (HTTP), dosya okuma yavaş işler. Eğer "klasik" şekilde yazarsan, program o sorgu bitene kadar **donar** (block olur). Bu süre içinde başka HTTP isteği gelirse cevap verilmez.

`async` ve `await` ile şu olur: "Ben bu sorguyu başlattım, sen başka işlere bak, sorgu bitince beni uyandır." Sonuç: aynı sunucu çok daha fazla isteği aynı anda kaldırabiliyor.

Tüm servis methodlarımız `async Task<T>` dönüyor, `await _db.Customers.ToListAsync()` gibi çağrılar yapıyor. Bu yüzden uygulamamız bir kullanıcı için ödeme işlemi yaparken (mock servisler 500-1000ms sürüyor) başka kullanıcıları bekletmiyor.

---

## BÖLÜM 5: MOCK SERVİSLER NASIL ÇALIŞIYOR?

### Interface Nedir, Neden Var?

**Interface** = sözleşme, taahhüt. Bir interface şunu söyler: "Bu sözleşmeyi imzalayan herhangi bir sınıf şu metodları yapmak ZORUNDA."

Bizim `IDebtInquiryService` interface'i şöyle diyor:
> "Beni implement eden sınıf `GetDebtAsync(subscriptionNumber, serviceType)` metodunu yapacak ve `DebtInfo` döndürecek."

Bu sözleşmeyi `MockDebtInquiryService` imzalıyor — random tutar dönerek. İleride `RealBogaziciElektrikDebtService` diye bir sınıf yazıp gerçek API'ye HTTP çağrı atan bir implementation da yazabiliriz. Sözleşme aynı, içerik farklı.

### Mock Implementation Ne Yapıyor?

**MockDebtInquiryService** — `Random.Shared` kullanarak (her çağrıda farklı sayı üreten thread-safe global random):
1. 200-500 ms arası bekliyor (`Task.Delay`) — gerçek API hissi versin
2. 50.00 - 500.00 TL arası rastgele bir tutar üretiyor
3. Son ödeme tarihi: bugünden 30 gün sonra
4. Dönem: şu anki UTC ay/yıl

Bu kadar. Gerçek implementation yerine **inanılmaz basit bir taklit**. Ama mantığın doğruluğunu test etmek için yeterli.

**MockPaymentGatewayService** — benzer şekilde:
1. 300-800 ms arası bekliyor
2. 0-99 arası rastgele sayı çekiyor
3. Sayı 90'dan küçükse (yani %90 ihtimalle): Success, fake bir TransactionId üretiyor
4. 90-99 arası ise: Failed, "Banka reddetti (mock)" hata mesajı

### Gerçek Servise Geçiş Nasıl Olur?

İşin güzel kısmı: kodun **%99'unu** değiştirmen GEREKMİYOR.

Tek yapacağın:
1. `RealDebtInquiryService` adında yeni bir sınıf yaz, `IDebtInquiryService` interface'ini implement et. İçinde `HttpClient` kullanarak Boğaziçi Elektrik'in API'sine gerçek istek at.
2. `Program.cs`'teki şu satırı değiştir:
   - Önce: `AddScoped<IDebtInquiryService, MockDebtInquiryService>()`
   - Sonra: `AddScoped<IDebtInquiryService, RealDebtInquiryService>()`

Bitti. Tek satır değişikliği.

`PaymentService` ne `Mock` kelimesini biliyor ne `Real` kelimesini. Sadece `IDebtInquiryService` ile konuşuyor. DI sistemi hangisinin verileceğine karar veriyor.

### Dependency Injection ile Bağlantı

`PaymentService.cs`'in constructor'ı şöyle:

> public PaymentService(AppDbContext db, IDebtInquiryService debtInquiry, IPaymentGatewayService gateway)

Yani PaymentService doğmadan önce 3 şey istiyor: bir DbContext, bir borç sorgu servisi, bir gateway servisi. ASP.NET DI sistemi şöyle düşünüyor: "PaymentService doğacak, ben Program.cs'te kayıtlı olanlara bakıp bunları üretip ona vereceğim."

Sen hiç `new PaymentService(...)` yazmıyorsun. Controller'ın constructor'ında `IPaymentService` istiyorsun, DI sana ihtiyacın olan her şeyi zinciri ile beraber teslim ediyor.

### Bu Niye Önemli?

Mülakatta sorulursa:
> "Mock servisler bir interface arkasında. Production'a geçişte sadece DI registration değişiyor — business kodu hiç değişmiyor. Bu test edilebilirliği ve genişletilebilirliği sağlar."

---

## BÖLÜM 6: ÖDEME AKIŞI (EN KRİTİK BÖLÜM)

POST /api/payments çağrıldığında ne oluyor? PaymentService.CreateAsync metoduna girip satır satır inceliyoruz. Bu projenin kalbinin atışını öğrenmek için.

### Adım 0: Controller'a Geliş

Frontend "Öde" butonuna basıldı. POST /api/payments'a gidiyor. Body'de `{ subscriptionId, periodYear, periodMonth }` var.

PaymentsController.Create method'u tetikleniyor. Service çağrılıyor:
> var result = await _paymentService.CreateAsync(dto);

### Adım 1: Subscription Bul

PaymentService.CreateAsync ilk olarak şunu yapıyor:
> var subscription = await _db.Subscriptions.FindAsync(dto.SubscriptionId);

`FindAsync` — Id'ye göre sorgu, en hızlı yol. Eğer abonelik yoksa `subscription` değişkeni `null` olur.

**Sonuç eğer null ise:** Service hemen geri dönüyor:
> return new PaymentCreateResult(PaymentCreateOutcome.SubscriptionNotFound, null);

Yani "Bu abonelik bulunamadı, ödeme yapılmadı."

### Adım 2: Aynı Dönem için Success Var mı? (KRİTİK İŞ KURALI)

Subscription bulundu. Şimdi check:
> var alreadyPaid = await _db.Payments.AnyAsync(p => p.SubscriptionId == dto.SubscriptionId && p.PeriodYear == dto.PeriodYear && p.PeriodMonth == dto.PeriodMonth && p.Status == PaymentStatus.Success);

Bu ne diyor: "Aynı abonelik için, aynı yıl, aynı ay, status Success olan bir Payment var mı?"

**Önemli detay:** Status==Success şartı var. Yani önceki bir denemen Failed ile bitmişse, tekrar deneyebilirsin. Sadece başarılı ödeme bloklayıcı.

`AnyAsync` — sadece "var mı yok mu" sorgusu, kayıtların kendisini getirmiyor. Performans için optimal.

**Sonuç eğer true ise:** Service hemen geri dönüyor:
> return new PaymentCreateResult(PaymentCreateOutcome.PeriodAlreadyPaid, null);

Yani "Bu dönem için zaten başarılı ödemen var."

### Adım 3: Borç Sorgula (External Servis Çağrısı #1)

Period kontrolü geçti. Şimdi mock dış servise borç soruyoruz:
> var debt = await _debtInquiry.GetDebtAsync(subscription.SubscriptionNumber, subscription.ServiceType);

Yukarıdaki çağrı 200-500 ms sürüyor (mock'taki Task.Delay). Geri dönen `DebtInfo` record'unda Amount, DueDate, PeriodYear, PeriodMonth alanları var.

**Önemli not:** Burada **no exception handling** var. Eğer bu servis patlasa (gerçekte network problemi olurdu) global ExceptionHandlerMiddleware yakalıyor, kullanıcı 500 hatası görüyor. Service bu durumda hiç try-catch yazmıyor — temiz kalıyor.

### Adım 4: Gateway'e Gönder (External Servis Çağrısı #2)

Borcu öğrendik. Şimdi mock ödeme gateway'ine gönderiyoruz:
> var gatewayResult = await _gateway.ProcessPaymentAsync(debt.Amount, subscription.SubscriptionNumber);

300-800 ms sonra `PaymentGatewayResult` dönüyor: IsSuccess (bool), TransactionId (varsa), ErrorMessage (varsa).

%90 ihtimalle IsSuccess=true. %10 ihtimalle IsSuccess=false.

### Adım 5: Payment Kaydet (Audit Trail)

Gateway sonucu ne olursa olsun **bir Payment kaydı yazıyoruz**:
> var payment = new Payment { ... Status = gatewayResult.IsSuccess ? PaymentStatus.Success : PaymentStatus.Failed ... };

Status alanı koşullu: IsSuccess true ise Success, değilse Failed.

**Bu neden önemli?** Banka dünyasında "denedik ama olmadı" bilgisi de değerli. Audit trail (denetim izi) için her deneme kayıt altına alınır. Gateway başarısız olunca Payment yazmasaydık, kullanıcı tekrar denerse aynı şekilde başarısız olabilir, hiç iz kalmazdı.

> Sonra `_db.Payments.Add(payment); await _db.SaveChangesAsync();` ile DB'ye INSERT.

### Adım 6: Success Sonucu Dön

Service final olarak:
> return new PaymentCreateResult(PaymentCreateOutcome.Success, ToDto(payment));

`Outcome.Success` ne demek? **"İşlem akışı başarılı bir şekilde sonlandı."** Status alanı içerideki Payment'ın Failed olmasına engel değil — outcome ayrı, payment status ayrı.

### Controller'a Geri Dönüş

Controller `result.Outcome`'a bakıyor ve switch expression ile HTTP koduna çeviriyor:
- **Success** → 201 Created, body'de PaymentResponseDto (Status alanı Success ya da Failed olabilir)
- **SubscriptionNotFound** → 404 Not Found
- **PeriodAlreadyPaid** → 409 Conflict

> ÖNEMLİ: Gateway başarısız olsa BİLE 201 dönüyoruz. Çünkü kayıt oluştu. Body'deki `"status": "Failed"` durumu söylüyor. Bu RESTful tasarımla uyumlu — "kaynak yaratıldı" demek "iş başarılı" demek değil.

### Frontend'e Cevap

Frontend 201 alıyor, status alanını kontrol ediyor:
- "Success" ise yeşil toast: "Ödeme başarılı — 287,45 ₺"
- "Failed" ise kırmızı toast: "Ödeme BAŞARISIZ — 287,45 ₺"

Eğer 409 geldiyse error message gösteriyor: "Bu dönem için zaten başarılı bir ödeme var."

### Bu Akışın "Neden" Soruları

**Neden borç sorgudan ÖNCE period check yapıyorum?** Çünkü borç sorgulama yavaş (mock'ta 200-500ms, gerçekte daha çok). Period check hızlı (DB'de basit query). Kullanıcıya hızlı reddetmek daha iyi.

**Neden Gateway başarısız olunca da kayıt yazıyorum?** Audit. Banka regülasyonları her ödeme denemesi izi tutmayı isteyebilir.

**Neden Result pattern, exception fırlatma değil?** Çünkü "abonelik bulunamadı" beklenen bir durum, hata değil. Exception, beklenmeyen durumlar için. Beklenen sonuç akışı için enum daha temiz.

---

## BÖLÜM 7: YENİ ÖZELLİK EKLEMEK İSTESEM NE YAPARIM?

### Senaryo A: Müşteriye Adres Alanı Eklemek

**Hedef:** Customer entity'sine `Address` alanı eklemek.

**Adımlar:**

1. **`Models/Entities/Customer.cs`** dosyasını aç. `PhoneNumber` property'sinin altına yeni bir property ekle:
   - `[MaxLength(500)] public string? Address { get; set; }` — opsiyonel, max 500 karakter.

2. **`Models/Dtos/CustomerDtos.cs`** dosyasını aç:
   - `CustomerCreateDto`'ya aynı alanı ekle (yeni müşteri yaratırken adres göndereblimek için).
   - `CustomerResponseDto`'ya da ekle (response'ta dönsün).
   - `CustomerSummaryDto`'ya isteğe göre — şart değil.

3. **`Services/CustomerService.cs`** dosyasını aç:
   - `CreateAsync` metodundaki `new Customer { ... }` bloğuna `Address = dto.Address` ekle.
   - `ToDto` helper metoduna `Address = c.Address` ekle.

4. **DB'yi güncelle.** EnsureCreated kullandığımız için **otomatik schema güncelleme yok**. Demo için en kolay yol:
   - PostgreSQL'de `subscription_tracker` veritabanını drop edip yeniden yarat.
   - veya `ALTER TABLE "Customers" ADD COLUMN "Address" varchar(500);` SQL'ini elle çalıştır.

5. **`wwwroot/index.html`** içinde Müşteri formuna yeni input ekle:
   - `<input name="address" maxlength="500" ... />`
   - Ve Customer kart render'ında `${c.address || "adres yok"}` göster.

6. `dotnet build` → 0 hata → `dotnet run` → tarayıcıda test.

> Toplam: 1 entity + 1 DTO dosyası + 1 service + 1 HTML değişikliği. ~10 dakika.

### Senaryo B: SMS Bildirim Servisi (3. Mock Servis)

**Hedef:** Ödeme başarılı olduğunda kullanıcıya SMS atmak için 3. mock servis eklemek.

**Adımlar:**

1. **Yeni dosya:** `Services/External/INotificationService.cs`
   - Interface: `Task SendPaymentNotificationAsync(string phoneNumber, decimal amount, string providerName)` metodu.

2. **Yeni dosya:** `Services/External/MockNotificationService.cs`
   - INotificationService implementasyonu.
   - Method içinde `await Task.Delay(100, 300)` (gecikme), sonra Console.WriteLine ile "[MOCK SMS] {phoneNumber}'e {amount} ₺ ödeme bildirildi" yaz. (Veya logger kullan.)

3. **`Program.cs`'e register et:**
   - `builder.Services.AddScoped<INotificationService, MockNotificationService>();`
   - Diğer mock servislerin yanına yaz.

4. **`Services/PaymentService.cs`** dosyasını aç:
   - Constructor'a 4. parametre ekle: `INotificationService notification`. Field olarak sakla.
   - `CreateAsync` metodunun en altında, payment kaydedildikten sonra eğer Status==Success ise:
     - Customer bilgisini de almak gerek (PhoneNumber için). Bunun için aboneliğin Customer'ını yükle: `var customer = await _db.Customers.FindAsync(subscription.CustomerId);`
     - Eğer customer.PhoneNumber boş değilse: `await _notification.SendPaymentNotificationAsync(customer.PhoneNumber, debt.Amount, subscription.ProviderName);`

5. `dotnet build` → 0 hata → `dotnet run` → ödeme yap → Console'da SMS log'unu gör.

> Toplam: 2 yeni dosya + 1 register satırı + 1 service değişikliği. ~15 dakika.

### Senaryo C: Abonelik Güncelleme Logları (Yeni Tablo)

**Hedef:** Bir abonelikte değişiklik yapıldığında "ne zaman, ne değişti" diye log tutan yeni bir tablo.

**Adımlar:**

1. **Yeni dosya:** `Models/Entities/SubscriptionAuditLog.cs`
   - Properties: Id (Guid), SubscriptionId (Guid), ChangedAt (DateTime UTC), FieldName (string), OldValue (string), NewValue (string).

2. **`Data/AppDbContext.cs`** dosyasını aç:
   - `public DbSet<SubscriptionAuditLog> SubscriptionAuditLogs => Set<SubscriptionAuditLog>();` ekle.
   - OnModelCreating'te FK ilişkisi ekleyebilirsin: `Entity<SubscriptionAuditLog>().HasOne<Subscription>().WithMany().HasForeignKey(a => a.SubscriptionId).OnDelete(DeleteBehavior.Cascade);`

3. **Yeni dosya:** `Models/Dtos/SubscriptionAuditLogDtos.cs`
   - SubscriptionAuditLogResponseDto.

4. **`Services/SubscriptionService.cs`** dosyasını aç:
   - `UpdateAsync` metodunun içinde, eski değerlerle yeni değerleri karşılaştır. Her değişen alan için bir SubscriptionAuditLog kaydı yarat ve `_db.SubscriptionAuditLogs.Add()` ile ekle.
   - SaveChangesAsync zaten en sonda — log'lar da aynı transaction'da yazılır.

5. (Opsiyonel) Yeni endpoint yapmak için:
   - `Services/ISubscriptionService.cs`'e `Task<List<SubscriptionAuditLogResponseDto>> GetAuditLogsAsync(Guid subscriptionId);` ekle.
   - Service'te implement et (`_db.SubscriptionAuditLogs.Where(a => a.SubscriptionId == id).ToListAsync()`).
   - `Controllers/SubscriptionsController.cs`'e `[HttpGet("{id:guid}/audit")]` endpoint'i ekle.

6. **DB'yi güncelle:** EnsureCreated yeni tabloyu otomatik yaratmaz (sadece DB hiç yoksa yaratır). DB'yi drop edip yeniden yarat, ya da elle CREATE TABLE.

7. `dotnet build` → 0 hata → `dotnet run` → bir aboneliği güncelle → audit endpoint'ten logları gör.

> Toplam: 1 yeni entity + 1 yeni DTO + 1 service değişikliği + 1 controller değişikliği + DbContext değişikliği + DB schema. ~30 dakika.

---

## BÖLÜM 8: CANLI'YA ALMA (Production Deployment)

Şu an proje kişisel bilgisayarında çalışıyor. Gerçek bir bankada production'a almak için 7 büyük adım var:

### 1. EnsureCreated → EF Migrations'a Geçiş

**Sorun:** EnsureCreated() sadece DB hiç yoksa tablo yaratıyor. Production'da schema değişiklikleri olur (yeni alan, yeni tablo) — ve verileri silmek istemezsiniz.

**Çözüm:** EF Migrations. Her schema değişikliği için bir migration dosyası oluşturulur (`dotnet ef migrations add <isim>`). Bu dosya hem yeni schema'ya geçişi (Up) hem geri dönüşü (Down) içerir. Production'a deploy ederken `dotnet ef database update` ile sırayla migration'lar çalışır, mevcut veriler korunur.

### 2. Connection String'i Environment Variable'a Taşıma

**Sorun:** `appsettings.json`'da `Password=app_pass_123` yazılı. Bu git'e commit'lendi. Production şifresi asla kaynak kodda olmamalı.

**Çözüm:** Production'da connection string'i ortam değişkeninden (environment variable) oku. ASP.NET zaten `ConnectionStrings__DefaultConnection` adlı env var'ı otomatik tanır. Cloud sağlayıcılarında (Azure Key Vault, AWS Secrets Manager) gizli bilgileri saklarsın, deploy sırasında env var olarak geçirilir.

### 3. Mock Servisleri Gerçek Servislerle Değiştirme

**Sorun:** Şu an MockDebtInquiryService random tutar dönüyor. Gerçek dünyada Boğaziçi Elektrik'in API'sine bağlanmak gerek.

**Çözüm:** `RealDebtInquiryService` adlı yeni bir sınıf yaz. İçinde `HttpClient` (HTTP istemcisi) ile gerçek API'ye çağrı at. Authentication için API key gerekir, bu da env var'dan alınır. `Program.cs`'te tek satır değişikliği: `AddScoped<IDebtInquiryService, MockDebtInquiryService>` → `AddScoped<IDebtInquiryService, RealDebtInquiryService>`. Aynı şey gateway için (İyzico SDK).

Ayrıca **Polly** adlı kütüphane ile retry (hatalı çağrıyı tekrar dene) ve circuit breaker (sürekli patlayan servisi geçici olarak çağırma) eklersin.

### 4. Authentication Eklemek (JWT)

**Sorun:** Şu an herkes herkese ait müşterileri silebilir. Yetki yok.

**Çözüm:** JWT (JSON Web Token) bearer authentication. Login endpoint'i ekle (kullanıcı adı/şifre alıp JWT döner). Korunması gereken endpoint'lere `[Authorize]` attribute koy. Token süresi 1 saat, refresh token mekanizması.

Ayrıca yetki seviyeleri: müşteri sadece kendi aboneliklerini görsün. Bunun için `User.Identity` üzerinden customer ID alıp service'lerde filtreleme yap.

### 5. Docker'a Paketleme

**Sorun:** Sunucuya deploy ederken ".NET SDK kur, EF Core kur, dependencies kur..." denemez. Tek paket lazım.

**Çözüm:** Multi-stage Dockerfile yaz:
- Stage 1 (build): .NET SDK image'ı kullan, projeyi build et, publish et
- Stage 2 (runtime): .NET runtime image'ı (daha küçük) kullan, sadece publish çıktısını kopyala

Ayrıca `docker-compose.yml` ile API + PostgreSQL'i tek komutla ayağa kaldırırsın (`docker compose up`).

### 6. CI/CD Pipeline (GitHub Actions)

**Sorun:** Her değişiklikte el ile build/test/deploy yapmak hata kaynağı.

**Çözüm:** GitHub Actions workflow yaz (`.github/workflows/build.yml`):
- Push olunca otomatik trigger
- Adımlar: checkout → dotnet restore → dotnet build → dotnet test → dotnet publish → Docker image build → Container Registry'e push → Production server'a deploy

Branch protection ile main'e doğrudan push'u engelle, PR ve review zorunlu yap.

### 7. Cloud'a Deploy (Azure App Service veya AWS)

**Sorun:** Lokalden production server'a geçiş.

**Çözüm:** Cloud sağlayıcısı seç (Azure App Service, AWS Elastic Beanstalk, veya sadece bir VM). Adımlar:
- Database: Azure Database for PostgreSQL (yönetilen servis)
- App: Azure App Service for Containers (Docker image'ı çekip çalıştırıyor)
- Secrets: Azure Key Vault
- Logging: Azure Application Insights (loglar, performans)
- Monitoring: alerting (5xx hata oranı yükselince mail at)
- Backup: günlük otomatik DB backup

Domain ayarla, HTTPS sertifikası al (Let's Encrypt veya Azure'un kendi cert manager'ı).

### Bonus Adımlar

- **Unit/Integration testler:** xUnit + Testcontainers (Postgres) ile gerçek DB üzerinde test.
- **Rate limiting:** Aynı IP'den dakikada 100 istekten fazlasını engelle.
- **API versioning:** /api/v1/customers gibi.
- **Swagger'ı production'da kapat:** Sadece development'ta aktif.

---

## BÖLÜM 9: MUHTEMEL MÜLAKAT SORULARI VE CEVAPLARI

### Mimari Sorular

**S1. Neden Clean Architecture (multi-project) değil de tek proje?**

Bu projede 4-5 katmanlı bir Clean Architecture overkill (gereksiz karmaşıklık) olurdu. Toplam ~25 dosyalık küçük bir case'iz. Her katman ayrı projede olunca dolaşmak zorlaşır, küçük bir değişiklik için 5 dosya açmak gerek. Tek proje + klasör tabanlı yapı ile aynı separation of concerns (görev ayrımı) sağlandı: Models, Services, Controllers ayrı klasörlerde. Büyük projede Clean Architecture mantıklı, burada değildi.

**S2. Neden Repository pattern yok?**

EF Core'un `DbSet<T>` zaten repository pattern'in implementasyonu. Üzerine ikinci bir katman koymak (`ICustomerRepository`) sadece kod miktarını artırır, gerçek bir fayda sağlamaz. Servislerimde doğrudan `_db.Customers` kullanıyorum — açık, okunur, test edilebilir (test'te `DbContextOptionsBuilder.UseInMemoryDatabase` ile sahte DB verilebilir). Büyük projelerde özel sorgu mantığı çok yoğunsa repository mantıklı olabilir, burada değil.

**S3. Dependency Injection nedir, neden kullandın?**

DI, bir sınıfın bağımlılıklarını kendisi yaratmak yerine constructor'dan alması. Faydaları: gevşek bağlılık (loose coupling), test edilebilirlik (mock verebilirsin), değiştirilebilirlik (mock servisten gerçek servise geçiş tek satır). ASP.NET Core'un built-in container'ı (`builder.Services`) zaten var; AddScoped/Singleton/Transient ile lifetime ayarlıyorsun. Tüm servislerimi `AddScoped` ile kayıt ettim çünkü her HTTP isteği için yeni instance üretilmesi gerekir.

### Domain Soruları

**S4. Aynı dönem tekrar ödeme nasıl engelleniyor?**

`PaymentService.CreateAsync` içinde, gateway'i çağırmadan önce şu kontrolü yapıyorum: aynı `(SubscriptionId, PeriodYear, PeriodMonth, Status=Success)` kombinasyonu DB'de var mı? `AnyAsync` ile (sadece var/yok kontrolü, tüm kayıtları getirmiyor). Varsa servis `PaymentCreateOutcome.PeriodAlreadyPaid` döner, controller bunu 409 Conflict'e çevirir. Önemli detay: Status=Success şartı var — Failed kayıtlar engel değil, çünkü kullanıcının başarısız ödemeyi tekrar denemesi gerek.

**S5. Gateway başarısız olursa ne oluyor?**

Gateway başarısız olsa bile Payment kaydı yine veritabanına yazılıyor — Status=Failed olarak. Bu **audit trail** için kritik. Banka dünyasında "denedim ama olmadı" bilgisi kaybedilemez. HTTP response 201 Created dönüyor (kayıt oluştu), body'deki `status: "Failed"` alanı durumu söylüyor. Aynı dönem için kullanıcı tekrar deneyebilir; Failed kayıtlar 409'u tetiklemez. Bir sonraki deneme başarılı olursa Success kaydı eklenir, ondan sonra 409 başlar.

**S6. Mock servisler gerçek servisle nasıl değiştirilir?**

İki dosya değişir, ikisi de minimal: 1) Yeni bir `RealDebtInquiryService` sınıfı yaz, `IDebtInquiryService` interface'ini implement et, içinde `HttpClient` ile gerçek API'ye çağrı at. 2) `Program.cs`'te tek satır değiştir: `AddScoped<IDebtInquiryService, MockDebtInquiryService>` → `AddScoped<IDebtInquiryService, RealDebtInquiryService>`. PaymentService ve diğer business kodları hiç değişmez — interface arkasında olduğu için. Bu Dependency Injection + Interface kullanımının en güzel pratik faydası.

### Teknik Sorular

**S7. EnsureCreated ile Migrations farkı ne?**

EnsureCreated tablolar yoksa yaratır, varsa hiçbir şey yapmaz. Schema değişikliklerini takip etmez — yeni alan eklersen DB'yi silip yeniden yaratman lazım, veriler kaybolur. Migrations ise versiyonlanabilir bir zincir — her schema değişikliği yeni dosya, hem ileri hem geri yönde uygulanabilir. Production için Migrations şart. Ben bu case'de süre kısa olduğu için EnsureCreated tercih ettim — sadece bir kez tablo yaratılması yeter. README'nin 9. bölümünde bu eksik olarak listeli.

**S8. decimal neden kullanıldı, double olsa ne olurdu?**

Para. `double` ve `float` IEEE 754 floating-point standardını kullanır — bunlar binary'de saklandığı için 0.1 + 0.2 = 0.30000000000000004 gibi yuvarlak hatalar verir. Finansal hesaplamalarda kuruş kaybı kabul edilemez. `decimal` tam ondalık hassasiyetle saklar, 0.1 + 0.2 = 0.3'tür. PostgreSQL'de `numeric(18,2)` olarak saklanıyor — 18 hane toplam, 2'si ondalık. Tüm Amount alanlarımız `decimal` tipinde, asla `double` değil.

**S9. async/await burada neden var?**

Veritabanı sorguları, dış servis çağrıları yavaş işlerdir. "Klasik" senkron kodda kod o işin bitmesini bekleyerek thread'i bloklar — sunucu o anda başka istek alamaz. async/await ile thread serbest kalır, başka istekleri işler, beklenen iş bittiğinde devam eder. Bu sayede aynı sunucu çok daha fazla concurrent (eş zamanlı) kullanıcıya hizmet verir. Tüm servis methodlarımız `async Task<T>` dönüyor, DB çağrılarımız `ToListAsync`, `FindAsync`, `AnyAsync` gibi async versiyonlarını kullanıyor.

**S10. Entity ile DTO farkı ne?**

Entity = veritabanı tablosunun C# karşılığı. EF Core onu görüp INSERT/SELECT/UPDATE/DELETE SQL'leri üretiyor. DTO = API'den dış dünyaya çıkıp giren veri şekli. Neden ayrılar? Birincisi güvenlik: dışarıya her şeyi göstermek istemeyebilirsin (örn parola hash'i). İkincisi versiyon: API contract'ı stabil olmalı; entity'yi değiştirsen bile DTO'yu değiştirmeyebilirsin (geriye uyumluluk). Üçüncüsü: girdi DTO'larında ID dahil etmezsin — Id'yi sen üretirsin, kullanıcı belirleyemez.

**S11. Cascade delete ne demek?**

Bir parent satır silinince bağlı child satırların otomatik silinmesi. Bizde Customer silinince o müşterinin tüm Subscription'ları, onlar silinince de Payment'lar silinir. `OnDelete(DeleteBehavior.Cascade)` ile AppDbContext.OnModelCreating'te tanımlı. PostgreSQL'de bu FOREIGN KEY constraint'inin `ON DELETE CASCADE` kısmı oluyor. Avantaj: orphan (yetim) kayıt kalmıyor, manuel silme zinciri yazmıyorsun. Dezavantaj: yanlışlıkla bir customer silersen tüm geçmişi gider — production'da soft delete (silindi diye işaretle) tercih edilebilir.

### AI Soruları

**S12. AI'ı nasıl kullandın?**

Anthropic'in Claude Code ürünüyle pair-programming şeklinde çalıştım. Önce CLAUDE.md adında bir "anayasa" dosyası yazdım — teknik tercihler (.NET 10, PostgreSQL), yasak listeler (no AutoMapper, no MediatR), klasör yapısı, davranış kuralları. AI bu dosyayı her oturumun başında okudu. Sonra projeyi 4 aşamada böldük: scaffold, CRUD, mock servisler, frontend+docs. Her aşama sonunda AI durup onayımı bekliyordu. Build patlarsa devam etmiyordu.

**S13. AI çıktılarını nasıl kontrol ettin?**

4 katmanlı kontrolüm vardı: (1) Her aşama sonunda `dotnet build` — derleme hatasız mı? (2) `dotnet run` ile uygulamayı ayağa kaldırıp Swagger UI üzerinden her endpoint'i manuel test ettim. (3) Frontend'den uçtan uca demo akışı yaptım — müşteri ekleme, abonelik, borç sorgu, ödeme, tekrar ödeme engeli (409), özet. (4) Uç durum kontrolü: olmayan müşteri (404), gateway fail (201+Failed), aynı dönem (409). Özellikle PaymentService.CreateAsync'i satır satır okudum — borç sorgu, gateway, kaydetme sıralamasının doğruluğunu teyit ettim.

**S14. Hangi kararları sen verdin?**

Tüm tasarım kararları benim: Clean Architecture yerine tek proje, EF Migrations yerine EnsureCreated, Result pattern (exception değil), gateway fail için 201+Failed status, period kontrolü için service-level check (DB index değil), Vanilla JS + Tailwind CDN (React değil), Data Annotations (FluentValidation değil), AutoMapper yerine manuel mapping, global ExceptionHandlerMiddleware. Her kararın gerekçesini README'nin 7. ve 8. bölümlerinde tablolaştırdım.

### Genel Sorular

**S15. Bu projeye daha fazla zamanın olsaydı ne eklerdin?**

Önce JWT bearer authentication (gerçek bir banka uygulaması authsız olamaz). Sonra unit + integration testler (xUnit + Testcontainers ile gerçek PostgreSQL üzerinde end-to-end test). Sonra EF Migrations'a geçiş — production'a aday yapmak için. SMS/Email bildirim servisi (3. mock) — case "en az 2" diyor ama 3 olsa daha tatmin edici. Polly ile retry/circuit breaker mock servislere — gerçek hayatta dış servisler aksıyor. Son olarak Docker + docker-compose ile tek komutla çalıştırılabilir hale getirme.

**S16. En zorlandığın kısım ne oldu?**

PostgreSQL'in DateTime davranışı. Npgsql 6+ default olarak `Kind=Utc` DateTime'leri `timestamp without time zone` kolona yazamıyor — runtime'da exception veriyor. Build aşamasında görünmüyor, sadece çalıştırınca patlıyor. Çözüm: AppDbContext.OnModelCreating'te bir foreach ile tüm DateTime kolonlarını `timestamp with time zone` (timestamptz) tipine çevirdim. Bu .NET ekosistemine özgü bir tuzak; AI yardımıyla erken yakaladık.

**S17. Bu sistemi 1000 kullanıcıya ölçeklendirsen ne değişir?**

Kod değişmez, mimari kararlar değişir: (1) PostgreSQL'i yönetilen bir servise (Azure Database for PostgreSQL) taşı, read replica ekle. (2) API'yi container'da koş, en az 2 instance + load balancer (yatay ölçeklendirme). (3) Mock dış servislere gerçek HTTP client + Polly retry/circuit-breaker. (4) Cache layer (Redis) — sık erişilen veri (customer profili) için. (5) Background job sistemi (Hangfire veya Azure Functions) — bildirim gönderme, raporlama. (6) Application Insights ile performans takibi. (7) Rate limiting — DDoS ve abuse'a karşı. async/await zaten temelde olduğu için single instance bile birkaç yüz concurrent isteği kaldırır.

---

## BÖLÜM 10: DEMO AKIŞI (SUNUM SCRIPT'İ)

> Tam cümleleri yazıyorum. Ezberleme — okuduğun zaman havayı içine sindir, kendi kelimelerinle anlat.

### Açılış (1-2 dakika)

> "Merhaba. Size **Subscription Tracker** adını verdiğim bir proje sunacağım. Bankacılık case study'si olarak istediniz: müşterilerin elektrik, su, internet gibi düzenli faturalarını tek bir platformda yönetmesini sağlıyor — abonelik tanımlama, borç sorgulama, ödeme yapma, tüm geçmişi görme.
>
> Backend tarafında **.NET 10 + ASP.NET Core Web API + Entity Framework Core + PostgreSQL** kullandım. Frontend için tek bir HTML dosyası — Tailwind CDN ile stillenmiş, vanilla JavaScript ile fetch çağrıları yapan basit bir SPA. Build adımı yok, Node bağımlılığı yok.
>
> Sunumu canlı demo ile başlatmak istiyorum, sonra mimariye geçeriz."

### Canlı Demo (5-7 dakika)

**1. Frontend'i aç:**

> "Önce uygulamayı açıyorum: https://localhost:7220. Burada üç sekme görüyorsunuz: Müşteriler, Abonelikler, Ödemeler. Sistem açılırken seed data ile 3 müşteri ve 7 abonelik hazır geldi."

**2. Bir müşterinin özetini göster:**

> "Şimdi Ahmet Yılmaz'ın özetine bakalım. *Özet butonuna basıyorsun*. Burada görüyorsunuz: 3 aktif aboneliği var (elektrik, internet, su). Bu ay (Mayıs 2026) henüz hiçbiri ödenmemiş — kırmızı listede. Son ödemeleri tablosunda Ocak ve Şubat 2026 için yaptığı elektrik ödemeleri görünüyor. Bu ekran case'in 'görüntüleme' başlığını karşılıyor."

**3. Borç sorgu:**

> "Şimdi abonelikler sekmesine geçiyorum. Ahmet'in elektrik aboneliği için 'Borç Sorgula' diyorum. *Modal açılıyor*. 287 küsür TL borç görünüyor, son ödeme tarihi 30 gün sonra. Bu rakam her seferinde farklı çıkıyor — çünkü mock 3rd-party servis çağrılıyor, 200-500ms gecikme ile random tutar üretiyor. Gerçek hayatta Boğaziçi Elektrik'in API'sine HTTP çağrı atılırdı; biz mock implementation yazdık."

**4. Ödeme yap:**

> "Şimdi 'Öde' butonuna basıyorum. *Toast belirir*. Yeşil bildirim geldi: ödeme başarılı, 287 TL. Akış kısaca: subscription bulundu, period kontrolü geçti, mock borç sorgu servisinden tutar alındı, mock gateway'e gönderildi, gateway başarılı dönünce Payment kaydı yazıldı. Tüm bu işlem 700ms civarında — iki external servis çağrısı dahil."

**5. Tekrar dene — 409 göster:**

> "Şimdi 'Öde' butonuna tekrar basıyorum. *Kırmızı toast*. 409 hatası: 'Bu dönem için zaten başarılı bir ödeme var.' İşte projenin **kritik iş kuralı**: aynı `(abonelik, yıl, ay)` üçlüsü için sadece bir başarılı ödeme olabilir. Bu kuralı `PaymentService.CreateAsync` içinde, gateway'e gitmeden önce `AnyAsync` ile zorluyorum. DB seviyesinde unique index koymadım — kuralın nerede yaşadığı kodda açıkça görünsün diye."

**6. (Eğer şanslıysan) %10 fail göster:**

> "Eğer ödemeyi 5-10 kez denerseniz birinde 'Failed' göreceksiniz — gateway %90 başarılı, %10 başarısız simülasyonu yapıyor. Failed durumda bile kayıt yazılır, audit trail için. Bu durumda 409 olmaz, kullanıcı tekrar deneyebilir."

**7. Swagger UI'ı aç:**

> "Tüm API'yi Swagger üzerinden de gezebilirsiniz. *Swagger sekmesi*. 14 endpoint var: customers, subscriptions, payments için CRUD, ek olarak debt-inquiry ve summary. Her endpoint'in request/response şemasını otomatik dokümante ediyor — Swashbuckle paketi sayesinde."

### Mimari Anlatım (3-5 dakika)

> "Şimdi kod tarafına bakalım. *VS Code'a geç*.
>
> Tek proje, klasör tabanlı yapı kullandım. Multi-project Clean Architecture overkill olurdu bu boyutta. Klasörler: Controllers, Services (External alt klasörü ile), Models (Entities ve Dtos), Data, Middleware, wwwroot.
>
> *PaymentService.cs'i aç*. Bu projenin en önemli dosyası. CreateAsync metodu sırayla: subscription bul, period check, debt inquiry, gateway, kaydet. Service exception fırlatmıyor — `PaymentCreateOutcome` adında enum + record döndürüyor. Controller bunu switch expression ile HTTP koduna çeviriyor: Success→201, NotFound→404, PeriodAlreadyPaid→409.
>
> *AppDbContext.cs'i aç*. EF Core'un veritabanı oturumu. OnModelCreating'te cascade delete kuralları, enum konversiyonları, ve PostgreSQL için tüm DateTime kolonlarını timestamptz olarak işaretleyen bir foreach var. Bu son foreach Npgsql'in bir tuzağı için — DateTime.UtcNow'u timestamp without time zone'a yazamıyor.
>
> *Program.cs'i aç*. Tüm DI registration'ları, pipeline sıralaması, ve seed data burada. Mock servisler 2 satır ile register edilmiş; gerçek servise geçiş tek satır değişikliği."

### AI Kullanımı Anlatımı (2-3 dakika)

> "Bu projeyi Anthropic'in Claude Code ürünüyle pair-programming şeklinde geliştirdim. Önce CLAUDE.md adında bir anayasa dosyası yazdım: hangi teknolojileri kullanacağımızı, hangilerini KULLANMAYACAĞIMIZI (AutoMapper, MediatR, FluentValidation gibi), klasör yapısını, davranış kurallarını belirledim.
>
> AI'a 'sen yaz, ben kontrol edeyim' yaklaşımıyla yaklaşmadım. Projeyi 4 aşamada böldük: scaffold + entity'ler, CRUD + service'ler, mock servisler + ödeme akışı, frontend + middleware + dokümantasyon. Her aşama sonunda AI durup onayımı bekliyordu. Build patlarsa devam etmiyordu.
>
> Tasarım kararları benim. AI öneriler sundu, ben tartıp seçtim. Mesela Result pattern vs exception, EF Migrations vs EnsureCreated, gateway fail için HTTP kodu — bunları ben karara bağladım. AI bana en değerli katkısını syntax değil, .NET ekosistemine özgü tuzakları (Swashbuckle yok artık built-in OpenAPI var ama UI yok, Npgsql DateTime davranışı) yakalayarak sundu."

### Kapanış — Eksikler ve İyileştirme (1-2 dakika)

> "Eksiklerimi söyleyerek bitireyim — bunları bilinçli olarak yapmadım, çünkü case scope'una uygun olmazlardı.
>
> Birincisi authentication yok; gerçek bir banka uygulamasında JWT bearer token ile per-customer scope şart. İkincisi unit/integration test yok; xUnit + Testcontainers ile yazılır. Üçüncüsü EF Migrations yerine EnsureCreated kullandım — schema değişikliği için DB drop gerek; production'da migrations şart. Dördüncüsü Docker container'ı yok — multi-stage Dockerfile + docker-compose ile yazılırdı. Beşincisi mock servislere Polly retry/circuit-breaker eklenmedi.
>
> README'nin 9. bölümünde tüm bu iyileştirmeleri tablolaştırdım. Sorularınız var mı?"

### "Bilmiyorum" Demek İçin Cümle

> "İyi soru. Şu an aklımda net bir cevap yok ama düşüneyim — sanırım [tahmin]. Bunu projeye eklemiş olsaydım [şu yolu] denerdim."

Bu cümleyi mutlaka kullan. Mühendislik dünyasında "bilmiyorum, ama..." cevabı uydurma cevaptan **çok daha değerli**.

---

## SON SÖZ

Sen 4 aşamada bir banka uygulaması yaptın. Her satırı sen yazmamış olabilirsin, ama:
- **Mimari kararlar senin.**
- **Hangi teknolojinin neden seçildiğini biliyorsun.**
- **Akışı kafanda canlandırabiliyorsun.**
- **Eksikleri ve neden olmadıklarını biliyorsun.**

Bu bir mühendisin gerçek değeri. Mülakatçı senden mükemmel kod beklemiyor — **düşünme şeklini görmek** istiyor.

Kahveni yudumla. Otobüsten in. Sunumdan önce 5 dakika sessizce dur, derin nefes al. Sahibi olduğun bir projeyi anlatacaksın.

Bol şans 🍀
