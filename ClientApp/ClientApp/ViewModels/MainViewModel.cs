using CommunityToolkit.Mvvm.ComponentModel;

namespace ClientApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{ 
    //[ObservableProperty] private ObservableCollection<ChatViewModel> chats = new();
    [ObservableProperty] private ChatViewModel openedChat = new();
    [ObservableProperty] private int carouselPage;
}