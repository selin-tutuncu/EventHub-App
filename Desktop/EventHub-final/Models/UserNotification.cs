using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventHub.Models
{
    /// <summary>
    /// Kullanici icin uygulama ici bildirim kaydi.
    /// Bekleme listesinden cikan davetler ve benzeri mesajlar burada tutulur.
    /// </summary>
    public class UserNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int? EventId { get; set; }

        public int? RegistrationId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.WaitlistPromotion;

        public bool RequiresAction { get; set; } = true;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(EventId))]
        public Event? Event { get; set; }

        [ForeignKey(nameof(RegistrationId))]
        public EventRegistration? Registration { get; set; }
    }

    public enum NotificationType
    {
        WaitlistPromotion = 1
    }
}
