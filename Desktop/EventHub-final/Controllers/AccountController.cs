using EventHub.Data;
using EventHub.Models;
using EventHub.Services;
using EventHub.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Controllers
{
    /// <summary>
    /// Kullanici kimlik dogrulama ve yetkilendirme islemlerini yoneten controller.
    /// Kayit, giris, cikis ve profil islemlerini kapsar.
    /// </summary>
    public class AccountController : Controller
    {
        // Identity servisleri DI ile enjekte edilir
        private readonly ApplicationDbContext _context;
        private readonly EventService _eventService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            ApplicationDbContext context,
            EventService eventService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _eventService = eventService;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _environment = environment;
        }

        // ----------------------------------------------------------------
        // KAYIT ISLEMI
        // ----------------------------------------------------------------

        /// <summary>
        /// Kullanici kayit sayfasini gosterir
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            // Giris yapilmissa ana sayfaya yonlendir
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new RegisterViewModel());
        }

        /// <summary>
        /// Kullanici kayit formunu isler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Model validasyonu basarisizsa formu tekrar goster
            if (!ModelState.IsValid)
                return View(model);

            // E-posta kullanilmakta mi kontrol et
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanilmaktadir.");
                return View(model);
            }

            // Yeni kullanici olustur
            var newUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Bio = model.Bio,
                EmailConfirmed = true, // Demo ortami icin otomatik onaylandi
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (result.Succeeded)
            {
                // Varsayilan olarak "User" rolunu ata
                if (!await _roleManager.RoleExistsAsync("User"))
                    await _roleManager.CreateAsync(new IdentityRole("User"));

                await _userManager.AddToRoleAsync(newUser, "User");

                // Kayit basarili, giris yaptir
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                TempData["Success"] = "Hesabiniz basariyla olusturuldu. Hos geldiniz!";
                return RedirectToAction("Index", "Home");
            }

            // Identity hata mesajlarini ekle
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ----------------------------------------------------------------
        // GIRIS ISLEMI
        // ----------------------------------------------------------------

        /// <summary>
        /// Kullanici giris sayfasini gosterir
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        /// <summary>
        /// Kullanici giris formunu isler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Giris islemini gerceklestir
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true); // 5 basarisiz denemede hesap kilitlenir

            if (result.Succeeded)
            {
                TempData["Success"] = "Basariyla giris yaptiniz.";

                // Guvenligi dogrula ve yonlendir
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Hesabiniz gecici olarak kilitlendi. Lutfen birkaç dakika sonra tekrar deneyin.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "E-posta veya sifre yanlis.");
            return View(model);
        }

        // ----------------------------------------------------------------
        // CIKIS ISLEMI
        // ----------------------------------------------------------------

        /// <summary>
        /// Kullanici cikis islemini gerceklestirir
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "Guvenli cikis yapildi.";
            return RedirectToAction("Index", "Home");
        }

        // ----------------------------------------------------------------
        // PROFIL SAYFASI
        // ----------------------------------------------------------------

        /// <summary>
        /// Kullanici profil sayfasini gosterir
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var model = await BuildProfilePageViewModelAsync(user);
            return View(model);
        }

        /// <summary>
        /// Profil bilgilerini gunceller.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([Bind(Prefix = "Edit")] ProfileEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildProfilePageViewModelAsync(user, editInput: model);
                invalidModel.IsEditPanelOpen = true;
                return View("Profile", invalidModel);
            }

            user.FullName = model.FullName.Trim();
            user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
            user.ProfileImageUrl = await SaveProfileImageAsync(model.ProfileImageFile, user.ProfileImageUrl);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                var failedModel = await BuildProfilePageViewModelAsync(user, editInput: model);
                failedModel.IsEditPanelOpen = true;
                return View("Profile", failedModel);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Profil bilgileriniz guncellendi.";
            return RedirectToAction(nameof(Profile));
        }

        /// <summary>
        /// Kullanici sifresini degistirir.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                var invalidModel = await BuildProfilePageViewModelAsync(user, passwordInput: model);
                invalidModel.IsEditPanelOpen = true;
                invalidModel.IsPasswordPanelOpen = true;
                return View("Profile", invalidModel);
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                var failedModel = await BuildProfilePageViewModelAsync(user, passwordInput: model);
                failedModel.IsEditPanelOpen = true;
                failedModel.IsPasswordPanelOpen = true;
                return View("Profile", failedModel);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Sifreniz guncellendi.";
            return RedirectToAction(nameof(Profile));
        }

        /// <summary>
        /// Kullanici bildirimlerini listeler.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Notifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var notifications = await _context.UserNotifications
                .Include(n => n.Event)
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var viewModel = notifications
                .Select(n => new UserNotificationViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    CreatedAt = n.CreatedAt,
                    IsRead = n.IsRead,
                    RequiresAction = n.RequiresAction,
                    EventTitle = n.Event != null ? n.Event.Title : null,
                    EventId = n.EventId,
                    RegistrationId = n.RegistrationId,
                    Type = n.Type
                })
                .ToList();

            return View(viewModel);
        }

        /// <summary>
        /// Bekleme listesinden cikma davetini kabul eder.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AcceptWaitlistNotification(int notificationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var (success, message, eventId) = await _eventService.RespondToWaitlistInvitationAsync(notificationId, user.Id, true);
            TempData[success ? "Success" : "Error"] = message;

            if (eventId.HasValue)
                return RedirectToAction("Detail", "Event", new { id = eventId.Value });

            return RedirectToAction(nameof(Notifications));
        }

        /// <summary>
        /// Bekleme listesinden cikma davetini reddeder.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeclineWaitlistNotification(int notificationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var (success, message, eventId) = await _eventService.RespondToWaitlistInvitationAsync(notificationId, user.Id, false);
            TempData[success ? "Success" : "Error"] = message;

            if (eventId.HasValue)
                return RedirectToAction("Detail", "Event", new { id = eventId.Value });

            return RedirectToAction(nameof(Notifications));
        }

        // ----------------------------------------------------------------
        // ERISIM REDDEDILDI SAYFASI
        // ----------------------------------------------------------------

        /// <summary>
        /// Yetkisiz erisim durumunda gosterilen sayfa
        /// </summary>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<ProfilePageViewModel> BuildProfilePageViewModelAsync(
            ApplicationUser user,
            ProfileEditViewModel? editInput = null,
            ChangePasswordViewModel? passwordInput = null)
        {
            var confirmedRegistrations = await _context.EventRegistrations
                .Include(r => r.Event)
                .Where(r => r.UserId == user.Id && r.Status == RegistrationStatus.Confirmed)
                .ToListAsync();

            var totalRegistrations = confirmedRegistrations.Count;
            var upcomingRegistrations = confirmedRegistrations.Count(r =>
                r.Event != null && r.Event.EventDate >= DateTime.UtcNow);

            var roleName = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "User";

            return new ProfilePageViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Bio = user.Bio,
                ProfileImageUrl = user.ProfileImageUrl,
                CreatedAt = user.CreatedAt,
                RoleName = roleName,
                TotalRegistrations = totalRegistrations,
                UpcomingRegistrations = upcomingRegistrations,
                PastRegistrations = Math.Max(0, totalRegistrations - upcomingRegistrations),
                Edit = editInput ?? new ProfileEditViewModel
                {
                    FullName = user.FullName,
                    Bio = user.Bio,
                    ProfileImageUrl = user.ProfileImageUrl
                },
                Password = passwordInput ?? new ChangePasswordViewModel(),
                IsEditPanelOpen = editInput != null,
                IsPasswordPanelOpen = passwordInput != null
            };
        }

        private async Task<string?> SaveProfileImageAsync(IFormFile? file, string? currentImagePath)
        {
            if (file == null || file.Length == 0)
                return currentImagePath;

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsRoot = Path.Combine(webRoot, "uploads", "profile-images");
            Directory.CreateDirectory(uploadsRoot);

            if (!string.IsNullOrWhiteSpace(currentImagePath) &&
                currentImagePath.StartsWith("/uploads/profile-images/", StringComparison.OrdinalIgnoreCase))
            {
                var oldRelativePath = currentImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
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

            return $"/uploads/profile-images/{fileName}";
        }
    }
}
