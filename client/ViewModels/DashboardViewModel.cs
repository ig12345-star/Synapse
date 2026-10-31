using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading; 
using client.Services;

namespace client.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly TcpNetworkService _networkService = new();
    
    private string _localId = "842 109 443";
    private string _targetId = string.Empty;
    private string _accessKey = string.Empty;
    private string _connectionStatus = "Ready to connect";
    private bool _requestElevation = true;
    private bool _clipboardSync = true;

    public string LocalId
    {
        get => _localId;
        set => SetField(ref _localId, value);
    }

    public string TargetId
    {
        get => _targetId;
        set => SetField(ref _targetId, value);
    }

    public string AccessKey
    {
        get => _accessKey;
        set => SetField(ref _accessKey, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        set => SetField(ref _connectionStatus, value);
    }

    public bool RequestElevation
    {
        get => _requestElevation;
        set => SetField(ref _requestElevation, value);
    }

    public bool ClipboardSync
    {
        get => _clipboardSync;
        set => SetField(ref _clipboardSync, value);
    }

    public ICommand ConnectCommand { get; }
    public ICommand CopyIdCommand { get; }

    public DashboardViewModel()
    {
        ConnectCommand = new RelayCommand(async () => await ExecuteConnectAsync());
        CopyIdCommand = new RelayCommand(ExecuteCopyId);

        _networkService.OnDataReceived += HandleDataReceived;
    }

    private async Task ExecuteConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetId))
        {
            ConnectionStatus = "Please enter a valid Target ID.";
            return;
        }

        ConnectionStatus = "Connecting to server (Port 5000)...";

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

    private void HandleDataReceived(byte[] data)
    {
        string receivedMessage = Encoding.UTF8.GetString(data);
        
        Dispatcher.UIThread.Post(() =>
        {
            ConnectionStatus = $"Msg from server: {receivedMessage}";
        });
    }

    private void ExecuteCopyId()
    {
        ConnectionStatus = "Local ID copied to clipboard!";
    }
}