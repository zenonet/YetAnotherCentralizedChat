using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClientApp.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty] private string chatName;
    [ObservableProperty] private ObservableCollection<Message> messages = new();

    [ObservableProperty] private string draftMessage = "";

    public Action? CloseChat { get; set; }

    [RelayCommand]
    public async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(DraftMessage)) return;

        await App.Client!.SendMessage(ChatName, DraftMessage);
        AddMessage(new()
        {
            Content = DraftMessage,
            Timestamp = DateTime.Now,
            RecipientName = ChatName,
            SenderName = App.Client.Username,
        });
        DraftMessage = "";
    }

    public event Action? MessagesChanged;

    public void AddMessage(Message msg)
    {
        Messages.Add(msg);   
        MessagesChanged?.Invoke();
    }
}