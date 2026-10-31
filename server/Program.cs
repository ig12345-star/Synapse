using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services; 

var builder = WebApplication.CreateBuilder(args);

// Register SQLite Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=synapse.db"));

// הוספה: רישום ה-TCP Relay Service כדי שירוץ ברקע 
builder.Services.AddHostedService<TcpRelayService>();

var app = builder.Build();

// Automatically create the database file on startup if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();