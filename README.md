# Hotel Management API

Otel, oda ve rezervasyon yönetimi için geliştirilmiş bir REST API. Kullanıcılar otel ve oda arayıp rezervasyon yapabilir, otel sahipleri kendi otellerini ve odalarını yönetebilir, yöneticiler tüm rezervasyonları görüntüleyebilir.

Bu proje, yaz stajımın ikinci yarısında (20 Temmuz – 10 Ağustos 2026) backend / .NET alanında geliştirdiğim ana projedir.

## Kullanılan Teknolojiler

| Alan | Teknoloji |
|---|---|
| Dil / Platform | C#, .NET 10, ASP.NET Core Web API |
| Veritabanı | Microsoft SQL Server (Docker üzerinde) |
| ORM | Entity Framework Core (Code-First, Migrations) |
| Kimlik doğrulama | JWT Bearer |
| Şifre güvenliği | BCrypt |
| Loglama | Serilog (konsol ve günlük dosya) |
| Dokümantasyon / Test | Swagger (Swashbuckle) |

## Özellikler

- **Kimlik doğrulama:** Kayıt ve giriş işlemleri. Şifreler BCrypt ile karmalanarak saklanır, giriş yapan kullanıcıya JWT üretilir.
- **Rol tabanlı yetkilendirme:** `Customer`, `Owner` ve `Admin` rolleri. Otel sahibi yalnızca kendi otelini ve odalarını değiştirebilir.
- **Otel ve oda yönetimi:** Ekleme, listeleme, güncelleme ve silme işlemleri.
- **Rezervasyon:** Tarih doğrulaması, tarih aralığına göre oda müsaitlik (çakışma) kontrolü ve gece sayısı × gecelik fiyat üzerinden otomatik ücret hesaplama.
- **Ödeme ve iptal:** Ücret kullanıcının bakiyesinden düşülür ve ödeme kaydı oluşturulur. İptalde iade yapılır, geç iptalde ceza uygulanır.
- **Filtreleme ve sayfalama:** Oteller şehre ve arama metnine göre, odalar tipe ve en yüksek fiyata göre filtrelenir. Liste uç noktaları `page` ve `pageSize` parametreleriyle sayfalanır.
- **Merkezî hata yönetimi:** Tüm hatalar özel bir middleware tarafından yakalanır ve standart bir hata cevabına dönüştürülür.

## Mimari

İstek akışı şu şekildedir:

```
İstek → ExceptionHandlingMiddleware → Controller → Service → AppDbContext → SQL Server
```

- **Controllers:** İsteği karşılar, ilgili servisi çağırır ve uygun HTTP cevabını döndürür.
- **Services:** İş kurallarını içerir (sahiplik kontrolü, tarih çakışması, fiyat ve ceza hesabı). Servisler arayüzleri üzerinden Dependency Injection ile `Scoped` olarak kaydedilir.
- **Data:** `AppDbContext` ve uygulama açılışında rolleri ve yönetici kullanıcısını oluşturan `DataSeeder`.
- **Middleware:** Merkezî hata yakalama.
- **Migrations:** Veritabanı şemasının sürüm geçmişi.

## Veritabanı

| Tablo | Açıklama |
|---|---|
| Users | Kullanıcı bilgileri, şifre karması ve bakiye |
| Roles | Rol tanımları |
| UserRoles | Kullanıcı ile rol arasındaki çoka-çok ilişkinin ara tablosu |
| Hotels | Otel bilgileri ve otelin sahibi |
| Rooms | Odalar: tip, kapasite, gecelik fiyat |
| Reservations | Rezervasyon kayıtları, durum ve ceza tutarı |
| Payments | Ödeme hareketleri |

İlişkiler:

- Bir kullanıcının birden çok rezervasyonu olabilir (bire-çok).
- Bir otelin birden çok odası olabilir (bire-çok).
- Bir odanın birden çok rezervasyonu olabilir (bire-çok).
- Kullanıcı ile rol arasında çoka-çok ilişki vardır.

Kısıtlar: e-posta adresi benzersizdir; oda numarası aynı otel içinde benzersizdir.

## Uç Noktalar

### Auth

| Metot | Adres | Açıklama | Yetki |
|---|---|---|---|
| POST | `/api/auth/register` | Yeni kullanıcı kaydı | Herkese açık |
| POST | `/api/auth/login` | Giriş yapar ve JWT döndürür | Herkese açık |

