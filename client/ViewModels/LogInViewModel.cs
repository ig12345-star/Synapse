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

public partial class LoginViewModel : ViewModelBase
{
    private readonly TcpNetworkService _networkService;

    public LoginViewModel(TcpNetworkService networkService)
    {
        _networkService = networkService; // קבלת השירות המשותף מ-MainViewModel
        _networkService.OnDataReceived += HandleServerResponse;
    }

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActionbuttonText))]
    [NotifyPropertyChangedFor(nameof(ToggleModeText))]
    [NotifyPropertyChangedFor(nameof(SwitchPromptText))]
    private bool _isSignUpMode;

    public event Action? OnLoginSuccess;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public string ActionbuttonText => IsSignUpMode ? "CREATE ACCOUNT" : "LOG IN";
    public string ToggleModeText => IsSignUpMode ? "Log In" : "Create Account";
    public string SwitchPromptText => IsSignUpMode ? "Already have an account?" : "New to Synapse?";

    public LoginViewModel()
    {
        _networkService.OnDataReceived += HandleServerResponse;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Please fill in all required fields.";
            return;
        }

        if (IsSignUpMode && Password != ConfirmPassword)
        {
            StatusMessage = "Passwords do not match. Please re-enter.";
            return;
        }

        StatusMessage = "Connecting to TCP Server...";
        bool isConnected = await _networkService.ConnectAsync("127.0.0.1", 5000);
        
        if (!isConnected)
        {
            StatusMessage = "Error: Could not connect to the server.";
            return;
        }

        StatusMessage = IsSignUpMode ? "Creating account..." : "Authenticating...";

        var message = new TcpMessage
        {
            Action = IsSignUpMode ? "Register" : "Login",
            Username = Username,
            Password = Password
        };

        string json = JsonSerializer.Serialize(message);
        byte[] data = Encoding.UTF8.GetBytes(json);
        await _networkService.SendAsync(data);
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsSignUpMode = !IsSignUpMode;
        StatusMessage = string.Empty;
        ConfirmPassword = string.Empty;
    }

    private void HandleServerResponse(byte[] data)
    {
        string jsonString = Encoding.UTF8.GetString(data);
        var response = JsonSerializer.Deserialize<TcpMessage>(jsonString);

        if (response != null && response.Action == "Response")
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusMessage = response.MessageText;
                if (response.Success)
                {
                    _networkService.Disconnect(); 
                    OnLoginSuccess?.Invoke();
                }
            });
        }
    }
}