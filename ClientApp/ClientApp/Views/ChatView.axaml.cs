using Avalonia.Controls;
using Avalonia.Interactivity;
using ClientApp.ViewModels;

namespace ClientApp.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        SendButton.Focus();
    }

    private void BackButtonReleased(object? sender, RoutedEventArgs eventArgs)
    {
        ((ChatViewModel)DataContext!).CloseChat?.Invoke();
    }
}