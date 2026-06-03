# EventHub - Etkinlik ve Workshop Yonetim Sistemi

Programlama Dersi 2. Proje | ASP.NET Core 8 MVC

---

## Proje Ozeti

EventHub, etkinlik organizatorleri ile katilimcilari bir araya getiren kapsamli bir yonetim platformudur.
Kullanicilar etkinliklere kayit olabilir, adminler etkinlik olusturabilir ve katilimcilari yonetebilir.

---

## Kurulum Talimatları

### Gereksinimler
- .NET 8 SDK
- SQL Server (LocalDB veya Express)
- Visual Studio 2022 / VS Code

### Adimlar

1. Repoyu klonlayin
```
git clone https://github.com/kullanici/EventHub.git
cd EventHub
```

2. Veritabanini olusturun
```
dotnet ef database update
```

3. Uygulamayi baslatin
```
dotnet run
```

4. Tarayicide acin: `https://localhost:5001`

### Demo Giris Bilgileri
| Rol   | E-posta               | Sifre     |
|-------|-----------------------|-----------|
| Admin | admin@eventhub.com    | Admin123! |

---

## Proje Dosya Yapisi

```
EventHub/
|
|-- EventHub.csproj                          # Proje dosyasi, NuGet paketleri
|-- Program.cs                               # Uygulama baslangici, DI konfigurasyonu
|-- appsettings.json                         # Veritabani baglantisi, genel ayarlar
|-- appsettings.Development.json             # Gelistirme ortami ayarlari
|
|-- Models/                                  # Veritabani varlık modelleri
|   |-- ApplicationUser.cs                   # Identity uzerine genisletilmis kullanici modeli
|   |-- Event.cs                             # Etkinlik modeli (kategori, durum enumlari dahil)
|   |-- EventRegistration.cs                 # Kullanici-Etkinlik kayit modeli
|
|-- Data/                                    # Veritabani katmani
|   |-- ApplicationDbContext.cs              # EF Core DbContext, model yapılandirmasi, seed data
|
|-- ViewModels/                              # Veri transfer nesneleri (DTO)
|   |-- ViewModels.cs                        # Tum ViewModel siniflari tek dosyada
|                                            #   RegisterViewModel, LoginViewModel
|                                            #   EventListItemViewModel, EventDetailViewModel
|                                            #   EventFormViewModel, ParticipantViewModel
|                                            #   HomeViewModel, UserProfileViewModel
|                                            #   AdminDashboardViewModel
|
|-- Services/                                # Is mantigi katmani
|   |-- EventService.cs                      # Etkinlik CRUD, kayit/iptal, listeleme islemleri
|
|-- Controllers/                             # HTTP istek isleme katmani
|   |-- HomeController.cs                    # Ana sayfa, hakkimizda, iletisim, gizlilik
|   |-- AccountController.cs                 # Kayit, giris, cikis, profil
|   |-- EventController.cs                   # Etkinlik listele, detay, olustur, duzenle, kayit ol
|   |-- AdminController.cs                   # Admin dashboard, kullanici yonetimi, katilim dogrulama
|
|-- Views/                                   # Razor view dosyalari
|   |-- _ViewImports.cshtml                  # Global using direktifleri
|   |-- _ViewStart.cshtml                    # Varsayilan layout belirleme
|   |
|   |-- Shared/                              # Paylasilan layout ve partial view'ler
|   |   |-- _Layout.cshtml                   # Ana layout (navbar, footer, tema toggle, alert alani)
|   |   |-- _EventCard.cshtml                # Etkinlik karti partial view (yeniden kullaniilabilir)
|   |   |-- _ValidationScriptsPartial.cshtml # jQuery validasyon scriptleri
|   |
|   |-- Home/                                # Ana sayfa view'leri
|   |   |-- Index.cshtml                     # Hero, istatistikler, one cikan etkinlikler, CTA
|   |   |-- About.cshtml                     # Hakkimizda sayfasi
|   |   |-- Contact.cshtml                   # Iletisim formu
|   |   |-- Privacy.cshtml                   # Gizlilik politikasi
|   |
|   |-- Event/                               # Etkinlik view'leri
|   |   |-- Index.cshtml                     # Etkinlik listesi, arama ve kategori filtresi
|   |   |-- Detail.cshtml                    # Etkinlik detayi, kayit formu, katilimci listesi
|   |   |-- Create.cshtml                    # Etkinlik olusturma formu (Admin)
|   |   |-- Edit.cshtml                      # Etkinlik duzenleme formu (Admin)
|   |   |-- MyEvents.cshtml                  # Kullanicinin kayitli oldugu etkinlikler
|   |
|   |-- Account/                             # Kimlik dogrulama view'leri
|   |   |-- Login.cshtml                     # Giris formu, sifre goster/gizle, demo bilgi
|   |   |-- Register.cshtml                  # Kayit formu, sifre guc gostergesi
|   |   |-- Profile.cshtml                   # Kullanici profil sayfasi
|   |   |-- AccessDenied.cshtml              # Yetkisiz erisim sayfasi
|   |
|   |-- Admin/                               # Admin panel view'leri
|       |-- Index.cshtml                     # Dashboard, istatistik kartlari, son etkinlikler
|       |-- Events.cshtml                    # Etkinlik yonetim tablosu (yayinla/sil/duzenle)
|       |-- Users.cshtml                     # Kullanici listesi, rol degistirme
|       |-- Participants.cshtml              # Etkinlik katilimci listesi, katilim dogrulama
|
|-- wwwroot/                                 # Statik dosyalar
|   |-- css/
|   |   |-- site.css                         # Ana stil dosyasi
|   |                                        #   CSS degiskenleri (acik/koyu tema)
|   |                                        #   Navbar, hero, butonlar, kartlar
|   |                                        #   Formlar, tablolar, animasyonlar
|   |                                        #   Responsive kurallar
|   |-- js/
|       |-- site.js                          # Ana JavaScript dosyasi
|                                            #   Tema yonetimi (localStorage)
|                                            #   Scroll animasyonlari (IntersectionObserver)
|                                            #   Sayac animasyonu, toast bildirimi
|                                            #   Form loading durumu, silme onay dialogu
|
|-- Migrations/                              # EF Core migration dosyalari
    |-- 20240101000000_InitialCreate.cs      # Ilk migration (tablolar + seed data)
    |-- ApplicationDbContextModelSnapshot.cs # Model snapshot (EF Core tarafindan yonetilir)
```

