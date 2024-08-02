using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    }

    private async void ChatOpenButtonClicked(object? sender, RoutedEventArgs e)
    {
        await OpenChat(otherUserField.Text!);

        /*
        this.DockPanel.Children.Add(new ChatView
        {
            DataContext = chat,
        });*/

    }

    private async Task<ChatViewModel> GetChatModelForUser(string name)
    {
        ChatViewModel chat = new()
        {
            ChatName = name,
            Messages = new((await App.Client!.GetAllMessagesWithUser(name))!),
            CloseChat = () =>
            {
                Model.CarouselPage = 1;
            },
        };
        App.MessageReceived += msg =>
        {
            if (msg.SenderName == name)
            {
                chat.Messages.Add(msg);
            }
        };
        return chat;
    }

    private async Task OpenChat(string user)
    {
        ChatViewModel model = await GetChatModelForUser(user);
        Model.OpenedChat = model;
        Model.CarouselPage = 2;
    }

    private async void OnConversationClicked(object? sender, PointerReleasedEventArgs e)
    {
        string conversationName = ((TextBlock) ((Border) sender!).Child!).Text!;
        await OpenChat(conversationName);
    }
}