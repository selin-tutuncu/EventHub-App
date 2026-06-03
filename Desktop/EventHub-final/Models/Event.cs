using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHub.Models
{
    /// <summary>
    /// Etkinlik ana modeli. Her etkinlik bir organizator tarafindan olusturulur
    /// ve katilimci kontenjanina sahiptir.
    /// </summary>
    public class Event
    {
        // Birincil anahtar - otomatik artan
        [Key]
        public int Id { get; set; }

        // Etkinlik basligi - zorunlu, maksimum 150 karakter
        [Required(ErrorMessage = "Etkinlik basligi zorunludur.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Baslik 5 ile 150 karakter arasinda olmalidir.")]
        [Display(Name = "Etkinlik Basligi")]
        public string Title { get; set; } = string.Empty;

        // Etkinlik detayli aciklamasi - zorunlu
        [Required(ErrorMessage = "Etkinlik aciklamasi zorunludur.")]
        [StringLength(2000, MinimumLength = 20, ErrorMessage = "Aciklama en az 20 karakter olmalidir.")]
        [Display(Name = "Aciklama")]
        public string Description { get; set; } = string.Empty;

        // Kisa ozet - liste gorunumlerinde kullanilir
        [StringLength(300, ErrorMessage = "Ozet en fazla 300 karakter olmalidir.")]
        [Display(Name = "Kisa Ozet")]
        public string? Summary { get; set; }

        // Etkinlik tarihi ve saati - zorunlu
        [Required(ErrorMessage = "Etkinlik tarihi zorunludur.")]
        [Display(Name = "Tarih ve Saat")]
        public DateTime EventDate { get; set; }

        // Etkinlik bitis tarihi
        [Display(Name = "Bitis Tarihi")]
        public DateTime? EndDate { get; set; }

        // Etkinlik konumu / adresi
        [Required(ErrorMessage = "Konum bilgisi zorunludur.")]
        [StringLength(300, ErrorMessage = "Konum en fazla 300 karakter olmalidir.")]
        [Display(Name = "Konum")]
        public string Location { get; set; } = string.Empty;

        // Online etkinlik icin URL (opsiyonel)
        [Url(ErrorMessage = "Gecerli bir URL giriniz.")]
        [Display(Name = "Online Baglanti")]
        public string? OnlineUrl { get; set; }

        // Maksimum katilimci sayisi - 0 ise sinir yok
        [Range(0, 10000, ErrorMessage = "Kontenjan 0 ile 10000 arasinda olmalidir.")]
        [Display(Name = "Maksimum Katilimci")]
        public int MaxCapacity { get; set; } = 0;

        // Etkinlik kategorisi (Workshop, Konferans, Seminer vb.)
        [Required(ErrorMessage = "Kategori secimi zorunludur.")]
        [Display(Name = "Kategori")]
        public EventCategory Category { get; set; }

        // Etkinlik durumu (Yayin, Taslak, Iptal, Tamamlandi)
        [Display(Name = "Durum")]
        public EventStatus Status { get; set; } = EventStatus.Draft;

        // Etkinlik kapak gorseli URL'i
        [Display(Name = "Kapak Gorseli")]
        public string? CoverImageUrl { get; set; }

        // Ucretli mi, ucretsiz mi?
        [Display(Name = "Ucretli Etkinlik")]
        public bool IsPaid { get; set; } = false;

        // Ucret miktari (ucretli ise)
        [Range(0, 99999, ErrorMessage = "Ucret gecerli bir deger olmalidir.")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Ucret (TL)")]
        public decimal Price { get; set; } = 0;

        // Kayit olusturulma tarihi
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Son guncelleme tarihi
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Yabanci anahtar: Etkinligi olusturan kullanici
        public string CreatedByUserId { get; set; } = string.Empty;

        // Navigasyon: Etkinligi olusturan kullanici nesnesi
        [ForeignKey("CreatedByUserId")]
        public ApplicationUser? CreatedBy { get; set; }

        // Navigasyon: Bu etkinlige yapilan kayitlar koleksiyonu
        public ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();

        // Hesaplanan ozellik: Mevcut katilimci sayisi
        [NotMapped]
        public int CurrentParticipantCount => Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);

        // Hesaplanan ozellik: Kontenjan dolu mu?
        [NotMapped]
        public bool IsFullyBooked => MaxCapacity > 0 && CurrentParticipantCount >= MaxCapacity;

        // Hesaplanan ozellik: Kalan kontenjan sayisi
        [NotMapped]
        public int RemainingCapacity => MaxCapacity == 0 ? int.MaxValue : MaxCapacity - CurrentParticipantCount;
    }

    /// <summary>
    /// Etkinlik kategorileri enumerasyonu
    /// </summary>
    public enum EventCategory
    {
        [Display(Name = "Workshop")]
        Workshop = 1,

        [Display(Name = "Konferans")]
        Conference = 2,

        [Display(Name = "Seminer")]
        Seminar = 3,

        [Display(Name = "Networking")]
        Networking = 4,

        [Display(Name = "Hackathon")]
        Hackathon = 5,

        [Display(Name = "Egitim")]
        Training = 6,

        [Display(Name = "Diger")]
        Other = 7,

        [Display(Name = "Konser")]
        Concert = 8,

        [Display(Name = "Sinema")]
        Cinema = 9,

        [Display(Name = "Tiyatro")]
        Theater = 10,

        [Display(Name = "Spor")]
        Sports = 11,

        [Display(Name = "Festival")]
        Festival = 12,

        [Display(Name = "Sergi")]
        Exhibition = 13,

        [Display(Name = "Seminer")]
        SeminarSeries = 14
    }

    /// <summary>
    /// Etkinlik durum enumerasyonu
    /// </summary>
    public enum EventStatus
    {
        [Display(Name = "Taslak")]
        Draft = 0,

        [Display(Name = "Yayinda")]
        Published = 1,

        [Display(Name = "Iptal Edildi")]
        Cancelled = 2,

        [Display(Name = "Tamamlandi")]
        Completed = 3
    }
}
