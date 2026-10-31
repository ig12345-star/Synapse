
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using client.Services;
using SharedModels; // מודל ההודעה המשותף

namespace client.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly TcpNetworkService _networkService = new();
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isSignUpMode;

    public event Action? OnLoginSuccess;

    public string Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetField(ref _confirmPassword, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            SetField(ref _statusMessage, value);
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public bool IsSignUpMode
    {
        get => _isSignUpMode;
        set
        {
            SetField(ref _isSignUpMode, value);
            OnPropertyChanged(nameof(ActionbuttonText));
            OnPropertyChanged(nameof(ToggleModeText));
            OnPropertyChanged(nameof(SwitchPromptText));
            StatusMessage = string.Empty;
            ConfirmPassword = string.Empty;
        }
    }

    public string ActionbuttonText => IsSignUpMode ? "CREATE ACCOUNT" : "LOG IN";
    public string ToggleModeText => IsSignUpMode ? "Log In" : "Create Account";
    public string SwitchPromptText => IsSignUpMode ? "Already have an account?" : "New to Synapse?";

    public ICommand SubmitCommand { get; }
    public ICommand ToggleModeCommand { get; }

    public LoginViewModel()
    {
        SubmitCommand = new RelayCommand(async () => await ExecuteSubmitAsync());
        ToggleModeCommand = new RelayCommand(() => IsSignUpMode = !IsSignUpMode);
        
        // הרשמה לקבלת תשובות מהשרת
        _networkService.OnDataReceived += HandleServerResponse;
    }

    private async Task ExecuteSubmitAsync()
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

        // בניית הודעת ה-TCP
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

    private void HandleServerResponse(byte[] data)
    {
        string jsonString = Encoding.UTF8.GetString(data);
        var response = JsonSerializer.Deserialize<TcpMessage>(jsonString);

        if (response != null && response.Action == "Response")
        {
            // עדכון הממשק חייב לקרות ב-UI Thread
            Dispatcher.UIThread.Post(() =>
            {
                StatusMessage = response.MessageText;

                if (response.Success)
                {
                    _networkService.Disconnect(); 
                    OnLoginSuccess?.Invoke(); // מעבר למסך ה-Dashboard
                }
            });
        }
    }
}