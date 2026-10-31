using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace client.Services;

public class TcpNetworkService
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cancellationTokenSource;

    // אירוע שמופעל כשהשרת שולח לנו נתונים
    public event Action<byte[]>? OnDataReceived;

    public async Task<bool> ConnectAsync(string ipAddress, int port)
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(ipAddress, port);
            _stream = _tcpClient.GetStream();
            
            _cancellationTokenSource = new CancellationTokenSource();
            _ = StartListeningAsync(_cancellationTokenSource.Token);
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task SendAsync(byte[] data)
    {
        if (_stream != null && _stream.CanWrite)
        {
            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }
    }

    private async Task StartListeningAsync(CancellationToken token)
    {
        if (_stream == null) return;
        byte[] buffer = new byte[8192]; 

        try
        {
            while (!token.IsCancellationRequested)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0) break; 

                byte[] receivedData = new byte[bytesRead];
                Array.Copy(buffer, receivedData, bytesRead);
                OnDataReceived?.Invoke(receivedData);
            }
        }
        catch (Exception)
        {
            // שגיאת רשת או התנתקות
        }
        finally
        {
            Disconnect();
        }
    }

    public void Disconnect()
    {
        _cancellationTokenSource?.Cancel();
        _stream?.Close();
        _tcpClient?.Close();
    }
}