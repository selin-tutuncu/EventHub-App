using EventHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Data
{
    /// <summary>
    /// Uygulamanin ana veritabani baglam sinifi.
    /// IdentityDbContext'ten turetilerek Identity tablolari otomatik eklenir.
    /// Code-First yaklasimi ile migration'lar bu sinif uzerinden yonetilir.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        // Yapici metot - DI ile DbContextOptions enjekte edilir
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Etkinlikler tablosu
        public DbSet<Event> Events { get; set; } = null!;

        // Etkinlik kayitlari tablosu
        public DbSet<EventRegistration> EventRegistrations { get; set; } = null!;

        // Kullanici bildirimleri tablosu
        public DbSet<UserNotification> UserNotifications { get; set; } = null!;

        /// <summary>
        /// Model yapilandirmasi: iliskiler, kisitlamalar ve seed data burada tanimlanir.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Temel Identity konfigurasyonunu cagir
            base.OnModelCreating(builder);

            // --------- Event Konfigurasyonu ---------

            builder.Entity<Event>(entity =>
            {
                // Baslik benzersiz olmak zorunda degil ama indeksleme performans iyilestirmesi
                entity.HasIndex(e => e.EventDate);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Status);

                // Decimal precision tanimlama
                entity.Property(e => e.Price).HasPrecision(10, 2);

                // Etkinlik - Olusturan kullanici iliskisi (bir kullanici silinince etkinlikler korunur)
                entity.HasOne(e => e.CreatedBy)
                    .WithMany(u => u.CreatedEvents)
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // --------- EventRegistration Konfigurasyonu ---------

            builder.Entity<EventRegistration>(entity =>
            {
                // Bir kullanici ayni etkinlige yalnizca bir kez kayit olabilir
                entity.HasIndex(r => new { r.UserId, r.EventId }).IsUnique();

                entity.Property(r => r.PaidAmount).HasPrecision(10, 2);

                // Kayit - Kullanici iliskisi
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Registrations)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Kayit - Etkinlik iliskisi (etkinlik silinince kayitlar da silinir)
                entity.HasOne(r => r.Event)
                    .WithMany(e => e.Registrations)
                    .HasForeignKey(r => r.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<UserNotification>(entity =>
            {
                entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
                entity.HasIndex(n => new { n.EventId, n.Type });

                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.Event)
                    .WithMany()
                    .HasForeignKey(n => n.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.Registration)
                    .WithMany()
                    .HasForeignKey(n => n.RegistrationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // --------- Identity Tablo Ismi Ozellestime ---------
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // --------- Seed Data: Roller ---------
            // Uygulama baslangicinda Admin ve User rolleri olusturulur
            var adminRoleId = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
            var userRoleId = "b2c3d4e5-f6a7-8901-bcde-f12345678901";

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = adminRoleId
                },
                new IdentityRole
                {
                    Id = userRoleId,
                    Name = "User",
                    NormalizedName = "USER",
                    ConcurrencyStamp = userRoleId
                }
            );

            // --------- Seed Data: Admin Kullanicisi ---------
            var adminUserId = "c3d4e5f6-a7b8-9012-cdef-012345678902";
            var hasher = new PasswordHasher<ApplicationUser>();
            var adminUser = new ApplicationUser
            {
                Id = adminUserId,
                UserName = "admin@eventhub.com",
                NormalizedUserName = "ADMIN@EVENTHUB.COM",
                Email = "admin@eventhub.com",
                NormalizedEmail = "ADMIN@EVENTHUB.COM",
                EmailConfirmed = true,
                FullName = "EventHub Yoneticisi",
                Bio = "EventHub platformunun ana yoneticisi.",
                CreatedAt = new DateTime(2024, 1, 1),
                SecurityStamp = Guid.NewGuid().ToString()
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin123!");

            builder.Entity<ApplicationUser>().HasData(adminUser);

            // Admin kullanicisina Admin rolu atama
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    UserId = adminUserId,
                    RoleId = adminRoleId
                }
            );
        }
    }
}
