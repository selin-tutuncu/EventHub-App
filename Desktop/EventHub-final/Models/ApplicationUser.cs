using Microsoft.AspNetCore.Identity;

namespace EventHub.Models
{
    /// <summary>
    /// ASP.NET Core Identity uzerine genisletilmis uygulama kullanicisi.
    /// Standart Identity alanlarinin yani sira ek profil bilgilerini barindirir.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Kullanicinin tam adi - zorunlu alan
        public string FullName { get; set; } = string.Empty;

        // Kullanicinin biyografi / tanitim yazisi
        public string? Bio { get; set; }

        // Profil fotografi URL'i (opsiyonel)
        public string? ProfileImageUrl { get; set; }

        // Hesap olusturulma tarihi
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigasyon: Bu kullanicinin katildigi etkinlik kayitlari
        public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();

        // Navigasyon: Bu kullanicinin olusturdugu etkinlikler (Admin/Organizator icin)
        public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    }
}
