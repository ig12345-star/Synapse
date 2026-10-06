using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Input;

namespace client.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isSignUpMode;

    private readonly string _serverBaseUrl = "http://localhost:5146";

    // Event to notify the window to switch to the dashboard
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
        SubmitCommand = new RelayCommand(ExecuteSubmit);
        ToggleModeCommand = new RelayCommand(() => IsSignUpMode = !IsSignUpMode);
    }

    private async void ExecuteSubmit()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Please fill in all required fields.";
                return;
            }

            using var httpClient = new HttpClient();

            if (IsSignUpMode)
            {
                if (Password != ConfirmPassword)
                {
                    StatusMessage = "Passwords do not match. Please re-enter.";
                    return;
                }

                StatusMessage = "Connecting to server...";

                var response = await httpClient.PostAsJsonAsync($"{_serverBaseUrl}/api/auth/register", new
                {
                    Username = Username,
                    Password = Password
                });

                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = "Account successfully created!";
                    OnLoginSuccess?.Invoke(); // Triggers the dashboard transition
                }
                else
                {
                    StatusMessage = "Registration failed. User may already exist.";
                }
            }
            else
            {
                StatusMessage = "Authenticating...";

                var response = await httpClient.PostAsJsonAsync($"{_serverBaseUrl}/api/auth/login", new
                {
                    Username = Username,
                    Password = Password
                });

                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = "Login successful!";
                    OnLoginSuccess?.Invoke(); // Triggers the dashboard transition
                }
                else
                {
                    StatusMessage = "Invalid username or password.";
                }
            }
        }
        catch (Exception)
        {
            StatusMessage = "Error: Could not connect to the server.";
        }
    }
}