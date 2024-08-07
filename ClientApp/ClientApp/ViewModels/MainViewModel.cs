﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClientApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{ 
    //[ObservableProperty] private ObservableCollection<ChatViewModel> chats = new();
    [ObservableProperty] private ChatViewModel? openedChat = null;
    [ObservableProperty] private ObservableCollection<string> conversations = new();
    [ObservableProperty] private int carouselPage;
}