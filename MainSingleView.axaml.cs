using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace BongleMonitor;

public partial class MainSingleView : UserControl
{
    public MainSingleView()
    {
        InitializeComponent();
        Loaded += MainSingleView_Loaded;
    }

    private async void MainSingleView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        var mainView = new MainView();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            rootVisual.Children.Clear();
            rootVisual.Children.Add(mainView);
        });
        await mainView.InitBottomBar();
        await mainView.InitMainPanel();
    }
}