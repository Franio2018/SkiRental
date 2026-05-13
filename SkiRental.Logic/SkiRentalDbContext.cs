using Microsoft.EntityFrameworkCore;
using SkiRental.Domain.Models;

namespace SkiRental.Logic;

/// <summary>
/// Kontekst bazy danych wypożyczalni nart.
/// Obsługuje zarówno SQLite (offline / mobilny) jak i SQL Server (back-office).
/// </summary>
public sealed class SkiRentalDbContext : DbContext
{
    public SkiRentalDbContext(DbContextOptions<SkiRentalDbContext> options)
        : base(options) { }

    // ── DbSets ───────────────────────────────────────────────────────────────
    public DbSet<AppUser>         AppUsers         { get; set; } = null!;
    public DbSet<Equipment>       Equipment        { get; set; } = null!;
    public DbSet<UserProfile>     UserProfiles     { get; set; } = null!;
    public DbSet<Rental>          Rentals          { get; set; } = null!;
    public DbSet<RentalEquipment> RentalEquipments { get; set; } = null!;
    public DbSet<Payment>         Payments         { get; set; } = null!;

    // ── Fluent API ───────────────────────────────────────────────────────────
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── AppUser ──────────────────────────────────────────────────────────
        modelBuilder.Entity<AppUser>(b =>
        {
            b.HasIndex(u => u.Username).IsUnique();

            // Optional 1-to-1 with UserProfile
            b.HasOne(u => u.Profile)
             .WithOne()
             .HasForeignKey<AppUser>(u => u.UserProfileId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Equipment ────────────────────────────────────────────────────────
        modelBuilder.Entity<Equipment>(b =>
        {
            b.HasIndex(e => e.SerialNumber).IsUnique();
            b.Property(e => e.Status).HasConversion<string>();
            b.Property(e => e.Category).HasConversion<string>();
            b.Property(e => e.CompatibleStyle).HasConversion<string>();
        });

        // ── UserProfile ──────────────────────────────────────────────────────
        modelBuilder.Entity<UserProfile>(b =>
        {
            b.Property(u => u.SkillLevel).HasConversion<string>();
            b.Property(u => u.PreferredStyle).HasConversion<string>();
            b.HasIndex(u => u.Email);
        });

        // ── Rental ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Rental>(b =>
        {
            b.Property(r => r.Status).HasConversion<string>();
            b.Property(r => r.PaymentStatus).HasConversion<string>();

            // FIX: Unique index only on non-null QR codes (SQLite supports filtered indexes)
            b.HasIndex(r => r.QrCode)
             .IsUnique()
             .HasFilter("[QrCode] IS NOT NULL");

            b.HasOne(r => r.Customer)
             .WithMany(u => u.Rentals)
             .HasForeignKey(r => r.CustomerProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── RentalEquipment (join table) ─────────────────────────────────────
        modelBuilder.Entity<RentalEquipment>(b =>
        {
            b.HasKey(re => new { re.RentalId, re.EquipmentId });

            b.HasOne(re => re.Rental)
             .WithMany(r => r.RentalEquipments)
             .HasForeignKey(re => re.RentalId);

            b.HasOne(re => re.Equipment)
             .WithMany(e => e.RentalEquipments)
             .HasForeignKey(re => re.EquipmentId);
        });

        // ── Payment ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Payment>(b =>
        {
            b.Property(p => p.Status).HasConversion<string>();

            b.HasOne(p => p.Rental)
             .WithMany(r => r.Payments)
             .HasForeignKey(p => p.RentalId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed data (demo) ─────────────────────────────────────────────────
        SeedDemoData(modelBuilder);
    }

    private static void SeedDemoData(ModelBuilder b)
    {
        // ── Equipment seed (unchanged) ────────────────────────────────────────
        b.Entity<Equipment>().HasData(
            new Equipment { Id = 1, SerialNumber = "FISCH-CL-001", Brand = "Fischer", Model = "Twin Skin Pro", Category = Domain.Enums.EquipmentCategory.Skis, Size = 192m, Stiffness = 52, Status = Domain.Enums.EquipmentStatus.Available, CompatibleStyle = Domain.Enums.SkiStyle.Classic, YearOfProduction = 2023, DailyRate = 60m },
            new Equipment { Id = 2, SerialNumber = "FISCH-CL-002", Brand = "Fischer", Model = "Twin Skin Pro", Category = Domain.Enums.EquipmentCategory.Skis, Size = 197m, Stiffness = 62, Status = Domain.Enums.EquipmentStatus.Available, CompatibleStyle = Domain.Enums.SkiStyle.Classic, YearOfProduction = 2023, DailyRate = 60m },
            new Equipment { Id = 3, SerialNumber = "SALOMON-SK-001", Brand = "Salomon", Model = "S/Race Skate Plus", Category = Domain.Enums.EquipmentCategory.Skis, Size = 182m, Stiffness = 62, Status = Domain.Enums.EquipmentStatus.Available, CompatibleStyle = Domain.Enums.SkiStyle.Skate, YearOfProduction = 2024, DailyRate = 70m },
            new Equipment { Id = 4, SerialNumber = "LEKI-CL-001", Brand = "Leki", Model = "Tour Stick Vario", Category = Domain.Enums.EquipmentCategory.Poles, Size = 150m, Status = Domain.Enums.EquipmentStatus.Available, CompatibleStyle = Domain.Enums.SkiStyle.Classic, YearOfProduction = 2023, DailyRate = 20m },
            new Equipment { Id = 5, SerialNumber = "LEKI-SK-001", Brand = "Leki", Model = "CC 450 Vario", Category = Domain.Enums.EquipmentCategory.Poles, Size = 155m, Status = Domain.Enums.EquipmentStatus.Available, CompatibleStyle = Domain.Enums.SkiStyle.Skate, YearOfProduction = 2023, DailyRate = 20m }
        );

        // ── Demo accounts seed ────────────────────────────────────────────────
        // Passwords are BCrypt hashed (workFactor 12).
        // Plain-text reference (NEVER commit to production):
        //   owner123  → Owner role
        //   worker123 → Worker role
        //   user123   → Customer role
        b.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = 1,
                Username = "owner",
                // owner123  (BCrypt workFactor 12) — do NOT regenerate; hash must be stable
                PasswordHash = "$2a$12$JYsSZdsvjG.TKqmgoBpIve9h01tQ9Jo35S1P/wkr30Ba5q/X94Xc6",
                Role = AppRole.Owner.ToString(),
                CreatedAt = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AppUser
            {
                Id = 2,
                Username = "worker",
                // worker123
                PasswordHash = "$2a$12$5N0AOyEPqcfjrnH5K3CuMu9wyYueUO/jv0KO1JYZ0AN/k.PV6bk/K",
                Role = AppRole.Worker.ToString(),
                CreatedAt = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new AppUser
            {
                Id = 3,
                Username = "user",
                // user123
                PasswordHash = "$2a$12$0HULvmchAwUoYvENSIbWiOgNlGCxeoKtb50QaikDU7qe5ZKHbrfVG",
                Role = AppRole.Customer.ToString(),
                CreatedAt = new DateTime(2024, 12, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
