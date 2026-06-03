using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHub.Models
{
    /// <summary>
    /// Kullanici - Etkinlik iliskilesini temsil eden kayit modeli.
    /// Bir kullanicinin bir etkinlige katilim talebini ve durumunu tutar.
    /// </summary>
    public class EventRegistration
    {
        // Birincil anahtar
        [Key]
        public int Id { get; set; }

        // Yabanci anahtar: Hangi kullanici kayit oldu
        [Required]
        public string UserId { get; set; } = string.Empty;

        // Yabanci anahtar: Hangi etkinlige kayit oldu
        [Required]
        public int EventId { get; set; }

        // Kayit olusturulma zamani
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        // Kayit durumu (Onaylandi, Beklemede, Iptal Edildi)
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Confirmed;

        // Ek notlar / kullanicinin iletisi
        [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olmalidir.")]
        [Display(Name = "Not")]
        public string? Notes { get; set; }

        [StringLength(120, ErrorMessage = "Ad Soyad en fazla 120 karakter olmalidir.")]
        [Display(Name = "Odeme Ad Soyad")]
        public string? PaymentFullName { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PaidAmount { get; set; }

        public DateTime? PaymentCompletedAt { get; set; }

        [StringLength(64)]
        public string? PaymentReference { get; set; }

        public DateTime? RefundRequestedAt { get; set; }

        [StringLength(500, ErrorMessage = "Iade notu en fazla 500 karakter olmalidir.")]
        public string? RefundNote { get; set; }

        // Katilim dogrulandi mi? (Admin tarafindan etkinlik sonrasi isaretlenir)
        public bool AttendanceConfirmed { get; set; } = false;

        // Fiziksel etkinliklerde QR check-in icin olusturulan benzersiz kod
        [StringLength(64)]
        public string? CheckInCode { get; set; }

        // Check-in zamani
        public DateTime? CheckedInAt { get; set; }

        // Navigasyon: Kayit yapan kullanici
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        // Navigasyon: Kayit yapilan etkinlik
        [ForeignKey("EventId")]
        public Event? Event { get; set; }
    }

    /// <summary>
    /// Kayit durumu enumerasyonu
    /// </summary>
    public enum RegistrationStatus
    {
        [Display(Name = "Onaylandi")]
        Confirmed = 1,

        [Display(Name = "Beklemede")]
        Pending = 2,

        [Display(Name = "Iptal Edildi")]
        Cancelled = 3
    }
}
