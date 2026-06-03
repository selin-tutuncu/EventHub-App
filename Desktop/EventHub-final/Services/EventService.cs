using EventHub.Data;
using EventHub.Models;
using EventHub.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace EventHub.Services
{
    /// <summary>
    /// Etkinlik islemlerine ait is mantigi.
    /// Controller'lari sadelestirir ve veritabani islemlerini soyutlar.
    /// </summary>
    public class EventService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EventService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<EventListItemViewModel>> GetPublishedEventsAsync(
            string? searchTerm = null,
            EventCategory? category = null,
            string? userId = null)
        {
            var query = _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.Registrations)
                .Where(e => e.Status == EventStatus.Published)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(e =>
                    e.Title.ToLower().Contains(term) ||
                    (e.Summary != null && e.Summary.ToLower().Contains(term)) ||
                    e.Location.ToLower().Contains(term));
            }

            if (category.HasValue)
            {
                query = query.Where(e => e.Category == category.Value);
            }

            var events = await query
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return events.Select(e => MapToListItemViewModel(e, userId)).ToList();
        }

        public async Task<EventDetailViewModel?> GetEventDetailAsync(int eventId, string? userId = null, bool isAdmin = false)
        {
            var ev = await _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.Registrations)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null) return null;

            EventRegistration? userRegistration = null;
            if (!string.IsNullOrEmpty(userId))
            {
                userRegistration = ev.Registrations.FirstOrDefault(r =>
                    r.UserId == userId && r.Status != RegistrationStatus.Cancelled);
            }

            var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
            var isOfflineEvent = string.IsNullOrWhiteSpace(ev.OnlineUrl);
            var checkInUrl = userRegistration?.CheckInCode != null
                ? BuildCheckInUrl(userRegistration.CheckInCode)
                : null;

            var viewModel = new EventDetailViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                Summary = ev.Summary,
                EventDate = ev.EventDate,
                EndDate = ev.EndDate,
                Location = ev.Location,
                OnlineUrl = ev.OnlineUrl,
                CanViewOnlineUrl = isAdmin || userRegistration?.Status == RegistrationStatus.Confirmed,
                MaxCapacity = ev.MaxCapacity,
                CurrentParticipantCount = confirmedCount,
                RemainingCapacity = ev.MaxCapacity == 0 ? int.MaxValue : ev.MaxCapacity - confirmedCount,
                IsFullyBooked = ev.MaxCapacity > 0 && confirmedCount >= ev.MaxCapacity,
                Category = ev.Category,
                Status = ev.Status,
                IsPaid = ev.IsPaid,
                Price = ev.Price,
                CoverImageUrl = ev.CoverImageUrl,
                OrganizerName = ev.CreatedBy?.FullName ?? "Bilinmiyor",
                OrganizerEmail = ev.CreatedBy?.Email ?? "",
                OrganizerBio = ev.CreatedBy?.Bio,
                CreatedAt = ev.CreatedAt,
                IsRegistered = userRegistration != null,
                RegistrationStatus = userRegistration?.Status,
                RegistrationId = userRegistration?.Id,
                CheckInCode = userRegistration?.CheckInCode,
                CheckInUrl = checkInUrl,
                CheckInQrCodeBase64 = isOfflineEvent && userRegistration?.Status == RegistrationStatus.Confirmed && !string.IsNullOrWhiteSpace(checkInUrl)
                    ? BuildQrCodeBase64(checkInUrl)
                    : null,
                IsOfflineEvent = isOfflineEvent,
                AttendanceConfirmed = userRegistration?.AttendanceConfirmed ?? false,
                PaymentFullName = userRegistration?.PaymentFullName,
                PaidAmount = userRegistration?.PaidAmount,
                PaymentCompletedAt = userRegistration?.PaymentCompletedAt,
                PaymentReference = userRegistration?.PaymentReference,
                RefundRequestedAt = userRegistration?.RefundRequestedAt
            };

            if (isAdmin || ev.CreatedByUserId == userId)
            {
                viewModel.Participants = ev.Registrations
                    .Where(r => r.Status != RegistrationStatus.Cancelled)
                    .Select(r => new ParticipantViewModel
                    {
                        RegistrationId = r.Id,
                        UserId = r.UserId,
                        FullName = r.User?.FullName ?? "Bilinmiyor",
                        Email = r.User?.Email ?? "",
                        RegisteredAt = r.RegisteredAt,
                        Status = r.Status,
                        AttendanceConfirmed = r.AttendanceConfirmed,
                        Notes = r.Notes
                    })
                    .OrderBy(p => p.RegisteredAt)
                    .ToList();
            }

            return viewModel;
        }

        public async Task<Event> CreateEventAsync(EventFormViewModel model, string createdByUserId)
        {
            var newEvent = new Event
            {
                Title = model.Title,
                Description = model.Description,
                Summary = model.Summary,
                EventDate = model.EventDate,
                EndDate = model.EndDate,
                Location = model.Location,
                OnlineUrl = model.OnlineUrl,
                MaxCapacity = model.MaxCapacity,
                Category = model.Category,
                Status = model.Status,
                CoverImageUrl = model.CoverImageUrl,
                IsPaid = model.IsPaid,
                Price = model.IsPaid ? model.Price : 0,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent;
        }

        public async Task<bool> UpdateEventAsync(EventFormViewModel model)
        {
            var existingEvent = await _context.Events.FindAsync(model.Id);
            if (existingEvent == null) return false;

            var previousCapacity = existingEvent.MaxCapacity;

            existingEvent.Title = model.Title;
            existingEvent.Description = model.Description;
            existingEvent.Summary = model.Summary;
            existingEvent.EventDate = model.EventDate;
            existingEvent.EndDate = model.EndDate;
            existingEvent.Location = model.Location;
            existingEvent.OnlineUrl = model.OnlineUrl;
            existingEvent.MaxCapacity = model.MaxCapacity;
            existingEvent.Category = model.Category;
            existingEvent.Status = model.Status;
            existingEvent.CoverImageUrl = model.CoverImageUrl;
            existingEvent.IsPaid = model.IsPaid;
            existingEvent.Price = model.IsPaid ? model.Price : 0;
            existingEvent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (model.MaxCapacity > previousCapacity)
            {
                await EnsureWaitlistNotificationsAsync(existingEvent.Id, "kontenjan artırıldı");
            }

            return true;
        }

        public async Task<EventPaymentViewModel?> GetPaymentViewModelAsync(int eventId, string userId, string? notes = null)
        {
            var ev = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null || !ev.IsPaid)
                return null;

            if (ev.Status != EventStatus.Published || ev.EventDate < DateTime.UtcNow)
                return null;

            var existingRegistration = ev.Registrations.FirstOrDefault(r =>
                r.UserId == userId && r.Status != RegistrationStatus.Cancelled);
            if (existingRegistration != null)
                return null;

            var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
            if (ev.MaxCapacity > 0 && confirmedCount >= ev.MaxCapacity)
                return null;

            return new EventPaymentViewModel
            {
                EventId = ev.Id,
                EventTitle = ev.Title,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Price = ev.Price,
                Notes = notes
            };
        }

        public async Task<(bool success, string message)> RegisterUserAsync(
            int eventId,
            string userId,
            string? notes = null,
            string? paymentFullName = null)
        {
            var ev = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return (false, "Etkinlik bulunamadi.");

            if (ev.Status != EventStatus.Published)
                return (false, "Bu etkinlik su anda kayit kabul etmemektedir.");

            if (ev.EventDate < DateTime.UtcNow)
                return (false, "Bu etkinlik gerceklesti, artik kayit yapilamaz.");

            if (ev.IsPaid && string.IsNullOrWhiteSpace(paymentFullName))
                return (false, "Ucretli etkinliklerde odeme icin Ad Soyad bilgisi zorunludur.");

            var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
            var paymentReference = ev.IsPaid ? $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}" : null;

            var existingRegistration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (existingRegistration != null)
            {
                if (existingRegistration.Status == RegistrationStatus.Cancelled)
                {
                    if (ev.MaxCapacity > 0 && confirmedCount >= ev.MaxCapacity)
                        return (false, "Etkinlik kontenjani dolu. Bekleme listesine katilabilirsiniz.");

                    existingRegistration.Status = RegistrationStatus.Confirmed;
                    existingRegistration.Notes = notes;
                    existingRegistration.CheckInCode = ev.IsOnlineEvent() ? null : Guid.NewGuid().ToString("N");
                    existingRegistration.CheckedInAt = null;
                    existingRegistration.RegisteredAt = DateTime.UtcNow;
                    existingRegistration.PaymentFullName = ev.IsPaid ? paymentFullName?.Trim() : null;
                    existingRegistration.PaidAmount = ev.IsPaid ? ev.Price : null;
                    existingRegistration.PaymentCompletedAt = ev.IsPaid ? DateTime.UtcNow : null;
                    existingRegistration.PaymentReference = paymentReference;
                    existingRegistration.RefundRequestedAt = null;
                    existingRegistration.RefundNote = null;
                    await _context.SaveChangesAsync();
                    return (true, "Etkinlige basariyla tekrar kayit oldunuz.");
                }

                if (existingRegistration.Status == RegistrationStatus.Pending)
                    return (false, "Bu etkinlikte zaten bekleme listesindesiniz.");

                return (false, "Bu etkinlige zaten kayitlisiniz.");
            }

            if (ev.MaxCapacity > 0 && confirmedCount >= ev.MaxCapacity)
                return (false, "Etkinlik kontenjan doldu. Bekleme listesine katilabilirsiniz.");

            var registration = new EventRegistration
            {
                EventId = eventId,
                UserId = userId,
                Status = RegistrationStatus.Confirmed,
                Notes = notes,
                RegisteredAt = DateTime.UtcNow,
                CheckInCode = ev.IsOnlineEvent() ? null : Guid.NewGuid().ToString("N"),
                PaymentFullName = ev.IsPaid ? paymentFullName?.Trim() : null,
                PaidAmount = ev.IsPaid ? ev.Price : null,
                PaymentCompletedAt = ev.IsPaid ? DateTime.UtcNow : null,
                PaymentReference = paymentReference
            };

            _context.EventRegistrations.Add(registration);
            await _context.SaveChangesAsync();
            return (true, "Etkinlige basariyla kayit oldunuz.");
        }

        public async Task<EventRefundViewModel?> GetRefundViewModelAsync(int registrationId, string userId)
        {
            var registration = await _context.EventRegistrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == userId);

            if (registration == null || registration.Event == null)
                return null;

            if (registration.Status == RegistrationStatus.Cancelled || registration.Event.EventDate < DateTime.UtcNow)
                return null;

            return new EventRefundViewModel
            {
                RegistrationId = registration.Id,
                EventId = registration.EventId,
                EventTitle = registration.Event.Title,
                EventDate = registration.Event.EventDate,
                RefundAmount = registration.Event.IsPaid ? (registration.PaidAmount ?? registration.Event.Price) : 0,
                PaymentFullName = registration.PaymentFullName,
                PaymentReference = registration.PaymentReference
            };
        }

        public async Task<(bool success, string message)> JoinWaitlistAsync(int eventId, string userId, string? notes = null)
        {
            var ev = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return (false, "Etkinlik bulunamadi.");

            if (ev.Status != EventStatus.Published)
                return (false, "Bu etkinlik su anda bekleme listesi kabul etmiyor.");

            if (ev.EventDate < DateTime.UtcNow)
                return (false, "Bu etkinlik gerceklesti, bekleme listesi kullanilamaz.");

            var existingRegistration = await _context.EventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);

            if (existingRegistration != null)
            {
                if (existingRegistration.Status == RegistrationStatus.Pending)
                    return (false, "Bu etkinlikte zaten bekleme listesindesiniz.");

                if (existingRegistration.Status == RegistrationStatus.Confirmed)
                    return (false, "Bu etkinlige zaten kayitlisiniz.");

                existingRegistration.Status = RegistrationStatus.Pending;
                existingRegistration.Notes = notes;
                existingRegistration.CheckInCode = null;
                existingRegistration.CheckedInAt = null;
                existingRegistration.RegisteredAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return (true, "Bekleme listesi kaydiniz guncellendi.");
            }

            var waitlist = new EventRegistration
            {
                EventId = eventId,
                UserId = userId,
                Status = RegistrationStatus.Pending,
                Notes = notes,
                RegisteredAt = DateTime.UtcNow
            };

            _context.EventRegistrations.Add(waitlist);
            await _context.SaveChangesAsync();
            return (true, "Bekleme listesine eklendiniz.");
        }

        public async Task<(bool success, string message)> CancelRegistrationAsync(int registrationId, string userId, string? refundNote = null)
        {
            var registration = await _context.EventRegistrations
                .Include(r => r.Event)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == userId);

            if (registration == null)
                return (false, "Kayit bulunamadi.");

            if (registration.Event?.EventDate < DateTime.UtcNow)
                return (false, "Gerceklesmis etkinlik kaydi iptal edilemez.");

            registration.Status = RegistrationStatus.Cancelled;
            registration.CheckInCode = null;
            registration.CheckedInAt = null;
            if (registration.Event?.IsPaid == true)
            {
                registration.RefundRequestedAt = DateTime.UtcNow;
                registration.RefundNote = refundNote;
            }
            await _context.SaveChangesAsync();

            if (registration.EventId > 0)
            {
                await EnsureWaitlistNotificationsAsync(registration.EventId, "başka bir katılımcı kaydını iptal etti");
            }

            return (true, "Kaydiniz basariyla iptal edildi.");
        }

        public async Task<(bool success, string message, int? eventId)> RespondToWaitlistInvitationAsync(
            int notificationId,
            string userId,
            bool accept)
        {
            var notification = await _context.UserNotifications
                .Include(n => n.Event)
                    .ThenInclude(e => e!.Registrations)
                .Include(n => n.Registration)
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null || notification.Event == null || notification.Registration == null)
                return (false, "Bildirim bulunamadi.", null);

            if (notification.Type != NotificationType.WaitlistPromotion)
                return (false, "Bu bildirim icin bu islem desteklenmiyor.", notification.EventId);

            if (notification.IsRead && notification.Registration.Status != RegistrationStatus.Pending)
                return (false, "Bu bildirim zaten islenmis.", notification.EventId);

            var ev = notification.Event;
            var registration = notification.Registration;
            var now = DateTime.UtcNow;

            if (accept)
            {
                var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
                if (ev.MaxCapacity > 0 && confirmedCount >= ev.MaxCapacity)
                    return (false, "Etkinlik kontenjani dolmus.", ev.Id);

                registration.Status = RegistrationStatus.Confirmed;
                registration.RegisteredAt = now;
                registration.CheckInCode = ev.IsOnlineEvent() ? null : Guid.NewGuid().ToString("N");
                registration.CheckedInAt = null;
                notification.IsRead = true;
                notification.ReadAt = now;

                await _context.SaveChangesAsync();
                await EnsureWaitlistNotificationsAsync(ev.Id, "bir yer yeniden açıldı");
                return (true, "Tebrikler, bekleme listesinden cikis kaydiniz onaylandi.", ev.Id);
            }

            registration.Status = RegistrationStatus.Cancelled;
            registration.CheckInCode = null;
            registration.CheckedInAt = null;
            notification.IsRead = true;
            notification.ReadAt = now;

            await _context.SaveChangesAsync();
            await EnsureWaitlistNotificationsAsync(ev.Id, "bekleme daveti reddedildi");
            return (true, "Bekleme listesi daveti reddedildi ve kaydiniz silindi.", ev.Id);
        }

        public async Task<List<EventListItemViewModel>> GetUserRegistrationsAsync(string userId)
        {
            var registrations = await _context.EventRegistrations
                .Include(r => r.Event)
                    .ThenInclude(e => e!.CreatedBy)
                .Include(r => r.Event)
                    .ThenInclude(e => e!.Registrations)
                .Where(r => r.UserId == userId && r.Status != RegistrationStatus.Cancelled)
                .OrderBy(r => r.Status == RegistrationStatus.Pending ? 0 : 1)
                .ThenBy(r => r.Event!.EventDate)
                .ToListAsync();

            return registrations
                .Where(r => r.Event != null)
                .Select(r => MapToListItemViewModel(r.Event!, userId, r))
                .ToList();
        }

        public async Task<List<EventListItemViewModel>> GetAllEventsForAdminAsync()
        {
            var events = await _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.Registrations)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            return events.Select(e => MapToListItemViewModel(e)).ToList();
        }

        public async Task<(bool success, string message)> CheckInByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Gecersiz check-in kodu.");

            var registration = await _context.EventRegistrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.CheckInCode == code && r.Status == RegistrationStatus.Confirmed);

            if (registration == null || registration.Event == null)
                return (false, "Check-in kaydi bulunamadi.");

            if (registration.Event.IsOnlineEvent())
                return (false, "Bu check-in kodu fiziksel etkinlikler icin kullanilir.");

            if (registration.AttendanceConfirmed)
                return (true, "Katilim zaten onaylanmis.");

            registration.AttendanceConfirmed = true;
            registration.CheckedInAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return (true, "Katilim basariyla onaylandi.");
        }

        private async Task EnsureWaitlistNotificationsAsync(int eventId, string reasonText)
        {
            var ev = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null || ev.MaxCapacity == 0)
                return;

            var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
            var openSeats = ev.MaxCapacity - confirmedCount;
            if (openSeats <= 0)
                return;

            var activeNotificationCount = await _context.UserNotifications.CountAsync(n =>
                n.EventId == eventId &&
                n.Type == NotificationType.WaitlistPromotion &&
                !n.IsRead &&
                n.RequiresAction);

            var remainingSlots = openSeats - activeNotificationCount;
            if (remainingSlots <= 0)
                return;

            var pendingRegistrations = await _context.EventRegistrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.Pending)
                .OrderBy(r => r.RegisteredAt)
                .ToListAsync();

            foreach (var reg in pendingRegistrations)
            {
                if (remainingSlots <= 0)
                    break;

                var hasOpenNotification = await _context.UserNotifications.AnyAsync(n =>
                    n.EventId == eventId &&
                    n.RegistrationId == reg.Id &&
                    n.Type == NotificationType.WaitlistPromotion &&
                    !n.IsRead &&
                    n.RequiresAction);

                if (hasOpenNotification)
                    continue;

                _context.UserNotifications.Add(new UserNotification
                {
                    UserId = reg.UserId,
                    EventId = eventId,
                    RegistrationId = reg.Id,
                    Type = NotificationType.WaitlistPromotion,
                    RequiresAction = true,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Title = "Bekleme listesinden cikis firsati",
                    Message = $"{ev.Title} etkinliginde yer acildi. {reasonText}. Yer acilmasi nedeniyle waitlistten cikarilabilirsiniz. Bu etkinlik icin kaydinizi onayliyor musunuz?"
                });

                remainingSlots--;
            }

            await _context.SaveChangesAsync();
        }

        private EventListItemViewModel MapToListItemViewModel(Event ev, string? userId = null, EventRegistration? userRegistration = null)
        {
            var confirmedCount = ev.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);

            if (userRegistration == null && !string.IsNullOrEmpty(userId))
            {
                userRegistration = ev.Registrations.FirstOrDefault(r =>
                    r.UserId == userId && r.Status != RegistrationStatus.Cancelled);
            }

            var checkInCode = userRegistration?.CheckInCode;
            var checkInUrl = !string.IsNullOrWhiteSpace(checkInCode) ? BuildCheckInUrl(checkInCode) : null;

            return new EventListItemViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Summary = ev.Summary,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Category = ev.Category,
                Status = ev.Status,
                MaxCapacity = ev.MaxCapacity,
                CurrentParticipantCount = confirmedCount,
                IsFullyBooked = ev.MaxCapacity > 0 && confirmedCount >= ev.MaxCapacity,
                IsPaid = ev.IsPaid,
                Price = ev.Price,
                CoverImageUrl = ev.CoverImageUrl,
                OrganizerName = ev.CreatedBy?.FullName ?? "Bilinmiyor",
                IsRegistered = userRegistration?.Status == RegistrationStatus.Confirmed,
                UserRegistrationStatus = userRegistration?.Status,
                RegistrationId = userRegistration?.Id,
                CheckInCode = checkInCode,
                CheckInUrl = checkInUrl,
                CheckInQrCodeBase64 = userRegistration?.Status == RegistrationStatus.Confirmed && !ev.IsOnlineEvent() && !string.IsNullOrWhiteSpace(checkInUrl)
                    ? BuildQrCodeBase64(checkInUrl)
                    : null
            };
        }

        private string BuildCheckInUrl(string code)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return $"/Event/CheckIn?code={Uri.EscapeDataString(code)}";

            return $"{request.Scheme}://{request.Host}/Event/CheckIn?code={Uri.EscapeDataString(code)}";
        }

        private static string BuildQrCodeBase64(string content)
        {
            using var generator = new QRCodeGenerator();
            using var qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(qrData);
            var bytes = png.GetGraphic(18);
            return Convert.ToBase64String(bytes);
        }
    }

    internal static class EventExtensions
    {
        public static bool IsOnlineEvent(this Event ev) => !string.IsNullOrWhiteSpace(ev.OnlineUrl);
    }
}
