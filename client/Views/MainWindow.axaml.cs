using Avalonia.Controls;
using client.ViewModels;

namespace client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var mainViewModel = new MainViewModel();

        mainViewModel.RequestMaximizeWindow += () =>
        {
            // מגדיל את חלון הדאשבורד למסך מלא, תוך שמירה על כפתור ה-X למעלה
            WindowState = WindowState.Maximized; 
        };

        DataContext = mainViewModel;
    }
}