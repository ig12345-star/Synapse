using System.Windows.Input;

namespace client.ViewModels;

public class DashboardViewModel : ViewModelBase
{
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
        ConnectCommand = new RelayCommand(ExecuteConnect);
        CopyIdCommand = new RelayCommand(ExecuteCopyId);
    }

    private void ExecuteConnect()
    {
        if (string.IsNullOrWhiteSpace(TargetId))
        {
            ConnectionStatus = "Please enter a valid Target ID.";
            return;
        }

        ConnectionStatus = $"Connecting via WebRTC P2P to node {TargetId}...";
    }

    private void ExecuteCopyId()
    {
        ConnectionStatus = "Local ID copied to clipboard!";
    }
}