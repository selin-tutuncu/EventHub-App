using EventHub.Data;
using EventHub.Models;
using EventHub.Services;
using EventHub.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    /// <summary>
    /// Ana sayfa ve genel navigasyon islemlerini yoneten controller.
    /// Tum kullanicilara acik sayfalar burada bulunur.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Ana sayfa: Onsoz etkinlikler ve istatistikler gosterilir
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Yayindaki yaklasan etkinlikleri getir
            var featuredEvents = await _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.Registrations)
                .Where(e => e.Status == EventStatus.Published && e.EventDate >= DateTime.UtcNow)
                .OrderBy(e => e.EventDate)
                .Take(6)
                .ToListAsync();

            // Giris yapan kullanici ID'si (kayit durumu gostermek icin)
            var userId = _userManager.GetUserId(User);

            // Genel istatistikler
            var totalEvents = await _context.Events.CountAsync(e => e.Status == EventStatus.Published);
            var totalUsers = await _userManager.Users.CountAsync();
            var totalRegistrations = await _context.EventRegistrations
                .CountAsync(r => r.Status == RegistrationStatus.Confirmed);
            var upcomingCount = await _context.Events
                .CountAsync(e => e.Status == EventStatus.Published && e.EventDate >= DateTime.UtcNow);

            var viewModel = new HomeViewModel
            {
                FeaturedEvents = featuredEvents.Select(e =>
                {
                    var confirmed = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
                    return new EventListItemViewModel
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Summary = e.Summary,
                        EventDate = e.EventDate,
                        Location = e.Location,
                        Category = e.Category,
                        Status = e.Status,
                        MaxCapacity = e.MaxCapacity,
                        CurrentParticipantCount = confirmed,
                        IsFullyBooked = e.MaxCapacity > 0 && confirmed >= e.MaxCapacity,
                        IsPaid = e.IsPaid,
                        Price = e.Price,
                        CoverImageUrl = e.CoverImageUrl,
                        OrganizerName = e.CreatedBy?.FullName ?? "Bilinmiyor",
                        IsRegistered = userId != null && e.Registrations.Any(r =>
                            r.UserId == userId && r.Status == RegistrationStatus.Confirmed)
                    };
                }).ToList(),
                TotalEvents = totalEvents,
                TotalUsers = totalUsers,
                TotalRegistrations = totalRegistrations,
                UpcomingEventsCount = upcomingCount
            };

            return View("~/Views/Home/Index.cshtml", viewModel);
        }

        /// <summary>
        /// Hakkimizda sayfasi
        /// </summary>
        public IActionResult About()
        {
            return View("~/Views/Home/About.cshtml");
        }

        /// <summary>
        /// Iletisim sayfasi
        /// </summary>
        public IActionResult Contact()
        {
            return View("~/Views/Home/Contact.cshtml");
        }

        /// <summary>
        /// Gizlilik politikasi sayfasi
        /// </summary>
        public IActionResult Privacy()
        {
            return View("~/Views/Home/Privacy.cshtml");
        }
    }
}
