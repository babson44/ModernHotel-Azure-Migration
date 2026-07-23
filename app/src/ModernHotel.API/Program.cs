using Microsoft.EntityFrameworkCore;
using ModernHotel.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("HotelDb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__HotelDb")
    ?? "Server=localhost,1433;Database=GrandAzureHotel;User Id=sa;Password=Hotel@2025!;TrustServerCertificate=True;";

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Grand Azure Hotel API", Version = "v1",
        Description = "Hotel management API for the Grand Azure Hotel — Azure Migration Demo" });
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    var retries = 0;
    while (retries < 10)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch
        {
            retries++;
            Console.WriteLine($"Database not ready, retrying ({retries}/10)...");
            await Task.Delay(3000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Grand Azure Hotel API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
