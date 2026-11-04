using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading; 
using client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace client.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly TcpNetworkService _networkService;
    
    [ObservableProperty]
    private string _localId = "842 109 443";
    
    [ObservableProperty]
    private string _targetId = string.Empty;
    
    [ObservableProperty]
    private string _accessKey = string.Empty;
    
    [ObservableProperty]
    private string _connectionStatus = "Ready to connect";
    
    [ObservableProperty]
    private bool _requestElevation = true;
    
    [ObservableProperty]
    private bool _clipboardSync = true;

    // קבלת שירות הרשת דרך הבנאי
    public DashboardViewModel(TcpNetworkService networkService)
    {
        _networkService = networkService;
        _networkService.OnDataReceived += HandleDataReceived;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetId))
        {
            ConnectionStatus = "Please enter a valid Target ID.";
            return;
        }

        ConnectionStatus = "Connecting to server (Port 5000)...";

        // בודק אם אנחנו כבר מחוברים (מהלוגין). אם לא, מנסה להתחבר.
        // כדאי להוסיף מאפיין IsConnected ב-TcpNetworkService בהמשך, אבל בינתיים ננסה להתחבר
        bool isConnected = await _networkService.ConnectAsync("127.0.0.1", 5000);

        if (isConnected)
        {
            ConnectionStatus = $"Connected via TCP to node {TargetId}!";
            
            string testMessage = $"Hello from Client {LocalId} trying to reach {TargetId}!";
            byte[] data = Encoding.UTF8.GetBytes(testMessage);
            await _networkService.SendAsync(data);
        }
        else
        {
            ConnectionStatus = "Connection failed. Is the server running?";
        }
    }

    [RelayCommand]
    private void CopyId()
    {
        ConnectionStatus = "Local ID copied to clipboard!";
    }

    private void HandleDataReceived(byte[] data)
    {
        string receivedMessage = Encoding.UTF8.GetString(data);
        
        Dispatcher.UIThread.Post(() =>
        {
            ConnectionStatus = $"Msg from server: {receivedMessage}";
        });
    }
}