using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ConsoleClient;

namespace ClientApp.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void LoginButtonClicked(object? sender, RoutedEventArgs e)
    {
        Connect();
        bool success = await App.Client!.Login(Username.Text!, Password.Text!);
        // TODO manage errors
        if(success)
            Next();
    }

    private async void RegisterButtonClicked(object? sender, RoutedEventArgs e)
    {
        Connect();
        await App.Client!.Register(Username.Text!, Password.Text!);
        bool success = await App.Client!.Login(Username.Text!, Password.Text!);
        if(success)
            Next();
    }

    private void Next()
    {
        App.LoggedIn?.Invoke();
    }

    private void Connect()
    {
        try
        {
            App.Client = new(HostBox.Text!);
        }
        catch (Exception e)
        {
            // TODO
        }
    }
}