using System;
using System.Collections.ObjectModel;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClientApp.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty] private string chatName;
    [ObservableProperty] private ObservableCollection<Message> messages = new();

    
    [ObservableProperty] private string draftMessage;

    [RelayCommand]
    public async void SendMessage()
    {
        await App.Client!.SendMessage(ChatName, DraftMessage);
        Messages.Add(new()
        {
            Content = DraftMessage,
            Timestamp = DateTime.Now,
            RecipientName = ChatName,
            SenderName = App.Client.Username,
        });
        DraftMessage = "";
    }
}