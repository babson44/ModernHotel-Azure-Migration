using Microsoft.EntityFrameworkCore;
using ModernHotel.Core.Entities;

namespace ModernHotel.Infrastructure.Data;

public class HotelDbContext : DbContext
{
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options) { }

    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Guest>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.FirstName).IsRequired().HasMaxLength(100);
            e.Property(g => g.LastName).IsRequired().HasMaxLength(100);
            e.Property(g => g.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(g => g.Email).IsUnique();
            e.Property(g => g.Phone).HasMaxLength(30);
            e.Property(g => g.Address).HasMaxLength(300);
            e.Property(g => g.Nationality).HasMaxLength(100);
            e.Ignore(g => g.FullName);
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.RoomNumber).IsRequired().HasMaxLength(10);
            e.HasIndex(r => r.RoomNumber).IsUnique();
            e.Property(r => r.RatePerNight).HasPrecision(10, 2);
            e.Property(r => r.Description).HasMaxLength(500);
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.TotalAmount).HasPrecision(10, 2);
            e.Property(r => r.SpecialRequests).HasMaxLength(500);
            e.Ignore(r => r.Nights);

            e.HasOne(r => r.Guest)
                .WithMany(g => g.Reservations)
                .HasForeignKey(r => r.GuestId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(r => r.Room)
                .WithMany(rm => rm.Reservations)
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Rooms
        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1,  RoomNumber = "101", Type = RoomType.Standard,  Floor = 1, BedCount = 1, MaxOccupancy = 2, RatePerNight = 129.00m, Description = "Cozy standard room with garden view",           IsActive = true },
            new Room { Id = 2,  RoomNumber = "102", Type = RoomType.Standard,  Floor = 1, BedCount = 2, MaxOccupancy = 4, RatePerNight = 149.00m, Description = "Spacious standard double room",                   IsActive = true },
            new Room { Id = 3,  RoomNumber = "201", Type = RoomType.Deluxe,    Floor = 2, BedCount = 1, MaxOccupancy = 2, RatePerNight = 199.00m, Description = "Deluxe room with city view",                      IsActive = true },
            new Room { Id = 4,  RoomNumber = "202", Type = RoomType.Deluxe,    Floor = 2, BedCount = 2, MaxOccupancy = 4, RatePerNight = 229.00m, Description = "Deluxe double room with balcony",                 IsActive = true },
            new Room { Id = 5,  RoomNumber = "203", Type = RoomType.Deluxe,    Floor = 2, BedCount = 1, MaxOccupancy = 2, RatePerNight = 199.00m, Description = "Deluxe room with pool view",                      IsActive = true },
            new Room { Id = 6,  RoomNumber = "301", Type = RoomType.Suite,     Floor = 3, BedCount = 1, MaxOccupancy = 3, RatePerNight = 349.00m, Description = "Junior suite with separate living area",           IsActive = true },
            new Room { Id = 7,  RoomNumber = "302", Type = RoomType.Suite,     Floor = 3, BedCount = 2, MaxOccupancy = 4, RatePerNight = 449.00m, Description = "Executive suite with panoramic view",              IsActive = true },
            new Room { Id = 8,  RoomNumber = "303", Type = RoomType.Suite,     Floor = 3, BedCount = 2, MaxOccupancy = 4, RatePerNight = 499.00m, Description = "Presidential suite with private terrace",          IsActive = true },
            new Room { Id = 9,  RoomNumber = "401", Type = RoomType.Penthouse, Floor = 4, BedCount = 2, MaxOccupancy = 4, RatePerNight = 799.00m, Description = "Penthouse with full-floor panoramic views",        IsActive = true },
            new Room { Id = 10, RoomNumber = "402", Type = RoomType.Penthouse, Floor = 4, BedCount = 3, MaxOccupancy = 6, RatePerNight = 999.00m, Description = "Grand penthouse with private pool access",         IsActive = true }
        );

        // Seed Guests
        modelBuilder.Entity<Guest>().HasData(
            new Guest { Id = 1,  FirstName = "James",     LastName = "Mitchell",   Email = "james.mitchell@email.com",   Phone = "+1-555-0101", Nationality = "American",    Address = "123 Oak Street, New York, NY", CreatedAt = new DateTime(2024, 1, 15) },
            new Guest { Id = 2,  FirstName = "Sofia",     LastName = "Andersson",  Email = "sofia.andersson@email.com",  Phone = "+46-70-123456", Nationality = "Swedish",   Address = "Storgatan 5, Stockholm",       CreatedAt = new DateTime(2024, 2, 3) },
            new Guest { Id = 3,  FirstName = "Carlos",    LastName = "Rodriguez",  Email = "carlos.rodriguez@email.com", Phone = "+34-91-234567", Nationality = "Spanish",   Address = "Calle Mayor 42, Madrid",       CreatedAt = new DateTime(2024, 2, 18) },
            new Guest { Id = 4,  FirstName = "Yuki",      LastName = "Tanaka",     Email = "yuki.tanaka@email.com",      Phone = "+81-3-12345678", Nationality = "Japanese", Address = "1-2-3 Shinjuku, Tokyo",        CreatedAt = new DateTime(2024, 3, 5) },
            new Guest { Id = 5,  FirstName = "Emma",      LastName = "Thompson",   Email = "emma.thompson@email.com",    Phone = "+44-20-987654", Nationality = "British",   Address = "15 Baker Street, London",      CreatedAt = new DateTime(2024, 3, 22) },
            new Guest { Id = 6,  FirstName = "Marco",     LastName = "Ferrari",    Email = "marco.ferrari@email.com",    Phone = "+39-02-345678", Nationality = "Italian",   Address = "Via Roma 88, Milan",           CreatedAt = new DateTime(2024, 4, 10) },
            new Guest { Id = 7,  FirstName = "Aisha",     LastName = "Khalil",     Email = "aisha.khalil@email.com",     Phone = "+971-50-123456", Nationality = "Emirati",  Address = "DIFC Tower, Dubai",            CreatedAt = new DateTime(2024, 4, 28) },
            new Guest { Id = 8,  FirstName = "David",     LastName = "Chen",       Email = "david.chen@email.com",       Phone = "+1-650-555-0199", Nationality = "American", Address = "500 Sand Hill Rd, Palo Alto", CreatedAt = new DateTime(2024, 5, 14) },
            new Guest { Id = 9,  FirstName = "Marie",     LastName = "Dubois",     Email = "marie.dubois@email.com",     Phone = "+33-1-23456789", Nationality = "French",   Address = "12 Rue de Rivoli, Paris",      CreatedAt = new DateTime(2024, 5, 30) },
            new Guest { Id = 10, FirstName = "Lucas",     LastName = "Oliveira",   Email = "lucas.oliveira@email.com",   Phone = "+55-11-98765432", Nationality = "Brazilian", Address = "Av Paulista 1500, São Paulo", CreatedAt = new DateTime(2024, 6, 7) },
            new Guest { Id = 11, FirstName = "Priya",     LastName = "Sharma",     Email = "priya.sharma@email.com",     Phone = "+91-98765-43210", Nationality = "Indian",  Address = "MG Road, Bangalore",           CreatedAt = new DateTime(2024, 6, 20) },
            new Guest { Id = 12, FirstName = "Alexander", LastName = "Petrov",     Email = "alex.petrov@email.com",      Phone = "+7-495-123456", Nationality = "Russian",   Address = "Tverskaya 10, Moscow",         CreatedAt = new DateTime(2024, 7, 3) },
            new Guest { Id = 13, FirstName = "Hannah",    LastName = "Müller",     Email = "hannah.mueller@email.com",   Phone = "+49-30-123456", Nationality = "German",    Address = "Kurfürstendamm 100, Berlin",   CreatedAt = new DateTime(2024, 7, 18) },
            new Guest { Id = 14, FirstName = "Omar",      LastName = "Hassan",     Email = "omar.hassan@email.com",      Phone = "+20-2-12345678", Nationality = "Egyptian", Address = "Tahrir Square 5, Cairo",       CreatedAt = new DateTime(2024, 8, 1) },
            new Guest { Id = 15, FirstName = "Isabella",  LastName = "Rossi",      Email = "isabella.rossi@email.com",   Phone = "+39-06-456789", Nationality = "Italian",   Address = "Piazza Navona 3, Rome",        CreatedAt = new DateTime(2024, 8, 15) }
        );

        // Seed Reservations  (spread across past, present, future)
        var today = new DateTime(2025, 7, 23);
        modelBuilder.Entity<Reservation>().HasData(
            // Checked Out (past)
            new Reservation { Id = 1,  GuestId = 1,  RoomId = 3,  CheckInDate = today.AddDays(-10), CheckOutDate = today.AddDays(-7),  Status = ReservationStatus.CheckedOut, TotalAmount = 597.00m, Adults = 2, Children = 0, CreatedAt = today.AddDays(-20), ActualCheckIn = today.AddDays(-10), ActualCheckOut = today.AddDays(-7) },
            new Reservation { Id = 2,  GuestId = 2,  RoomId = 6,  CheckInDate = today.AddDays(-8),  CheckOutDate = today.AddDays(-5),  Status = ReservationStatus.CheckedOut, TotalAmount = 1047.00m, Adults = 1, Children = 0, CreatedAt = today.AddDays(-15), ActualCheckIn = today.AddDays(-8), ActualCheckOut = today.AddDays(-5) },
            new Reservation { Id = 3,  GuestId = 5,  RoomId = 1,  CheckInDate = today.AddDays(-6),  CheckOutDate = today.AddDays(-4),  Status = ReservationStatus.CheckedOut, TotalAmount = 258.00m, Adults = 1, Children = 0, CreatedAt = today.AddDays(-14), ActualCheckIn = today.AddDays(-6), ActualCheckOut = today.AddDays(-4) },
            new Reservation { Id = 4,  GuestId = 9,  RoomId = 9,  CheckInDate = today.AddDays(-5),  CheckOutDate = today.AddDays(-2),  Status = ReservationStatus.CheckedOut, TotalAmount = 2397.00m, Adults = 2, Children = 1, CreatedAt = today.AddDays(-18), ActualCheckIn = today.AddDays(-5), ActualCheckOut = today.AddDays(-2) },

            // Currently Checked In
            new Reservation { Id = 5,  GuestId = 3,  RoomId = 4,  CheckInDate = today.AddDays(-2),  CheckOutDate = today.AddDays(2),   Status = ReservationStatus.CheckedIn,  TotalAmount = 916.00m, Adults = 2, Children = 2, CreatedAt = today.AddDays(-20), ActualCheckIn = today.AddDays(-2) },
            new Reservation { Id = 6,  GuestId = 4,  RoomId = 7,  CheckInDate = today.AddDays(-1),  CheckOutDate = today.AddDays(3),   Status = ReservationStatus.CheckedIn,  TotalAmount = 1796.00m, Adults = 2, Children = 0, CreatedAt = today.AddDays(-12), ActualCheckIn = today.AddDays(-1) },
            new Reservation { Id = 7,  GuestId = 7,  RoomId = 10, CheckInDate = today,               CheckOutDate = today.AddDays(4),   Status = ReservationStatus.CheckedIn,  TotalAmount = 3996.00m, Adults = 2, Children = 2, CreatedAt = today.AddDays(-30), ActualCheckIn = today },
            new Reservation { Id = 8,  GuestId = 8,  RoomId = 5,  CheckInDate = today.AddDays(-3),  CheckOutDate = today.AddDays(1),   Status = ReservationStatus.CheckedIn,  TotalAmount = 796.00m, Adults = 1, Children = 0, CreatedAt = today.AddDays(-10), ActualCheckIn = today.AddDays(-3) },

            // Arriving Today (Confirmed)
            new Reservation { Id = 9,  GuestId = 6,  RoomId = 8,  CheckInDate = today,               CheckOutDate = today.AddDays(3),   Status = ReservationStatus.Confirmed,  TotalAmount = 1497.00m, Adults = 2, Children = 0, CreatedAt = today.AddDays(-25) },
            new Reservation { Id = 10, GuestId = 11, RoomId = 2,  CheckInDate = today,               CheckOutDate = today.AddDays(2),   Status = ReservationStatus.Confirmed,  TotalAmount = 298.00m, Adults = 2, Children = 1, CreatedAt = today.AddDays(-7) },

            // Upcoming
            new Reservation { Id = 11, GuestId = 10, RoomId = 6,  CheckInDate = today.AddDays(2),   CheckOutDate = today.AddDays(5),   Status = ReservationStatus.Confirmed,  TotalAmount = 1047.00m, Adults = 2, Children = 0, CreatedAt = today.AddDays(-14) },
            new Reservation { Id = 12, GuestId = 12, RoomId = 3,  CheckInDate = today.AddDays(3),   CheckOutDate = today.AddDays(6),   Status = ReservationStatus.Confirmed,  TotalAmount = 597.00m, Adults = 1, Children = 0, CreatedAt = today.AddDays(-5) },
            new Reservation { Id = 13, GuestId = 13, RoomId = 9,  CheckInDate = today.AddDays(5),   CheckOutDate = today.AddDays(9),   Status = ReservationStatus.Confirmed,  TotalAmount = 3196.00m, Adults = 2, Children = 0, CreatedAt = today.AddDays(-20) },
            new Reservation { Id = 14, GuestId = 14, RoomId = 1,  CheckInDate = today.AddDays(7),   CheckOutDate = today.AddDays(10),  Status = ReservationStatus.Pending,    TotalAmount = 387.00m, Adults = 1, Children = 0, CreatedAt = today.AddDays(-2) },
            new Reservation { Id = 15, GuestId = 15, RoomId = 7,  CheckInDate = today.AddDays(10),  CheckOutDate = today.AddDays(14),  Status = ReservationStatus.Pending,    TotalAmount = 1796.00m, Adults = 2, Children = 0, CreatedAt = today.AddDays(-1) },

            // Cancelled
            new Reservation { Id = 16, GuestId = 1,  RoomId = 2,  CheckInDate = today.AddDays(-15), CheckOutDate = today.AddDays(-12), Status = ReservationStatus.Cancelled,  TotalAmount = 447.00m, Adults = 2, Children = 0, CreatedAt = today.AddDays(-30) },
            new Reservation { Id = 17, GuestId = 5,  RoomId = 8,  CheckInDate = today.AddDays(4),   CheckOutDate = today.AddDays(7),   Status = ReservationStatus.Cancelled,  TotalAmount = 1497.00m, Adults = 1, Children = 0, CreatedAt = today.AddDays(-10) },

            // No-Show
            new Reservation { Id = 18, GuestId = 12, RoomId = 5,  CheckInDate = today.AddDays(-3),  CheckOutDate = today.AddDays(-1),  Status = ReservationStatus.NoShow,     TotalAmount = 398.00m, Adults = 1, Children = 0, CreatedAt = today.AddDays(-12) },

            // More future bookings
            new Reservation { Id = 19, GuestId = 2,  RoomId = 4,  CheckInDate = today.AddDays(14),  CheckOutDate = today.AddDays(17),  Status = ReservationStatus.Confirmed,  TotalAmount = 687.00m, Adults = 2, Children = 2, CreatedAt = today.AddDays(-3) },
            new Reservation { Id = 20, GuestId = 8,  RoomId = 10, CheckInDate = today.AddDays(20),  CheckOutDate = today.AddDays(25),  Status = ReservationStatus.Pending,    TotalAmount = 4995.00m, Adults = 2, Children = 0, CreatedAt = today }
        );
    }
}
