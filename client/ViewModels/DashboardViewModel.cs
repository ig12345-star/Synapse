using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading; 
using client.Services;
using SharedModels;
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

        ConnectionStatus = $"Contacting node {TargetId}...";

        try
        {
            var message = new TcpMessage
            {
                Action = "RequestRemoteConnection",
                Username = LocalId, 
                MessageText = TargetId 
            };

            string json = JsonSerializer.Serialize(message);
            byte[] data = Encoding.UTF8.GetBytes(json);
            
            await _networkService.SendAsync(data);
            
            ConnectionStatus = $"Connection request sent for {TargetId}!";
        }
        catch (Exception)
        {
            ConnectionStatus = "Network error. Connection to server lost.";
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