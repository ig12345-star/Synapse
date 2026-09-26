using Avalonia.Controls;
using client.ViewModels;

namespace client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}