### Hotels

| Metot | Adres | Açıklama | Yetki |
|---|---|---|---|
| GET | `/api/hotels` | Otelleri listeler (`city`, `search`, `page`, `pageSize`) | Herkese açık |
| GET | `/api/hotels/{id}` | Otel ayrıntısı | Herkese açık |
| POST | `/api/hotels` | Otel ekler | Owner, Admin |
| PUT | `/api/hotels/{id}` | Oteli günceller | Owner (kendi oteli), Admin |
| DELETE | `/api/hotels/{id}` | Oteli siler | Owner (kendi oteli), Admin |

### Rooms

| Metot | Adres | Açıklama | Yetki |
|---|---|---|---|
| GET | `/api/rooms/hotel/{hotelId}` | Otelin odalarını listeler (`type`, `maxPrice`, `page`, `pageSize`) | Herkese açık |
| POST | `/api/rooms` | Oda ekler | Owner, Admin |
| PUT | `/api/rooms/{id}` | Odayı günceller | Owner (kendi oteli), Admin |
| DELETE | `/api/rooms/{id}` | Odayı siler | Owner (kendi oteli), Admin |

### Reservations

| Metot | Adres | Açıklama | Yetki |
|---|---|---|---|
| POST | `/api/reservations` | Rezervasyon oluşturur | Customer, Admin |
| GET | `/api/reservations/my` | Kullanıcının kendi rezervasyonları | Customer, Admin |
| GET | `/api/reservations/{id}` | Rezervasyon ayrıntısı | Customer, Admin |
| PUT | `/api/reservations/{id}` | Rezervasyonu günceller | Customer, Admin |
| POST | `/api/reservations/{id}/cancel` | Rezervasyonu iptal eder | Customer, Admin |
| GET | `/api/reservations` | Tüm rezervasyonlar (sayfalı) | Admin |
| DELETE | `/api/reservations/{id}` | Rezervasyonu siler | Admin |

## Rezervasyon Kuralları

- Çıkış tarihi giriş tarihinden sonra olmalıdır; geçmiş bir tarihe rezervasyon yapılamaz.
- Oda müsaitliği tarih aralığına göre kontrol edilir. Yeni rezervasyon, aynı odanın aktif bir rezervasyonuyla şu koşulda çakışır:
  `yeniGiriş < mevcutÇıkış` ve `yeniÇıkış > mevcutGiriş`
- Ücret = gece sayısı × odanın gecelik fiyatı. Çıkış günü geceye dâhil değildir.
- Ücret kullanıcının bakiyesinden düşülür; bakiye yetersizse işlem reddedilir.
- Bakiye düşümü, rezervasyon ve ödeme kaydı tek bir kaydetme işlemiyle yazılır; böylece işlem ya tamamen gerçekleşir ya da hiç gerçekleşmez.
- İptalde ücret iade edilir; geç iptalde ceza tutarı kesilir.

## Hata Cevapları

| Durum kodu | Anlamı |
|---|---|
| 400 Bad Request | Geçersiz veri veya iş kuralı ihlali |
| 401 Unauthorized | Token yok veya geçersiz |
| 403 Forbidden | Kullanıcının bu işlem için yetkisi yok |
| 404 Not Found | Kayıt bulunamadı |
| 500 Internal Server Error | Beklenmeyen hata (ayrıntı istemciye açılmaz, loglanır) |

## Kurulum

Gereksinimler: .NET 10 SDK ve Docker.

1. Depoyu klonlayın:

```bash
   git clone https://github.com/pinartatar/HotelManagementAPI.git
   cd HotelManagementAPI/HotelManagement
```

2. SQL Server'ı Docker üzerinde başlatın:

```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<Sifreniz>" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

3. `appsettings.json` dosyasında `ConnectionStrings:DefaultConnection` ile `Jwt:Key`, `Jwt:Issuer` ve `Jwt:Audience` değerlerini kendi ortamınıza göre düzenleyin.

4. Veritabanını oluşturun:

```bash
   dotnet ef database update
```

5. Uygulamayı çalıştırın:

```bash
   dotnet run
```

6. Tarayıcıda `/swagger` adresini açın. Korumalı uç noktalar için önce giriş yapıp dönen token'ı **Authorize** butonuna yapıştırın.

## Geliştirici

Pınar Tatar
