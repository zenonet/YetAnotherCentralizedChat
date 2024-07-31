using Avalonia.Controls;

namespace ClientApp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        App.LoggedIn += (() =>
        {
            LoginView.IsVisible = false;
        });
    }
}