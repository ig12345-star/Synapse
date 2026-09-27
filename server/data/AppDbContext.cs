using Microsoft.EntityFrameworkCore;

namespace Server.Data;

public class AppDbContext : DbContext
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}

public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // Store hashed passwords in production
}