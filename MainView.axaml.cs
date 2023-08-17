using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using BongleMonitor.PartialView;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Input;
using Tmds.DBus.Protocol;

namespace BongleMonitor;

public partial class MainView : UserControl
{
    private static MainView _instance;
    public static MainView Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new MainView();
            }
            return _instance;
        }
    }
    public MainView()
    {
        InitializeComponent();
        btnShutdown.Click += BtnShutdown_Click;
        btnReset.Click += BtnReset_Click;
    }

    private async void BtnReset_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new PartialView.Modal();
        dialog.ConfirmClick += async (s, e) => await dialog.RunScript();
        await dialog.InitScript("shutdown -r now");
        await dialog.SetConfirmMessage("System will reboot");
        await dialog.ShowConfirm();
        await dialog.ShowDialog();
    }

    private async void BtnShutdown_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new PartialView.Modal();
        await dialog.InitScript("shutdown now");
        dialog.ConfirmClick += async (s, e) => await dialog.RunScript();
        await dialog.SetConfirmMessage("System will shutdown");
        await dialog.ShowConfirm();
        await dialog.ShowDialog();
    }

    public async Task InitBottomBar()
    {
        var bottomBar = new PartialView.BottomBar();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            bottomBarRoot.Child = bottomBar;
        });
        await bottomBar.StartCalculateTime();
        await bottomBar.SubscribeServiceStatus();
        await bottomBar.FindIPAddresses();
    }

    public async Task InitMainPanel()
    {
        var mainPanel = new PartialView.MainPanel();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            mainPanelRoot.Child = mainPanel;
        });
    }
}
