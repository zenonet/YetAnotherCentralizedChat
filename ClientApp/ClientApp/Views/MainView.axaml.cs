using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClientApp.ViewModels;

namespace ClientApp.Views;

public partial class MainView : UserControl
{
    private MainViewModel Model => (MainViewModel) DataContext!;

    public MainView()
    {
        InitializeComponent();
        App.LoggedIn += async () =>
        {
            Model.CarouselPage = 1;
            // To prevent the login view from yoinking the enter-press
            LoginView.IsVisible = false;
            Model.Conversations = new(await App.Client?.GetConversationPartners()! ?? []);
        };

        App.MessageReceived += msg => Dispatcher.UIThread.Invoke(() =>
        {
            if (!Model.Conversations.Contains(msg.SenderName))
            {
                Model.Conversations.Add(msg.SenderName);
            }
        });
    }

    private async void ChatOpenButtonClicked(object? sender, RoutedEventArgs e)
    {
        await OpenChat(otherUserField.Text!);
    }

    private async Task OpenChat(string name)
    {
        ChatViewModel chat = new()
        {
            ChatName = name,
            Messages = new((await App.Client!.GetAllMessagesWithUser(name))!),
            CloseChat = () => { Model.CarouselPage = 1; },
        };
        
        App.MessageReceived += msg =>
        {
            if (msg.SenderName == name)
            {
                chat.AddMessage(msg);
                ChatView.ScrollViewer.ScrollToEnd();
            }
        };
        Model.OpenedChat = chat;
        Model.CarouselPage = 2;
    }

    private async void OnConversationClicked(object? sender, PointerReleasedEventArgs e)
    {
        string conversationName = ((TextBlock) ((Border) sender!).Child!).Text!;
        await OpenChat(conversationName);
    }
}