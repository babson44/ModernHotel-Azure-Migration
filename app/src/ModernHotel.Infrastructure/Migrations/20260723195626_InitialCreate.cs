using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ModernHotel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomNumber = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Floor = table.Column<int>(type: "int", nullable: false),
                    BedCount = table.Column<int>(type: "int", nullable: false),
                    MaxOccupancy = table.Column<int>(type: "int", nullable: false),
                    RatePerNight = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuestId = table.Column<int>(type: "int", nullable: false),
                    RoomId = table.Column<int>(type: "int", nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualCheckIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualCheckOut = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Guests_GuestId",
                        column: x => x.GuestId,
                        principalTable: "Guests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservations_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Guests",
                columns: new[] { "Id", "Address", "CreatedAt", "Email", "FirstName", "LastName", "Nationality", "Phone" },
                values: new object[,]
                {
                    { 1, "123 Oak Street, New York, NY", new DateTime(2024, 1, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "james.mitchell@email.com", "James", "Mitchell", "American", "+1-555-0101" },
                    { 2, "Storgatan 5, Stockholm", new DateTime(2024, 2, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "sofia.andersson@email.com", "Sofia", "Andersson", "Swedish", "+46-70-123456" },
                    { 3, "Calle Mayor 42, Madrid", new DateTime(2024, 2, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "carlos.rodriguez@email.com", "Carlos", "Rodriguez", "Spanish", "+34-91-234567" },
                    { 4, "1-2-3 Shinjuku, Tokyo", new DateTime(2024, 3, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "yuki.tanaka@email.com", "Yuki", "Tanaka", "Japanese", "+81-3-12345678" },
                    { 5, "15 Baker Street, London", new DateTime(2024, 3, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "emma.thompson@email.com", "Emma", "Thompson", "British", "+44-20-987654" },
                    { 6, "Via Roma 88, Milan", new DateTime(2024, 4, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "marco.ferrari@email.com", "Marco", "Ferrari", "Italian", "+39-02-345678" },
                    { 7, "DIFC Tower, Dubai", new DateTime(2024, 4, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), "aisha.khalil@email.com", "Aisha", "Khalil", "Emirati", "+971-50-123456" },
                    { 8, "500 Sand Hill Rd, Palo Alto", new DateTime(2024, 5, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "david.chen@email.com", "David", "Chen", "American", "+1-650-555-0199" },
                    { 9, "12 Rue de Rivoli, Paris", new DateTime(2024, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "marie.dubois@email.com", "Marie", "Dubois", "French", "+33-1-23456789" },
                    { 10, "Av Paulista 1500, São Paulo", new DateTime(2024, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), "lucas.oliveira@email.com", "Lucas", "Oliveira", "Brazilian", "+55-11-98765432" },
                    { 11, "MG Road, Bangalore", new DateTime(2024, 6, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "priya.sharma@email.com", "Priya", "Sharma", "Indian", "+91-98765-43210" },
                    { 12, "Tverskaya 10, Moscow", new DateTime(2024, 7, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "alex.petrov@email.com", "Alexander", "Petrov", "Russian", "+7-495-123456" },
                    { 13, "Kurfürstendamm 100, Berlin", new DateTime(2024, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "hannah.mueller@email.com", "Hannah", "Müller", "German", "+49-30-123456" },
                    { 14, "Tahrir Square 5, Cairo", new DateTime(2024, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "omar.hassan@email.com", "Omar", "Hassan", "Egyptian", "+20-2-12345678" },
                    { 15, "Piazza Navona 3, Rome", new DateTime(2024, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "isabella.rossi@email.com", "Isabella", "Rossi", "Italian", "+39-06-456789" }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "BedCount", "Description", "Floor", "IsActive", "MaxOccupancy", "RatePerNight", "RoomNumber", "Type" },
                values: new object[,]
                {
                    { 1, 1, "Cozy standard room with garden view", 1, true, 2, 129.00m, "101", 1 },
                    { 2, 2, "Spacious standard double room", 1, true, 4, 149.00m, "102", 1 },
                    { 3, 1, "Deluxe room with city view", 2, true, 2, 199.00m, "201", 2 },
                    { 4, 2, "Deluxe double room with balcony", 2, true, 4, 229.00m, "202", 2 },
                    { 5, 1, "Deluxe room with pool view", 2, true, 2, 199.00m, "203", 2 },
                    { 6, 1, "Junior suite with separate living area", 3, true, 3, 349.00m, "301", 3 },
                    { 7, 2, "Executive suite with panoramic view", 3, true, 4, 449.00m, "302", 3 },
                    { 8, 2, "Presidential suite with private terrace", 3, true, 4, 499.00m, "303", 3 },
                    { 9, 2, "Penthouse with full-floor panoramic views", 4, true, 4, 799.00m, "401", 4 },
                    { 10, 3, "Grand penthouse with private pool access", 4, true, 6, 999.00m, "402", 4 }
                });

            migrationBuilder.InsertData(
                table: "Reservations",
                columns: new[] { "Id", "ActualCheckIn", "ActualCheckOut", "Adults", "CheckInDate", "CheckOutDate", "Children", "CreatedAt", "GuestId", "RoomId", "SpecialRequests", "Status", "TotalAmount" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, new DateTime(2025, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 3, null, 4, 597.00m },
                    { 2, new DateTime(2025, 7, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2025, 7, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 6, null, 4, 1047.00m },
                    { 3, new DateTime(2025, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2025, 7, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, 1, null, 4, 258.00m },
                    { 4, new DateTime(2025, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, new DateTime(2025, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2025, 7, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 9, 9, null, 4, 2397.00m },
                    { 5, new DateTime(2025, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2, new DateTime(2025, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, new DateTime(2025, 7, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 4, null, 3, 916.00m },
                    { 6, new DateTime(2025, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2, new DateTime(2025, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, 7, null, 3, 1796.00m },
                    { 7, new DateTime(2025, 7, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2, new DateTime(2025, 7, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, new DateTime(2025, 6, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), 7, 10, null, 3, 3996.00m },
                    { 8, new DateTime(2025, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 1, new DateTime(2025, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, 5, null, 3, 796.00m },
                    { 9, null, null, 2, new DateTime(2025, 7, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 6, 8, null, 2, 1497.00m },
                    { 10, null, null, 2, new DateTime(2025, 7, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, new DateTime(2025, 7, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), 11, 2, null, 2, 298.00m },
                    { 11, null, null, 2, new DateTime(2025, 7, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 10, 6, null, 2, 1047.00m },
                    { 12, null, null, 1, new DateTime(2025, 7, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), 12, 3, null, 2, 597.00m },
                    { 13, null, null, 2, new DateTime(2025, 7, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 13, 9, null, 2, 3196.00m },
                    { 14, null, null, 1, new DateTime(2025, 7, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), 14, 1, null, 1, 387.00m },
                    { 15, null, null, 2, new DateTime(2025, 8, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), 15, 7, null, 1, 1796.00m },
                    { 16, null, null, 2, new DateTime(2025, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 6, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 2, null, 5, 447.00m },
                    { 17, null, null, 1, new DateTime(2025, 7, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), 5, 8, null, 5, 1497.00m },
                    { 18, null, null, 1, new DateTime(2025, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 12, 5, null, 6, 398.00m },
                    { 19, null, null, 2, new DateTime(2025, 8, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, new DateTime(2025, 7, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 4, null, 2, 687.00m },
                    { 20, null, null, 2, new DateTime(2025, 8, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 17, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, new DateTime(2025, 7, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), 8, 10, null, 1, 4995.00m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guests_Email",
                table: "Guests",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_GuestId",
                table: "Reservations",
                column: "GuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RoomId",
                table: "Reservations",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomNumber",
                table: "Rooms",
                column: "RoomNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Guests");

            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
