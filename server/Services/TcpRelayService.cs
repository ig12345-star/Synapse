using System.Collections.Concurrent;
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
// ודא שה-namespace של UserEntity מופיע כאן אם הוא שונה, למשל:
// using Server.Models; 
using SharedModels;

namespace Server.Services;

public class TcpRelayService : BackgroundService
{
    private readonly ILogger<TcpRelayService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    
    // המילון עדיין בזיכרון כי חיבורי TCP הם זמניים, אבל עכשיו המפתח הוא המספר הקבוע
    private static readonly ConcurrentDictionary<string, TcpClient> _activeConnections = new();

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

    // פונקציית עזר ליצירת קוד ייחודי מול ה-Database
    private async Task<string> GenerateUniquePermanentIdAsync(AppDbContext db)
    {
        string newId;
        bool exists;
        do
        {
            newId = Random.Shared.Next(100000000, 999999999).ToString("000 000 000");
            // מוודא שהמספר לא שמור לאף משתמש אחר ב-Database
            exists = await db.Users.AnyAsync(u => u.PermanentId == newId);
        } 
        while (exists); 
        
        return newId;
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        string? currentSessionId = null; 

        using (client)
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[8192];

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
                        if (request.Action == "RequestRemoteConnection")
                        {
                            await HandleRoutingAsync(request, client, currentSessionId, cancellationToken);
                            continue; 
                        }

                        var authResult = await ProcessAuthRequestAsync(request);
                        var response = authResult.Response;
                        
                        if (response.Success && request.Action == "Login")
                        {
                            currentSessionId = authResult.PermanentId;
                            _activeConnections.TryAdd(currentSessionId, client);
                            _logger.LogInformation($"User {request.Username} logged in. Permanent ID: {currentSessionId}");
                        }

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
            finally
            {
                if (currentSessionId != null)
                {
                    _activeConnections.TryRemove(currentSessionId, out _);
                    _logger.LogInformation($"ID {currentSessionId} disconnected from routing table.");
                }
            }
        }
    }

    private async Task HandleRoutingAsync(TcpMessage request, TcpClient senderClient, string? senderSessionId, CancellationToken cancellationToken)
    {
        string targetSessionId = request.MessageText; 
        
        if (_activeConnections.TryGetValue(targetSessionId, out var targetClient))
        {
            try
            {
                var relayMessage = new TcpMessage
                {
                    Action = "IncomingConnection",
                    Username = senderSessionId ?? "Unknown", 
                    MessageText = $"User with ID {senderSessionId} is ringing you..."
                };

                byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(relayMessage));
                await targetClient.GetStream().WriteAsync(data, 0, data.Length, cancellationToken);
                
                var ackMessage = new TcpMessage { Action = "Response", Success = true, MessageText = $"Ringing device {targetSessionId}..." };
                byte[] ackData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ackMessage));
                await senderClient.GetStream().WriteAsync(ackData, 0, ackData.Length, cancellationToken);
            }
            catch
            {
                _activeConnections.TryRemove(targetSessionId, out _);
            }
        }
        else
        {
            var failMessage = new TcpMessage { Action = "Response", Success = false, MessageText = $"Device ID {targetSessionId} is offline." };
            byte[] failData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(failMessage));
            await senderClient.GetStream().WriteAsync(failData, 0, failData.Length, cancellationToken);
        }
    }

    private async Task<(TcpMessage Response, string PermanentId)> ProcessAuthRequestAsync(TcpMessage request)
    {
        string permanentId = string.Empty;
        var response = new TcpMessage { Action = "Response" };
        var passwordHasher = new PasswordHasher<UserEntity>();

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // אם המשתמש נרשם - מייצרים לו מספר ושומרים אותו בדאטה בייס לכל החיים
        if (request.Action == "Register")
        {
            if (await db.Users.AnyAsync(u => u.Username == request.Username))
            {
                response.Success = false;
                response.MessageText = "User already exists.";
                return (response, permanentId);
            }

            var newUser = new UserEntity { Username = request.Username };
            newUser.PasswordHash = passwordHasher.HashPassword(newUser, request.Password);
            
            // הגרלת המספר הקבוע לפני השמירה
            newUser.PermanentId = await GenerateUniquePermanentIdAsync(db);
            
            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            response.Success = true;
            response.MessageText = "Account created successfully!";
        }
        // אם המשתמש מתחבר - שולפים את המספר הישן שלו מהדאטה בייס
        else if (request.Action == "Login")
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user != null && passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Success)
            {
                response.Success = true;
                
                // משיכת המספר הקיים במקום לייצר אחד חדש
                permanentId = user.PermanentId;
                response.MessageText = permanentId; 
            }
            else
            {
                response.Success = false;
                response.MessageText = "Invalid username or password.";
            }
        }

        return (response, permanentId);
    }
}