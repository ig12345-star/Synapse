using System;

namespace client.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    
    public event Action? RequestMaximizeWindow;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetField(ref _currentViewModel, value);
    }

    public MainViewModel()
    {
        var loginVM = new LoginViewModel();
        
        loginVM.OnLoginSuccess += () =>
        {
            CurrentViewModel = new DashboardViewModel();
            RequestMaximizeWindow?.Invoke(); // Tell the window to go full screen now
        };

        _currentViewModel = loginVM;
    }
}