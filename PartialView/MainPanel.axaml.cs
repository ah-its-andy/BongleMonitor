using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

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
        btnReset.Click += BtnReset_Click;
        btnShutdown.Click += BtnShutdown_Click;
    }

    private async void BtnShutdown_Click(object? sender, RoutedEventArgs e)
    {
        await StartAsync("poweroff", "shutdown now");
    }

    private async void BtnReset_Click(object? sender, RoutedEventArgs e)
    {
        await StartAsync("reboot", "reboot");
    }

    private async void BtnSvcStop_Click(object? sender, RoutedEventArgs e)
    {
        var selectedDev = await MainView.Instance.ShowBongleList();
        if(string.IsNullOrEmpty(selectedDev))
        {
            await MainView.Instance.WriteLogAsync("[systemctl] No device selected.");
            return;
        }
        await StartAsync("systemctl", $"systemctl stop gammu-smsd@{Path.GetFileNameWithoutExtension(selectedDev)}");
    }

    private async void BtnSvcStart_Click(object? sender, RoutedEventArgs e)
    {
        var selectedDev = await MainView.Instance.ShowBongleList();
        if (string.IsNullOrEmpty(selectedDev))
        {
            await MainView.Instance.WriteLogAsync("[systemctl] No device selected.");
            return;
        }
        await StartAsync("systemctl", $"systemctl start gammu-smsd@{Path.GetFileNameWithoutExtension(selectedDev)}");
    }

    private async void BtnSvcStatus_Click(object? sender, RoutedEventArgs e)
    {
        var selectedDev = await MainView.Instance.ShowBongleList();
        if (string.IsNullOrEmpty(selectedDev))
        {
            await MainView.Instance.WriteLogAsync("[systemctl] No device selected.");
            return;
        }
        await StartAsync("systemctl", $"systemctl status gammu-smsd@{Path.GetFileNameWithoutExtension(selectedDev)}");
    }

    public async Task StartAsync(string prefix, string commands)
    {
        try
        {
            await MainView.Instance.WriteLogAsync($"[SHELL] Executing {commands}");
            var process = Command.StartShell(commands);
            process.Start();
            MainView.Instance.BindLogStream(prefix, process.StandardOutput);
            //MainView.Instance.BindLogStream($"{prefix} ERROR", process.StandardError);
            await process.WaitForExitAsync();
        }
        catch(Exception e)
        {
            await MainView.Instance.WriteLogAsync($"[ERROR] {e.Message}");
        }
    }

    private async void BtnLSDev_Click(object? sender, RoutedEventArgs e)
    {
        await StartAsync("LSDev", "ls /dev/ttyUSB*");
    }

    private async void BtnLSUSB_Click(object? sender, RoutedEventArgs e)
    {
        await StartAsync("LSDev", "lsusb");
    }
}