---

## Teknik Detaylar

### Kullanilan Teknolojiler
- **Framework**: ASP.NET Core 8 MVC
- **ORM**: Entity Framework Core 8 (Code-First)
- **Kimlik Dogrulama**: ASP.NET Core Identity
- **Veritabani**: SQL Server (LocalDB)
- **Frontend**: Bootstrap 5.3, Bootstrap Icons
- **Font**: Outfit + Fraunces (Google Fonts)

### Proje Gereksinimleri

#### 1. Veritabani
- EF Core Code-First yaklasimi
- ApplicationDbContext ile DbContext
- Migration ile veritabani olusturma
- Temel CRUD islemleri

#### 2. Authentication & Authorization
- ASP.NET Core Identity ile kullanici kayit/giris
- Role-based yetkilendirme: **Admin** / **User**
- Cookie tabanli oturum yonetimi
- [Authorize(Roles = "Admin")] attribute kullanimi

#### 3. UI & Layout
- _Layout.cshtml ile ortak Header/Footer
- ViewBag ve ViewModel kullanimi
- _EventCard.cshtml Partial View

#### 4. Validations
- Data Annotations: [Required], [StringLength], [EmailAddress], [Range], [Url]
- Client-side: jQuery Unobtrusive Validation
- Server-side: ModelState.IsValid kontrolu

### Roller ve Yetkiler

| Islem                          | Admin | User  |
|-------------------------------|-------|-------|
| Etkinlikleri goruntule         |  X    |  X    |
| Etkinlige kayit ol             |  X    |  X    |
| Kendi etkinliklerini listele   |  X    |  X    |
| Etkinlik olustur               |  X    |       |
| Etkinlik duzenle               |  X    |       |
| Etkinlik sil                   |  X    |       |
| Etkinlik yayinla/taslaga al    |  X    |       |
| Katilimci listesini gor        |  X    |       |
| Kullanici rollerini degistir   |  X    |       |
| Katilim dogrula                |  X    |       |

---

## Ozellikler

### Kullanici (User) Ozellikleri
- Etkinlikleri listeleme ve detay goruntuleme
- Arama ve kategori filtresi
- Etkinlige kayit olma (Join)
- Kayit iptali
- Kendi kayitli etkinliklerini listeleme
- Profil sayfasi

### Admin Ozellikleri
- Etkinlik olusturma, duzenleme, silme
- Kontenjan siniri belirleme
- Etkinlik yayinlama / taslaga alma
- Katilimci listesini goruntuleme
- Katilim dogrulama (hazirun tutanagi)
- Kullanici listesi ve rol yonetimi
- Dashboard ile platform istatistikleri

### UI/UX Ozellikleri
- Acik/Koyu tema toggle (localStorage ile saklanir)
- Responsive tasarim (mobil, tablet, masaustu)
- Kaydirma ile tetiklenen animasyonlar (IntersectionObserver)
- Yuzuyor (floating) hero karti animasyonu
- Kontenjan progress bar
- Sayac animasyonlari
- Toast bildirimleri
- Otomatik kapanan alert kutulari
- Sifre guc gostergesi
- Sifre goster/gizle butonu

---

## Veritabani Diyagramı

```
Users (ApplicationUser : IdentityUser)
  |-- Id (PK)
  |-- FullName
  |-- Bio
  |-- CreatedAt
  |
  |--< Events (CreatedByUserId FK)
  |--< EventRegistrations (UserId FK)

Events
  |-- Id (PK)
  |-- Title, Description, Summary
  |-- EventDate, EndDate
  |-- Location, OnlineUrl
  |-- MaxCapacity, Category, Status
  |-- IsPaid, Price
  |-- CreatedByUserId (FK -> Users)
  |
  |--< EventRegistrations (EventId FK)

EventRegistrations
  |-- Id (PK)
  |-- UserId (FK -> Users)
  |-- EventId (FK -> Events)
  |-- RegisteredAt, Status
  |-- AttendanceConfirmed, Notes
  |-- UNIQUE (UserId, EventId)

Identity Tablolari:
  Roles, UserRoles, UserClaims, UserLogins, UserTokens, RoleClaims
```

---

## Hizli Baslangic

```bash
# 1. Projeyi baslat
dotnet run

# 2. Tarayicide ac
https://localhost:5001

# 3. Admin ile giris yap
# admin@eventhub.com / Admin123!

# 4. Yeni etkinlik olustur
# Admin Panel > Yeni Etkinlik

# 5. Yeni kullanici ile kayit ol ve etkinlige katil
```
