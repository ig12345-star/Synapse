using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Data;
using SharedModels; // מושך את מחלקת ה-TcpMessage שיצרנו

namespace Server.Services;

public class TcpRelayService : BackgroundService
{
    private readonly ILogger<TcpRelayService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TcpRelayService(ILogger<TcpRelayService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, 5000);
        listener.Start();
        _logger.LogInformation("TCP Server is listening on port 5000...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[4096];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) break;

                    string jsonString = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var request = JsonSerializer.Deserialize<TcpMessage>(jsonString);
                    
                    if (request != null)
                    {
                        var response = await ProcessAuthRequestAsync(request);
                        string responseJson = JsonSerializer.Serialize(response);
                        byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Client error: {ex.Message}");
            }
        }
    }

    private async Task<TcpMessage> ProcessAuthRequestAsync(TcpMessage request)
    {
        var response = new TcpMessage { Action = "Response" };
        var passwordHasher = new PasswordHasher<UserEntity>();

        // פתיחת גישה למסד הנתונים בצורה בטוחה מתוך תהליך רקע
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (request.Action == "Register")
        {
            if (await db.Users.AnyAsync(u => u.Username == request.Username))
            {
                response.Success = false;
                response.MessageText = "User already exists.";
                return response;
            }

            var newUser = new UserEntity { Username = request.Username };
            newUser.PasswordHash = passwordHasher.HashPassword(newUser, request.Password);
            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            response.Success = true;
            response.MessageText = "Account created successfully!";
        }
        else if (request.Action == "Login")
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user != null && passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Success)
            {
                response.Success = true;
                response.MessageText = "Login successful!";
            }
            else
            {
                response.Success = false;
                response.MessageText = "Invalid username or password.";
            }
        }

        return response;
    }
}