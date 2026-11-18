using System;
using CommunityToolkit.Mvvm.ComponentModel;
using client.Services;

namespace client.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    private readonly LoginViewModel _loginViewModel;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly TcpNetworkService _networkService;

    public event Action? RequestMaximizeWindow;

    public MainViewModel()
    {
        _networkService = new TcpNetworkService();
        _loginViewModel = new LoginViewModel(_networkService);
        _dashboardViewModel = new DashboardViewModel(_networkService);

        _loginViewModel.LoginSuccess += () =>
        {
            // הפקודה הקריטית: שואבת את המספר שהרגע התקבל מהשרת!
            _dashboardViewModel.InitializeSession();
            
            CurrentViewModel = _dashboardViewModel;
            RequestMaximizeWindow?.Invoke();
        };

        _currentViewModel = _loginViewModel;
    }
}