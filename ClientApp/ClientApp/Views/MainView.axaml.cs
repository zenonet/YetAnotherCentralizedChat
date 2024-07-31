using Avalonia.Controls;
using Avalonia.Interactivity;
using ClientApp.ViewModels;

namespace ClientApp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        App.LoggedIn += (() =>
        {
            LoginView.IsVisible = false;
            MainLayout.IsVisible = true;
        });
    }

    private async void ChatOpenButtonClicked(object? sender, RoutedEventArgs e)
    {
        ChatViewModel chat = new()
        {
            ChatName = otherUserField.Text!,
            Messages = new((await App.Client!.GetAllMessagesWithUser(otherUserField.Text!))!),
        };
        string other = otherUserField.Text!;
        App.MessageReceived += msg =>
        {
            if (msg.SenderName == other)
            {
                chat.Messages.Add(msg);
            }
        };
        this.DockPanel.Children.Add(new ChatView
        {
            DataContext = chat,
        });

    }
}