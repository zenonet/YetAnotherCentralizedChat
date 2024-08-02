using System;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
        try
        {
            bool success = await App.Client!.Login(Username.Text!, Password.Text!);
            // TODO manage errors
            if (success)
                Next();
            else
                ErrorMsg.Text = "An error occured while logging you in. Your credentials are probably invalid.";
        }
        catch (HttpRequestException)
        {
            ErrorMsg.Text = "Unable to reach servers.";
        }
    }

    private async void RegisterButtonClicked(object? sender, RoutedEventArgs e)
    {
        Connect();
        try
        {
            bool success = await App.Client!.Register(Username.Text!, Password.Text!);
            success &= await App.Client!.Login(Username.Text!, Password.Text!);

            if (success)
                Next();
            else
                ErrorMsg.Text = "An error occured. The username you selected is probably taken.";
        }
        catch (HttpRequestException)
        {
            ErrorMsg.Text = "Unable to reach servers.";
        }
    }

    private void Next()
    {
        App.Client!.StartLongPollingConnection(msg => { App.MessageReceived?.Invoke(msg); }, CancellationToken.None);
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