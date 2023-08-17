using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BongleMonitor.PartialView;

public partial class MainPanel : UserControl
{
    public MainPanel()
    {
        InitializeComponent();

        btnLSUSB.Click += BtnLSUSB_Click;
        btnLSDev.Click += BtnLSDev_Click;
        btnSvcStatus.Click += BtnSvcStatus_Click;
        btnSvcStart.Click += BtnSvcStart_Click;
        btnSvcStop.Click += BtnSvcStop_Click;
        btnShowLogs.Click += BtnShowLogs_Click;
    }


    private async void BtnShowLogs_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new PartialView.Modal();
        await dialog.InitScript("tail -n 1 -f /var/logs/gammu-smsd.log");
        await dialog.ShowOutput();
        await dialog.ShowDialog();
        await dialog.RunScript();
    }

    private async void BtnSvcStop_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new PartialView.Modal();
        await dialog.InitScript("systemctl stop gammu-smsd");
        await dialog.ShowOutput();
        await dialog.ShowDialog();
        await dialog.RunScript();
    }

    private async void BtnSvcStart_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new PartialView.Modal();
        await dialog.InitScript("systemctl start gammu-smsd");
        await dialog.ShowOutput();
        await dialog.ShowDialog();
        await dialog.RunScript();
    }

    private async void BtnSvcStatus_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new PartialView.Modal();
        await dialog.InitScript("systemctl status gammu-smsd");
        await dialog.ShowOutput();
        await dialog.ShowDialog();
        await dialog.RunScript();
    }

    private async void BtnLSDev_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new PartialView.Modal();
        await dialog.InitScript("ls /dev/ttyUSB*");
        await dialog.ShowOutput();
        await dialog.ShowDialog();
        await dialog.RunScript();
    }

    private async void BtnLSUSB_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new PartialView.Modal();
        await dialog.InitScript("lsusb");
        await dialog.ShowOutput();
        await dialog.ShowDialog();
        await dialog.RunScript();
    }


}