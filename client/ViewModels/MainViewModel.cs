using System;
using client.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace client.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // יצירת המופע המשותף שיחזיק את חיבור ה-TCP פעיל לאורך כל חדר האפליקציה
    private readonly TcpNetworkService _sharedNetworkService;
    
    [ObservableProperty]
    private ViewModelBase _currentViewModel;
    
    public event Action? RequestMaximizeWindow;

    public MainViewModel()
    {
        // אתחול שירות הרשת פעם אחת בלבד
        _sharedNetworkService = new TcpNetworkService();

        // העברת השירות המשותף למסך ההתחברות
        var loginVM = new LoginViewModel(_sharedNetworkService);
        
        loginVM.OnLoginSuccess += () =>
        {
            // העברת אותו השירות למסך ה-Dashboard
            CurrentViewModel = new DashboardViewModel(_sharedNetworkService);
            RequestMaximizeWindow?.Invoke();
        };

        _currentViewModel = loginVM;
    }
}