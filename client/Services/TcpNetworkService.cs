using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.Services;

public class TcpNetworkService
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    // המשתנה החדש ששומר את הקוד הייחודי שהשרת נתן לנו (לדוגמה: "123 456 789")
    public string LocalSessionId { get; set; } = string.Empty;

    public event Action<byte[]>? OnDataReceived;

    public async Task ConnectAsync(string ipAddress, int port)
    {
        if (_client != null && _client.Connected)
            return;

        _client = new TcpClient();
        await _client.ConnectAsync(ipAddress, port);
        _stream = _client.GetStream();

        // מתחיל את לולאת ההאזנה להודעות מהשרת ברקע
        _ = Task.Run(ReceiveLoopAsync);
    }

    public async Task SendAsync(byte[] data)
    {
        if (_stream != null && _client != null && _client.Connected)
        {
            await _stream.WriteAsync(data, 0, data.Length);
        }
        else
        {
            throw new InvalidOperationException("Not connected to the server.");
        }
    }

    private async Task ReceiveLoopAsync()
    {
        if (_stream == null) return;

        byte[] buffer = new byte[8192];

        try
        {
            while (true)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    // השרת ניתק את החיבור
                    break;
                }

                byte[] receivedData = new byte[bytesRead];
                Array.Copy(buffer, receivedData, bytesRead);

                // מעביר את המידע ל-ViewModels שנרשמו לאירוע
                OnDataReceived?.Invoke(receivedData);
            }
        }
        catch (Exception)
        {
            // טיפול בשגיאת רשת (למשל סגירת תוכנה פתאומית)
        }
        finally
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
        _stream?.Close();
        _client?.Close();
    }
}