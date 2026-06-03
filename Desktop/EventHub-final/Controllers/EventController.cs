using EventHub.Models;
using EventHub.Services;
using EventHub.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Controllers
{
    /// <summary>
    /// Etkinlik listeleme, detay, olusturma, duzenleme ve silme islemlerini yoneten controller.
    /// Ayrica kullanici kayit/iptal islemlerini de kapsar.
    /// </summary>
    public class EventController : Controller
    {
        private readonly EventService _eventService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public EventController(
            EventService eventService,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _eventService = eventService;
            _userManager = userManager;
            _environment = environment;
        }

        // --- YARDIMCI METOT: TURKIYE SAATINI ALMAK ICIN ---
        private DateTime GetTurkeyTime()
        {
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTimeZone);
        }

        // ----------------------------------------------------------------
        // LISTELEME VE DETAY
        // ----------------------------------------------------------------

        /// <summary>
        /// Tum yayindaki etkinlikleri listeler. Arama ve kategori filtresi destekler.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search, EventCategory? category)
        {
            // Aktif kullanici ID'si kayit durumu gostermek icin
            var userId = _userManager.GetUserId(User);

            var events = await _eventService.GetPublishedEventsAsync(search, category, userId);

            // Filtre bilgilerini ViewBag ile view'e aktar
            ViewBag.SearchTerm = search;
            ViewBag.SelectedCategory = category;
            ViewBag.Categories = Enum.GetValues<EventCategory>();

            return View(events);
        }

        /// <summary>
        /// Etkinlik detay sayfasini gosterir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");

            var viewModel = await _eventService.GetEventDetailAsync(id, userId, isAdmin);

            if (viewModel == null)
            {
                TempData["Error"] = "Etkinlik bulunamadi.";
                return RedirectToAction("Index");
            }

            // Yayinda olmayan etkinliklere yalnizca admin erisebilir
            if (viewModel.Status != EventStatus.Published && !isAdmin)
            {
                TempData["Error"] = "Bu etkinlik su anda goruntulenemez.";
                return RedirectToAction("Index");
            }

            return View(viewModel);
        }

        // ----------------------------------------------------------------
        // ETKINLIK OLUSTURMA (ADMIN)
        // ----------------------------------------------------------------

        /// <summary>
        /// Etkinlik olusturma formunu gosterir - Yalnizca Admin
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View(new EventFormViewModel());
        }

        /// <summary>
        /// Etkinlik olusturma formunu isler - Yalnizca Admin
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            // 1. TARIH KONTROLU: Baslangic tarihi gecmis olamaz
            if (model.EventDate < GetTurkeyTime())
            {
                ModelState.AddModelError("EventDate", "Etkinlik baslangic tarihi gecmis bir zaman olamaz.");
            }

            // 2. TARIH KONTROLU: Bitis tarihi baslangictan once olamaz
            if (model.EndDate.HasValue && model.EndDate < model.EventDate)
            {
                ModelState.AddModelError("EndDate", "Bitis tarihi baslangic tarihinden once olamaz.");
            }

            if (!ModelState.IsValid)
                return View(model);

            model.CoverImageUrl = await ResolveCoverImageAsync(model.CoverImageFile, null, model.CoverImageUrl);

            var userId = _userManager.GetUserId(User)!;
            var createdEvent = await _eventService.CreateEventAsync(model, userId);

            TempData["Success"] = $"'{createdEvent.Title}' etkinligi basariyla olusturuldu.";
            return RedirectToAction("Detail", new { id = createdEvent.Id });
        }

        // ----------------------------------------------------------------
        // ETKINLIK DUZENLEME (ADMIN)
        // ----------------------------------------------------------------

        /// <summary>
        /// Etkinlik duzenleme formunu gosterir - Yalnizca Admin
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _eventService.GetEventDetailAsync(id, isAdmin: true);
            if (ev == null)
            {
                TempData["Error"] = "Etkinlik bulunamadi.";
                return RedirectToAction("Index", "Admin");
            }

            // Detail ViewModel'i Form ViewModel'e donustur
            var formModel = new EventFormViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                Summary = ev.Summary,
                EventDate = ev.EventDate,
                EndDate = ev.EndDate,
                Location = ev.Location,
                OnlineUrl = ev.OnlineUrl,
                MaxCapacity = ev.MaxCapacity,
                Category = ev.Category,
                Status = ev.Status,
                CoverImageUrl = ev.CoverImageUrl,
                IsPaid = ev.IsPaid,
                Price = ev.Price
            };

            return View(formModel);
        }

        /// <summary>
        /// Etkinlik duzenleme formunu isler - Yalnizca Admin
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(EventFormViewModel model)
        {
            // Edit isleminde sadece Bitis tarihinin Baslangictan kucuk olup olmadigini kontrol ediyoruz. 
            // Gecmiste baslamis bir etkinlik guncelleniyor olabilir.
            if (model.EndDate.HasValue && model.EndDate < model.EventDate)
            {
                ModelState.AddModelError("EndDate", "Bitis tarihi baslangic tarihinden once olamaz.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _eventService.GetEventDetailAsync(model.Id, isAdmin: true);
            if (existing == null)
            {
                TempData["Error"] = "Etkinlik bulunamadi.";
                return RedirectToAction("Events", "Admin");
            }

            model.CoverImageUrl = await ResolveCoverImageAsync(
                model.CoverImageFile,
                existing.CoverImageUrl,
                model.CoverImageUrl);

            var updated = await _eventService.UpdateEventAsync(model);
            if (!updated)
            {
                TempData["Error"] = "Etkinlik guncellenirken bir hata olustu.";
                return View(model);
            }

            TempData["Success"] = "Etkinlik basariyla guncellendi.";
            return RedirectToAction("Detail", new { id = model.Id });
        }

        // ----------------------------------------------------------------
        // KULLANICI KAYIT / IPTAL ISLEMLERI
        // ----------------------------------------------------------------

        /// <summary>
        /// Kullanicinin bir etkinlige kayit olmasini saglar
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Register(int eventId, string? notes)
        {
            var userId = _userManager.GetUserId(User)!;
            var paymentModel = await _eventService.GetPaymentViewModelAsync(eventId, userId, notes);
            if (paymentModel != null)
                return View("Payment", paymentModel);

            var (success, message) = await _eventService.RegisterUserAsync(eventId, userId, notes);

            if (success)
                TempData["Success"] = message;
            else
                TempData["Error"] = message;

            return RedirectToAction("Detail", new { id = eventId });
        }

        [HttpGet]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> Payment(int eventId)
        {
            var userId = _userManager.GetUserId(User)!;
            var model = await _eventService.GetPaymentViewModelAsync(eventId, userId);
            if (model == null)
            {
                TempData["Error"] = "Odeme ekrani bu etkinlik icin kullanilamaz.";
                return RedirectToAction("Detail", new { id = eventId });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> CompletePayment(EventPaymentViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;
            var paymentModel = await _eventService.GetPaymentViewModelAsync(model.EventId, userId, model.Notes);
            if (paymentModel == null)
            {
                TempData["Error"] = "Odeme yapilabilecek etkinlik bulunamadi.";
                return RedirectToAction("Detail", new { id = model.EventId });
            }

            if (!ModelState.IsValid)
            {
                model.EventTitle = paymentModel.EventTitle;
                model.EventDate = paymentModel.EventDate;
                model.Location = paymentModel.Location;
                model.Price = paymentModel.Price;
                return View("Payment", model);
            }

            var (success, message) = await _eventService.RegisterUserAsync(
                model.EventId,
                userId,
                model.Notes,
                model.PaymentFullName);

            TempData[success ? "Success" : "Error"] = success
                ? $"{message} Odeme onayi simule edildi."
                : message;

            return RedirectToAction("Detail", new { id = model.EventId });
        }

        /// <summary>
        /// Kullaniciyi bekleme listesine ekler.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User,Admin")]
        public async Task<IActionResult> JoinWaitlist(int eventId, string? notes)
        {
            var userId = _userManager.GetUserId(User)!;
            var (success, message) = await _eventService.JoinWaitlistAsync(eventId, userId, notes);

            if (success)
                TempData["Success"] = message;
            else
                TempData["Error"] = message;

            return RedirectToAction("Detail", new { id = eventId });
        }

        /// <summary>
        /// Kullanicinin etkinlik kaydini iptal eder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelRegistration(int registrationId, int eventId)
        {
            var userId = _userManager.GetUserId(User)!;
            var refundModel = await _eventService.GetRefundViewModelAsync(registrationId, userId);
            if (refundModel != null && refundModel.RefundAmount > 0)
                return View("Refund", refundModel);

            var (success, message) = await _eventService.CancelRegistrationAsync(registrationId, userId);

            if (success)
                TempData["Success"] = message;
            else
                TempData["Error"] = message;

            return RedirectToAction("Detail", new { id = eventId });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Refund(int registrationId)
        {
            var userId = _userManager.GetUserId(User)!;
            var model = await _eventService.GetRefundViewModelAsync(registrationId, userId);
            if (model == null)
            {
                TempData["Error"] = "Iade ekrani bu kayit icin kullanilamaz.";
                return RedirectToAction("MyEvents");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ConfirmRefund(EventRefundViewModel model)
        {
            var userId = _userManager.GetUserId(User)!;
            var refundModel = await _eventService.GetRefundViewModelAsync(model.RegistrationId, userId);
            if (refundModel == null)
            {
                TempData["Error"] = "Iade yapilabilecek kayit bulunamadi.";
                return RedirectToAction("MyEvents");
            }

            if (!ModelState.IsValid)
            {
                model.EventTitle = refundModel.EventTitle;
                model.EventDate = refundModel.EventDate;
                model.RefundAmount = refundModel.RefundAmount;
                model.PaymentFullName = refundModel.PaymentFullName;
                model.PaymentReference = refundModel.PaymentReference;
                return View("Refund", model);
            }

            var (success, message) = await _eventService.CancelRegistrationAsync(
                model.RegistrationId,
                userId,
                model.RefundNote);

            TempData[success ? "Success" : "Error"] = success
                ? $"{message} {refundModel.RefundAmount:N0} TL iade talebi olusturuldu."
                : message;

            return RedirectToAction("Detail", new { id = refundModel.EventId });
        }

        /// <summary>
        /// Kullanicinin katildigi etkinliklerin listesi
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MyEvents()
        {
            var userId = _userManager.GetUserId(User)!;
            var events = await _eventService.GetUserRegistrationsAsync(userId);
            return View(events);
        }

        /// <summary>
        /// QR kod ile check-in yapar.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CheckIn(string code)
        {
            var (success, message) = await _eventService.CheckInByCodeAsync(code);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction("Index", "Home");
        }

        private async Task<string?> ResolveCoverImageAsync(IFormFile? file, string? currentImageUrl, string? fallbackUrl)
        {
            var urlCandidate = string.IsNullOrWhiteSpace(fallbackUrl) ? currentImageUrl : fallbackUrl;

            if (file == null || file.Length == 0)
                return urlCandidate;

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "uploads", "event-images");
            Directory.CreateDirectory(uploadsRoot);

            if (!string.IsNullOrWhiteSpace(urlCandidate) &&
                urlCandidate.StartsWith("/uploads/event-images/", StringComparison.OrdinalIgnoreCase))
            {
                var oldRelativePath = urlCandidate.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var oldAbsolutePath = Path.Combine(webRoot, oldRelativePath);
                if (System.IO.File.Exists(oldAbsolutePath))
                    System.IO.File.Delete(oldAbsolutePath);
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/event-images/{fileName}";
        }
    }
}