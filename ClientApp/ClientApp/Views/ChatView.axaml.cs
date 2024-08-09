using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClientApp.ViewModels;

namespace ClientApp.Views;

public partial class ChatView : UserControl
{
    public ChatView()
    {
        InitializeComponent();
        SendButton.Focus();
    }
    
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        ScrollViewer.ScrollToEnd();
        if (DataContext != null)
            ((ChatViewModel) DataContext!).MessagesChanged += () => { Dispatcher.UIThread.Post(() => { ScrollViewer.ScrollToEnd(); }); };
    }

    private void BackButtonReleased(object? sender, RoutedEventArgs eventArgs)
    {
        ((ChatViewModel) DataContext!).CloseChat?.Invoke();
    }
}