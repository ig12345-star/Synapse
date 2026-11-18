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
    private string _localId = string.Empty; 
    
    [ObservableProperty]
    private string _targetId = string.Empty;
    
    [ObservableProperty]
    private string _connectionStatus = "Ready to connect";
    
    [ObservableProperty]
    private bool _requestElevation = true;
    
    [ObservableProperty]
    private bool _clipboardSync = true;

    [ObservableProperty]
    private bool _isSettingsOpen = false;

    public DashboardViewModel(TcpNetworkService networkService)
    {
        _networkService = networkService;
        _networkService.OnDataReceived += HandleDataReceived;
    }

    public void InitializeSession()
    {
        LocalId = _networkService.LocalSessionId;
        // מאפס את ההודעה למטה בכל פעם שנכנסים
        ConnectionStatus = "Ready to connect";
    }

    [RelayCommand]
    private void ShowHome()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        IsSettingsOpen = true;
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
        string jsonString = Encoding.UTF8.GetString(data);
        
        try
        {
            var message = JsonSerializer.Deserialize<TcpMessage>(jsonString);
            
            if (message != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (message.Action == "IncomingConnection")
                    {
                        ConnectionStatus = $"📞 {message.MessageText}";
                    }
                    else if (message.Action == "Response")
                    {
                        // כאן התיקון: אנחנו מסננים החוצה את ההודעה שמכילה את המספר שלנו!
                        if (message.MessageText == LocalId || message.MessageText == _networkService.LocalSessionId)
                        {
                            ConnectionStatus = "Ready to connect";
                            return;
                        }
                        
                        ConnectionStatus = message.Success ? $"✅ {message.MessageText}" : $"❌ {message.MessageText}";
                    }
                });
            }
        }
        catch
        {
            Dispatcher.UIThread.Post(() =>
            {
                ConnectionStatus = $"Raw data: {jsonString}";
            });
        }
    }
}