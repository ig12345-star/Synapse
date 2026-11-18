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
    public Action? LoginSuccess;

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
    [NotifyPropertyChangedFor(nameof(ActionButtonText))]
    [NotifyPropertyChangedFor(nameof(SwitchPromptText))]
    [NotifyPropertyChangedFor(nameof(ToggleModeText))]
    private bool _isSignUpMode = false;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);
    public string ActionButtonText => IsSignUpMode ? "CREATE ACCOUNT" : "SIGN IN";
    public string SwitchPromptText => IsSignUpMode ? "ALREADY HAVE AN ACCOUNT?" : "DON'T HAVE AN ACCOUNT?";
    public string ToggleModeText => IsSignUpMode ? "SIGN IN" : "SIGN UP";

    public LoginViewModel(TcpNetworkService networkService)
    {
        _networkService = networkService;
        _networkService.OnDataReceived += HandleDataReceived;
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsSignUpMode = !IsSignUpMode;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "Please enter username and password.";
            return;
        }

        if (IsSignUpMode)
        {
            if (Password != ConfirmPassword)
            {
                StatusMessage = "Passwords do not match.";
                return;
            }
            StatusMessage = "Registering...";
        }
        else
        {
            StatusMessage = "Connecting...";
        }

        try
        {
            await _networkService.ConnectAsync("127.0.0.1", 5000);
            
            var action = IsSignUpMode ? "Register" : "Login";
            var message = new TcpMessage { Action = action, Username = Username, Password = Password };
            
            string json = JsonSerializer.Serialize(message);
            await _networkService.SendAsync(Encoding.UTF8.GetBytes(json));
        }
        catch
        {
            StatusMessage = "Connection failed. Is the server running?";
        }
    }

    private void HandleDataReceived(byte[] data)
    {
        string jsonString = Encoding.UTF8.GetString(data);
        
        try
        {
            var response = JsonSerializer.Deserialize<TcpMessage>(jsonString);
            if (response != null && response.Action == "Response")
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (response.Success)
                    {
                        if (!IsSignUpMode)
                        {
                            _networkService.LocalSessionId = response.MessageText;
                            _networkService.OnDataReceived -= HandleDataReceived;
                            LoginSuccess?.Invoke();
                        }
                        else
                        {
                            StatusMessage = response.MessageText; 
                            ToggleMode(); 
                        }
                    }
                    else
                    {
                        StatusMessage = response.MessageText;
                    }
                });
            }
        }
        catch
        {
            Dispatcher.UIThread.Post(() => StatusMessage = "Error communicating with server.");
        }
    }
}