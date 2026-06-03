using System.ComponentModel.DataAnnotations;
using EventHub.Models;
using Microsoft.AspNetCore.Http;

namespace EventHub.ViewModels
{
    // ============================================================
    // KIMLIK DOGRULAMA VIEWMODEL'LARI
    // ============================================================

    /// <summary>
    /// Kullanici kayit formu icin ViewModel.
    /// Data Annotations ile form validasyonu saglanir.
    /// </summary>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Ad Soyad 3 ile 100 karakter arasinda olmalidir.")]
        [Display(Name = "Kullanıcı Adı")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Gecerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sifre zorunludur.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Sifre en az 8 karakter olmalidir.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Sifre en az bir buyuk harf, bir kucuk harf, bir rakam ve bir ozel karakter icermelidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Sifre")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sifre tekrari zorunludur.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Sifreler eslesmemektedir.")]
        [Display(Name = "Sifre Tekrari")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Biyografi en fazla 500 karakter olmalidir.")]
        [Display(Name = "Hakkimda")]
        public string? Bio { get; set; }
    }

    /// <summary>
    /// Kullanici giris formu icin ViewModel
    /// </summary>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Gecerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Sifre")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni Hatirla")]
        public bool RememberMe { get; set; } = false;
    }

    // ============================================================
    // ETKINLIK VIEWMODEL'LARI
    // ============================================================

    /// <summary>
    /// Etkinlik listesi icin ozet bilgileri tasir
    /// </summary>
    public class EventListItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public EventCategory Category { get; set; }
        public EventStatus Status { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentParticipantCount { get; set; }
        public bool IsFullyBooked { get; set; }
        public bool IsPaid { get; set; }
        public decimal Price { get; set; }
        public string? CoverImageUrl { get; set; }
        public string OrganizerName { get; set; } = string.Empty;

        // Kullanicinin bu etkinlige kayitli olup olmadigini gosterir
        public bool IsRegistered { get; set; } = false;
        public RegistrationStatus? UserRegistrationStatus { get; set; }
        public int? RegistrationId { get; set; }
        public string? CheckInCode { get; set; }
        public string? CheckInUrl { get; set; }
        public string? CheckInQrCodeBase64 { get; set; }
        public bool IsWaitlisted => UserRegistrationStatus == RegistrationStatus.Pending;
    }

    /// <summary>
    /// Etkinlik detay sayfasi icin tam bilgileri tasir
    /// </summary>
    public class EventDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public DateTime EventDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? OnlineUrl { get; set; }
        public bool CanViewOnlineUrl { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentParticipantCount { get; set; }
        public int RemainingCapacity { get; set; }
        public bool IsFullyBooked { get; set; }
        public EventCategory Category { get; set; }
        public EventStatus Status { get; set; }
        public bool IsPaid { get; set; }
        public decimal Price { get; set; }
        public string? CoverImageUrl { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string OrganizerEmail { get; set; } = string.Empty;
        public string? OrganizerBio { get; set; }
        public DateTime CreatedAt { get; set; }

        // Mevcut kullanicinin kayit durumu
        public bool IsRegistered { get; set; } = false;
        public RegistrationStatus? RegistrationStatus { get; set; }
        public int? RegistrationId { get; set; }
        public string? CheckInCode { get; set; }
        public string? CheckInUrl { get; set; }
        public string? CheckInQrCodeBase64 { get; set; }
        public bool IsOfflineEvent { get; set; }
        public bool AttendanceConfirmed { get; set; }
        public string? PaymentFullName { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime? PaymentCompletedAt { get; set; }
        public string? PaymentReference { get; set; }
        public DateTime? RefundRequestedAt { get; set; }

        // Katilimci listesi (yalnizca Admin/Organizator gorebilir)
        public List<ParticipantViewModel> Participants { get; set; } = new();
    }

    public class EventPaymentViewModel
    {
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(120, MinimumLength = 3, ErrorMessage = "Ad Soyad 3 ile 120 karakter arasinda olmalidir.")]
        [Display(Name = "Ad Soyad")]
        public string PaymentFullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kart numarasi zorunludur.")]
        [RegularExpression(@"^[0-9\s]{16,23}$", ErrorMessage = "Gecerli bir kart numarasi giriniz.")]
        [Display(Name = "Kart Numarasi")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Son kullanma tarihi zorunludur.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/[0-9]{2}$", ErrorMessage = "AA/YY formatinda giriniz.")]
        [Display(Name = "Son Kullanma Tarihi")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV zorunludur.")]
        [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "Gecerli bir CVV giriniz.")]
        [Display(Name = "CVV")]
        public string Cvv { get; set; } = string.Empty;
    }

    public class EventRefundViewModel
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public decimal RefundAmount { get; set; }
        public string? PaymentFullName { get; set; }
        public string? PaymentReference { get; set; }

        [StringLength(500, ErrorMessage = "Iade notu en fazla 500 karakter olmalidir.")]
        [Display(Name = "Iade Notu")]
        public string? RefundNote { get; set; }
    }

    /// <summary>
    /// Etkinlik olusturma / duzenleme formu icin ViewModel
    /// </summary>
    public class EventFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Etkinlik basligi zorunludur.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Baslik 5 ile 150 karakter arasinda olmalidir.")]
        [Display(Name = "Etkinlik Basligi")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Aciklama zorunludur.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Aciklama en az 20 karakter olmalidir.")]
        [Display(Name = "Aciklama")]
        public string Description { get; set; } = string.Empty;

        [StringLength(300, ErrorMessage = "Ozet en fazla 300 karakter olmalidir.")]
        [Display(Name = "Kisa Ozet")]
        public string? Summary { get; set; }

        [Required(ErrorMessage = "Etkinlik tarihi zorunludur.")]
        [Display(Name = "Baslangic Tarihi ve Saati")]
        public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Bitis Tarihi ve Saati")]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Konum zorunludur.")]
        [StringLength(300, ErrorMessage = "Konum en fazla 300 karakter olmalidir.")]
        [Display(Name = "Konum / Adres")]
        public string Location { get; set; } = string.Empty;

        [Url(ErrorMessage = "Gecerli bir URL giriniz.")]
        [Display(Name = "Online Baglanti URL")]
        public string? OnlineUrl { get; set; }

        [Range(0, 10000, ErrorMessage = "Kontenjan 0 ile 10000 arasinda olmalidir.")]
        [Display(Name = "Maksimum Katilimci (0 = sinir yok)")]
        public int MaxCapacity { get; set; } = 0;

        [Required(ErrorMessage = "Kategori secimi zorunludur.")]
        [Display(Name = "Kategori")]
        public EventCategory Category { get; set; }

        [Display(Name = "Durum")]
        public EventStatus Status { get; set; } = EventStatus.Draft;

        [Display(Name = "Kapak Gorseli URL")]
        public string? CoverImageUrl { get; set; }

        [Display(Name = "Kapak Gorselini Yukle")]
        public IFormFile? CoverImageFile { get; set; }

        [Display(Name = "Ucretli Etkinlik")]
        public bool IsPaid { get; set; } = false;

        [Range(0, 99999, ErrorMessage = "Gecerli bir ucret giriniz.")]
        [Display(Name = "Ucret (TL)")]
        public decimal Price { get; set; } = 0;
    }

    /// <summary>
    /// Katilimci listesi icin ViewModel
    /// </summary>
    public class ParticipantViewModel
    {
        public int RegistrationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
        public RegistrationStatus Status { get; set; }
        public bool AttendanceConfirmed { get; set; }
        public string? Notes { get; set; }
    }

    // ============================================================
    // ANA SAYFA VIEWMODEL'LARI
    // ============================================================

    /// <summary>
    /// Ana sayfa icin istatistik ve ozet bilgileri tasir
    /// </summary>
    public class HomeViewModel
    {
        // Onsoz etkinlikler (yakin tarihli, yayinda olanlar)
        public List<EventListItemViewModel> FeaturedEvents { get; set; } = new();

        // Genel istatistikler
        public int TotalEvents { get; set; }
        public int TotalUsers { get; set; }
        public int TotalRegistrations { get; set; }
        public int UpcomingEventsCount { get; set; }
    }

    // ============================================================
    // PROFIL VIEWMODEL'LARI
    // ============================================================

    /// <summary>
    /// Kullanici profil sayfasi icin ViewModel
    /// </summary>
    public class UserProfileViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Kullanicinin katildigi etkinlikler
        public List<EventListItemViewModel> RegisteredEvents { get; set; } = new();

        // Toplam istatistikler
        public int TotalRegistrations { get; set; }
        public int UpcomingRegistrations { get; set; }
        public int PastRegistrations { get; set; }
    }

    /// <summary>
    /// Profil sayfasi icin kullanim alanlarini tek modelde birlestiren ViewModel.
    /// </summary>
    public class ProfilePageViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int TotalRegistrations { get; set; }
        public int UpcomingRegistrations { get; set; }
        public int PastRegistrations { get; set; }
        public bool IsEditPanelOpen { get; set; }
        public bool IsPasswordPanelOpen { get; set; }

        public ProfileEditViewModel Edit { get; set; } = new();
        public ChangePasswordViewModel Password { get; set; } = new();
    }

    /// <summary>
    /// Uygulama ici bildirimlerin listelenmesi icin ViewModel.
    /// </summary>
    public class UserNotificationViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool RequiresAction { get; set; }
        public string? EventTitle { get; set; }
        public int? EventId { get; set; }
        public int? RegistrationId { get; set; }
        public NotificationType Type { get; set; }
    }

    /// <summary>
    /// Profil bilgilerini guncellemek icin ViewModel.
    /// </summary>
    public class ProfileEditViewModel
    {
        [Required(ErrorMessage = "Kullanici adi zorunludur.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Kullanici adi 3 ile 100 karakter arasinda olmalidir.")]
        [Display(Name = "Kullanici Adi")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Biyografi en fazla 500 karakter olmalidir.")]
        [Display(Name = "Biyografi")]
        public string? Bio { get; set; }

        public string? ProfileImageUrl { get; set; }

        [Display(Name = "Profil Gorselini Degistir")]
        public IFormFile? ProfileImageFile { get; set; }
    }

    /// <summary>
    /// Sifre degistirme formu icin ViewModel.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut sifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Sifre")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yeni sifre zorunludur.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Yeni sifre en az 8 karakter olmalidir.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "Yeni sifre en az bir buyuk harf, bir kucuk harf, bir rakam ve bir ozel karakter icermelidir.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Sifre")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sifre tekrari zorunludur.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Sifreler eslesmemektedir.")]
        [Display(Name = "Yeni Sifre Tekrari")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ============================================================
    // ADMIN VIEWMODEL'LARI
    // ============================================================

    /// <summary>
    /// Admin panel anasayfa istatistikleri icin ViewModel
    /// </summary>
    public class AdminDashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int PublishedEvents { get; set; }
        public int DraftEvents { get; set; }
        public int TotalUsers { get; set; }
        public int TotalRegistrations { get; set; }
        public int TodayRegistrations { get; set; }

        // Son eklenen etkinlikler
        public List<EventListItemViewModel> RecentEvents { get; set; } = new();

        // Kategoriye gore etkinlik dagilimi
        public Dictionary<string, int> EventsByCategory { get; set; } = new();
    }
}
