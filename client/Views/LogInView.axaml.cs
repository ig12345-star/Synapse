using Avalonia.Controls;
using Avalonia.Interactivity;

namespace client.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void HideWindow_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.VisualRoot as Window;
        window?.Hide(); // Makes the window disappear instantly without closing the app process
    }
}