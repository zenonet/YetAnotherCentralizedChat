using Avalonia.Controls;
using Avalonia.Interactivity;
using ClientApp.ViewModels;

namespace ClientApp.Views;

public partial class MainView : UserControl
{
    private MainViewModel Model => (MainViewModel) DataContext!;
    public MainView()
    {
        InitializeComponent();
        App.LoggedIn += () =>
        {
            Model.CarouselPage = 1;
        };
    }

    private async void ChatOpenButtonClicked(object? sender, RoutedEventArgs e)
    {
        ChatViewModel chat = new()
        {
            ChatName = otherUserField.Text!,
            Messages = new((await App.Client!.GetAllMessagesWithUser(otherUserField.Text!))!),
            CloseChat = () =>
            {
                Model.CarouselPage = 1;
            },
        };
        string other = otherUserField.Text!;
        App.MessageReceived += msg =>
        {
            if (msg.SenderName == other)
            {
                chat.Messages.Add(msg);
            }
        };
        Model.OpenedChat = chat;
        Model.CarouselPage = 2;
        /*
        this.DockPanel.Children.Add(new ChatView
        {
            DataContext = chat,
        });*/

    }
}