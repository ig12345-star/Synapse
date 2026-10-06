using Avalonia.Controls;
using client.ViewModels;

namespace client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var mainViewModel = new MainViewModel();

        // Listen for the view model telling us login succeeded
        mainViewModel.RequestMaximizeWindow += () =>
        {
            // Clear any starting bounds
            Width = double.NaN;
            Height = double.NaN;
            MinWidth = 0;
            MinHeight = 0;
            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;

            // Force full screen / maximized state
            WindowState = WindowState.Maximized;
        };

        DataContext = mainViewModel;
    }
}