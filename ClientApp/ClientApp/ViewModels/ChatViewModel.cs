using System.Collections.ObjectModel;
using Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClientApp.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty] private string chatName;
    [ObservableProperty] private ObservableCollection<Message> messages = new();
}