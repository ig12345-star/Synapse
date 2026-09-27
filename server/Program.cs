using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Register SQLite Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=synapse.db"));

var app = builder.Build();

// Automatically create the database file on startup if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Initialize the built-in ASP.NET Core password hasher
var passwordHasher = new PasswordHasher<UserEntity>();

// 1. Register Endpoint (with Hashing)
app.MapPost("/api/auth/register", async (UserDto dto, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        return Results.BadRequest(new { message = "Fields cannot be empty." });

    if (await db.Users.AnyAsync(u => u.Username == dto.Username))
        return Results.BadRequest(new { message = "User already exists." });

    var newUser = new UserEntity { Username = dto.Username };
    
    // Hash the password securely
    newUser.PasswordHash = passwordHasher.HashPassword(newUser, dto.Password);

    db.Users.Add(newUser);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Account created successfully!" });
});

// 2. Login Endpoint (with Password Verification)
app.MapPost("/api/auth/login", async (UserDto dto, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
    
    if (user == null)
        return Results.BadRequest(new { message = "Invalid username or password." });

    // Verify the submitted password against the stored secure hash
    var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

    if (verificationResult == PasswordVerificationResult.Success)
    {
        return Results.Ok(new { message = "Login successful!", token = "mock-jwt-token" });
    }
    
    return Results.BadRequest(new { message = "Invalid username or password." });
});

app.Run();

public record UserDto(string Username, string Password);