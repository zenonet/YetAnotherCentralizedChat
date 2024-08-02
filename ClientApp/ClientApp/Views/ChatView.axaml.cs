using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClientApp.ViewModels;

namespace ClientApp.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
    }

    private void BackButtonReleased(object? sender, RoutedEventArgs eventArgs)
    {
        ((ChatViewModel)DataContext!).CloseChat?.Invoke();
    }
}