using System;
using System.Net.Http;
using System.Threading;
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
        try
        {
            await App.Client!.Login(Username.Text!, Password.Text!);
            // TODO manage errors
            Next();
        }
        catch (HttpRequestException)
        {
            ErrorMsg.Text = "Unable to reach servers.";
        }
        catch (YaccException ye)
        {
            ErrorMsg.Text = ye.Message;
        }
    }

    private async void RegisterButtonClicked(object? sender, RoutedEventArgs e)
    {
        Connect();
        try
        {
            await App.Client!.Register(Username.Text!, Password.Text!);
            await App.Client!.Login(Username.Text!, Password.Text!);
            Next();
        }
        catch (HttpRequestException)
        {
            ErrorMsg.Text = "Unable to reach servers.";
        }
        catch (YaccException ye)
        {
            ErrorMsg.Text = ye.Message;
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