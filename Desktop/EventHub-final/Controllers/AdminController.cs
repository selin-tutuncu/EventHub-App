using EventHub.Data;
using EventHub.Models;
using EventHub.Services;
using EventHub.ViewModels;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    /// <summary>
    /// Admin paneli islemlerini yoneten controller.
    /// Tum action'lar yalnizca "Admin" rolundeki kullanicilara erisime aciktir.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EventService _eventService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            EventService eventService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _eventService = eventService;
            _userManager = userManager;
        }

        // ----------------------------------------------------------------
        // ADMIN PANEL ANA SAYFASI
        // ----------------------------------------------------------------

        /// <summary>
        /// Admin dashboard: istatistikler ve ozet bilgiler
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Etkinlik istatistikleri
            var totalEvents = await _context.Events.CountAsync();
            var publishedEvents = await _context.Events.CountAsync(e => e.Status == EventStatus.Published);
            var draftEvents = await _context.Events.CountAsync(e => e.Status == EventStatus.Draft);

            // Kullanici istatistikleri
            var totalUsers = await _userManager.Users.CountAsync();

            // Kayit istatistikleri
            var totalRegistrations = await _context.EventRegistrations
                .CountAsync(r => r.Status == RegistrationStatus.Confirmed);
            var todayRegistrations = await _context.EventRegistrations
                .CountAsync(r => r.RegisteredAt.Date == DateTime.UtcNow.Date
                    && r.Status == RegistrationStatus.Confirmed);

            // Son eklenen 5 etkinlik
            var recentEvents = await _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.Registrations)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Kategoriye gore etkinlik dagilimi
            var eventsByCategory = await _context.Events
                .GroupBy(e => e.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalEvents = totalEvents,
                PublishedEvents = publishedEvents,
                DraftEvents = draftEvents,
                TotalUsers = totalUsers,
                TotalRegistrations = totalRegistrations,
                TodayRegistrations = todayRegistrations,
                RecentEvents = recentEvents.Select(e =>
                {
                    var confirmed = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
                    return new EventListItemViewModel
                    {
                        Id = e.Id,
                        Title = e.Title,
                        EventDate = e.EventDate,
                        Location = e.Location,
                        Category = e.Category,
                        Status = e.Status,
                        MaxCapacity = e.MaxCapacity,
                        CurrentParticipantCount = confirmed,
                        IsFullyBooked = e.MaxCapacity > 0 && confirmed >= e.MaxCapacity,
                        OrganizerName = e.CreatedBy?.FullName ?? "Bilinmiyor"
                    };
                }).ToList(),
                EventsByCategory = eventsByCategory.ToDictionary(
                    x => x.Category.ToString(),
                    x => x.Count)
            };

            return View(viewModel);
        }

        // ----------------------------------------------------------------
        // ETKINLIK YONETIMI
        // ----------------------------------------------------------------

        /// <summary>
        /// Admin: Tum etkinlikleri listeler
        /// </summary>
        public async Task<IActionResult> Events()
        {
            var events = await _eventService.GetAllEventsForAdminAsync();
            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> ExportEventsExcel()
        {
            var events = await _eventService.GetAllEventsForAdminAsync();
            using var workbook = BuildEventsWorkbook(events);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"EventHub-Etkinlikler-{DateTime.Now:yyyyMMddHHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportEventsPdf()
        {
            var events = await _eventService.GetAllEventsForAdminAsync();
            var bytes = BuildEventsPdf(events);
            var fileName = $"EventHub-Etkinlikler-{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Admin: Etkinligi siler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                TempData["Error"] = "Etkinlik bulunamadi.";
                return RedirectToAction("Events");
            }

            // Once ilgili kayitlari sil (cascade olmayan durum icin guvenlik)
            var registrations = _context.EventRegistrations.Where(r => r.EventId == id);
            _context.EventRegistrations.RemoveRange(registrations);

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{ev.Title}' etkinligi silindi.";
            return RedirectToAction("Events");
        }

        /// <summary>
        /// Admin: Etkinlik durumunu hizlica degistirir (Taslak/Yayinla)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEventStatus(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null)
            {
                TempData["Error"] = "Etkinlik bulunamadi.";
                return RedirectToAction("Events");
            }

            // Taslak -> Yayinda, Yayinda -> Taslak
            ev.Status = ev.Status == EventStatus.Published
                ? EventStatus.Draft
                : EventStatus.Published;

            ev.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var statusText = ev.Status == EventStatus.Published ? "yayinlandi" : "taslaga alindi";
            TempData["Success"] = $"'{ev.Title}' etkinligi {statusText}.";
            return RedirectToAction("Events");
        }

        // ----------------------------------------------------------------
        // KULLANICI YONETIMI
        // ----------------------------------------------------------------

        /// <summary>
        /// Admin: Tum kullanicilari listeler
        /// </summary>
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            // Her kullanicinin rollerini ve kayit sayisini ViewBag ile gonder
            var userRoles = new Dictionary<string, IList<string>>();
            var userRegCounts = new Dictionary<string, int>();

            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
                userRegCounts[user.Id] = await _context.EventRegistrations
                    .CountAsync(r => r.UserId == user.Id && r.Status == RegistrationStatus.Confirmed);
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.UserRegistrationCounts = userRegCounts;

            return View(users);
        }

        /// <summary>
        /// Admin: Kullanici rolunu degistirir (Admin <-> User)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Kullanici bulunamadi.";
                return RedirectToAction("Users");
            }

            // Kendini degrade etmeyi engelle
            var currentUserId = _userManager.GetUserId(User);
            if (userId == currentUserId)
            {
                TempData["Error"] = "Kendi rol bilginizi degistiremezsiniz.";
                return RedirectToAction("Users");
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (isAdmin)
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                await _userManager.AddToRoleAsync(user, "User");
                TempData["Success"] = $"{user.FullName} kullanicisinin rolu 'User' olarak guncellendi.";
            }
            else
            {
                await _userManager.RemoveFromRoleAsync(user, "User");
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = $"{user.FullName} kullanicisinin rolu 'Admin' olarak guncellendi.";
            }

            return RedirectToAction("Users");
        }

        // ----------------------------------------------------------------
        // KATILIM DOGRULAMA
        // ----------------------------------------------------------------

        /// <summary>
        /// Admin: Bir etkinligin katilim listesini gosterir
        /// </summary>
        public async Task<IActionResult> Participants(int eventId)
        {
            var participantBundle = await LoadParticipantBundleAsync(eventId);
            if (participantBundle == null)
            {
                TempData["Error"] = "Etkinlik bulunamadi.";
                return RedirectToAction("Events");
            }

            ViewBag.EventTitle = participantBundle.EventTitle;
            ViewBag.EventId = eventId;
            return View(participantBundle.Participants);
        }

        [HttpGet]
        public async Task<IActionResult> ExportParticipantsExcel(int eventId)
        {
            var participantBundle = await LoadParticipantBundleAsync(eventId);
            if (participantBundle == null)
            {
                TempData["Error"] = "Etkinlik bulunamadi.";
                return RedirectToAction("Events");
            }

            using var workbook = BuildParticipantsWorkbook(participantBundle.EventTitle, participantBundle.Participants);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"EventHub-Katilimcilar-{eventId}-{DateTime.Now:yyyyMMddHHmm}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportParticipantsPdf(int eventId)
        {
            var participantBundle = await LoadParticipantBundleAsync(eventId);
            if (participantBundle == null)
            {
                TempData["Error"] = "Etkinlik bulunamadi.";
                return RedirectToAction("Events");
            }

            var bytes = BuildParticipantsPdf(participantBundle.EventTitle, participantBundle.Participants);
            var fileName = $"EventHub-Katilimcilar-{eventId}-{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Admin: Katilim durumunu toggler (Katildi / Katilmadi)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAttendance(int registrationId, int eventId)
        {
            var registration = await _context.EventRegistrations.FindAsync(registrationId);
            if (registration != null)
            {
                registration.AttendanceConfirmed = !registration.AttendanceConfirmed;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Participants", new { eventId });
        }

        private async Task<ParticipantBundleResult?> LoadParticipantBundleAsync(int eventId)
        {
            var ev = await _context.Events
                .Include(e => e.Registrations)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return null;

            var participants = ev.Registrations
                .Where(r => r.Status == RegistrationStatus.Confirmed)
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

            return new ParticipantBundleResult
            {
                EventTitle = ev.Title,
                Participants = participants
            };
        }

        private static XLWorkbook BuildEventsWorkbook(List<EventListItemViewModel> events)
        {
            var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Etkinlikler");

            var headers = new[]
            {
                "#", "Etkinlik", "Kategori", "Tarih", "Durum", "Kontenjan", "Katilimci", "Organizator"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                sheet.Cell(2, i + 1).Value = headers[i];
                sheet.Cell(2, i + 1).Style.Font.Bold = true;
                sheet.Cell(2, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FDEDE4");
            }

            sheet.Cell(1, 1).Value = "EventHub Etkinlik Raporu";
            sheet.Range(1, 1, 1, headers.Length).Merge();
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 16;

            for (var i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                var row = i + 3;
                sheet.Cell(row, 1).Value = i + 1;
                sheet.Cell(row, 2).Value = ev.Title;
                sheet.Cell(row, 3).Value = ev.Category.ToString();
                sheet.Cell(row, 4).Value = ev.EventDate.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));
                sheet.Cell(row, 5).Value = ev.Status.ToString();
                sheet.Cell(row, 6).Value = ev.MaxCapacity > 0 ? ev.MaxCapacity.ToString() : "Sinirsiz";
                sheet.Cell(row, 7).Value = ev.CurrentParticipantCount;
                sheet.Cell(row, 8).Value = ev.OrganizerName;
            }

            sheet.Columns().AdjustToContents();
            return workbook;
        }

        private static XLWorkbook BuildParticipantsWorkbook(string eventTitle, List<ParticipantViewModel> participants)
        {
            var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Katilimcilar");

            var headers = new[]
            {
                "#", "Ad Soyad", "E-posta", "Kayit Tarihi", "Durum", "Katilim", "Not"
            };

            sheet.Cell(1, 1).Value = $"{eventTitle} - Katilimci Listesi";
            sheet.Range(1, 1, 1, headers.Length).Merge();
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 16;

            for (var i = 0; i < headers.Length; i++)
            {
                sheet.Cell(2, i + 1).Value = headers[i];
                sheet.Cell(2, i + 1).Style.Font.Bold = true;
                sheet.Cell(2, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FDEDE4");
            }

            for (var i = 0; i < participants.Count; i++)
            {
                var p = participants[i];
                var row = i + 3;
                sheet.Cell(row, 1).Value = i + 1;
                sheet.Cell(row, 2).Value = p.FullName;
                sheet.Cell(row, 3).Value = p.Email;
                sheet.Cell(row, 4).Value = p.RegisteredAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"));
                sheet.Cell(row, 5).Value = p.Status.ToString();
                sheet.Cell(row, 6).Value = p.AttendanceConfirmed ? "Katildi" : "Bekleniyor";
                sheet.Cell(row, 7).Value = p.Notes ?? string.Empty;
            }

            sheet.Columns().AdjustToContents();
            return workbook;
        }

        private static byte[] BuildEventsPdf(List<EventListItemViewModel> events)
        {
            using var stream = new MemoryStream();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("EventHub Etkinlik Raporu").FontSize(18).SemiBold().FontColor(Colors.Orange.Medium);
                        col.Item().Text($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("#").SemiBold();
                            header.Cell().Element(CellStyle).Text("Etkinlik").SemiBold();
                            header.Cell().Element(CellStyle).Text("Tarih").SemiBold();
                            header.Cell().Element(CellStyle).Text("Durum").SemiBold();
                            header.Cell().Element(CellStyle).Text("Katilimci").SemiBold();
                        });

                        foreach (var item in events.Select((ev, index) => new { ev, index }))
                        {
                            table.Cell().Element(CellStyle).Text((item.index + 1).ToString());
                            table.Cell().Element(CellStyle).Text(item.ev.Title);
                            table.Cell().Element(CellStyle).Text(item.ev.EventDate.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")));
                            table.Cell().Element(CellStyle).Text(item.ev.Status.ToString());
                            table.Cell().Element(CellStyle).Text(item.ev.CurrentParticipantCount.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text("EventHub").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf(stream);

            return stream.ToArray();

            static IContainer CellStyle(IContainer container)
            {
                return container.PaddingVertical(5).PaddingHorizontal(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
            }
        }

        private static byte[] BuildParticipantsPdf(string eventTitle, List<ParticipantViewModel> participants)
        {
            using var stream = new MemoryStream();
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(eventTitle).FontSize(18).SemiBold().FontColor(Colors.Orange.Medium);
                        col.Item().Text("Katilimci Listesi").FontSize(11).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("#").SemiBold();
                            header.Cell().Element(CellStyle).Text("Ad Soyad").SemiBold();
                            header.Cell().Element(CellStyle).Text("E-posta").SemiBold();
                            header.Cell().Element(CellStyle).Text("Durum").SemiBold();
                            header.Cell().Element(CellStyle).Text("Katilim").SemiBold();
                        });

                        foreach (var item in participants.Select((p, index) => new { p, index }))
                        {
                            table.Cell().Element(CellStyle).Text((item.index + 1).ToString());
                            table.Cell().Element(CellStyle).Text(item.p.FullName);
                            table.Cell().Element(CellStyle).Text(item.p.Email);
                            table.Cell().Element(CellStyle).Text(item.p.Status.ToString());
                            table.Cell().Element(CellStyle).Text(item.p.AttendanceConfirmed ? "Katildi" : "Bekleniyor");
                        }
                    });

                    page.Footer().AlignCenter().Text("EventHub").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf(stream);

            return stream.ToArray();

            static IContainer CellStyle(IContainer container)
            {
                return container.PaddingVertical(5).PaddingHorizontal(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
            }
        }

        private sealed class ParticipantBundleResult
        {
            public string EventTitle { get; set; } = string.Empty;
            public List<ParticipantViewModel> Participants { get; set; } = new();
        }
    }
}
