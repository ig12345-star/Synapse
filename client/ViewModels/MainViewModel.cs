using ReactiveUI;
using client.ViewModels;

namespace client.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetField(ref _currentViewModel, value);
    }

    public MainViewModel()
    {
        CurrentViewModel = new LoginViewModel();
    }
}