using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
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
    private Process? CurrentProcess;
    public MainView()
    {
        InitializeComponent();
        this.Loaded += MainView_Loaded;
        BtnCloseModal.Click += BtnCloseModal_Click;
        btnLSUSB.Click += BtnLSUSB_Click;
        btnLSDev.Click += BtnLSDev_Click;
        btnSvcStatus.Click += BtnSvcStatus_Click;
        btnSvcStart.Click += BtnSvcStart_Click;
        btnSvcStop.Click += BtnSvcStop_Click;
        btnShowLogs.Click += BtnShowLogs_Click;
        btnShutdown.Click += BtnShutdown_Click;
        btnReset.Click += BtnReset_Click;
        btnConfirmOk.Click += BtnConfirmOk_Click;
        btnConfirmCancel.Click += BtnConfirmCancel_Click;
    }

    private void BtnConfirmCancel_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("echo 'OK'");
    }

    private async void BtnConfirmOk_Click(object? sender, RoutedEventArgs e)
    {
        await RunScript();
    }

    private async void BtnReset_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("shutdown -r now");
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            txtConfirm.Text = "請確認重新啓動";
            modalConfirm.IsVisible = true;
            modalOutput.IsVisible = false;
            modalRoot.IsVisible = true;
        });
    }

    private async void BtnShutdown_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("shutdown now");
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            txtConfirm.Text = "請確認關機";
            modalConfirm.IsVisible = true;
            modalOutput.IsVisible = false;
            modalRoot.IsVisible = true;
        });
    }

    private async void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        await FindIPAddresses();
    }

    private async void BtnShowLogs_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("tail -n 1 -f /var/logs/gammu-smsd.log");
        await RunScript();
    }

    private async void BtnSvcStop_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("systemctl stop gammu-smsd");
        await RunScript();
    }

    private async void BtnSvcStart_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("systemctl start gammu-smsd");
        await RunScript();
    }

    private async void BtnSvcStatus_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("systemctl status gammu-smsd");
        await RunScript();
    }

    private async void BtnLSDev_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("ls /dev/ttyUSB*");
        await RunScript();
    }

    private async void BtnLSUSB_Click(object? sender, RoutedEventArgs e)
    {
        InitScript("lsusb");
        await RunScript();
    }

    private void KillCurrentProcess()
    {
        try
        {
            if (CurrentProcess != null && !CurrentProcess.HasExited)
            {
                CurrentProcess.Kill();
            }
        }
        catch (InvalidOperationException) { }
    }

    private async void BtnCloseModal_Click(object? sender, RoutedEventArgs e)
    {
        KillCurrentProcess();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            modalRoot.IsVisible = false;
        });
    }

    public void InitScript(string script)
    {
        KillCurrentProcess();
        CurrentProcess = Command.StartShell(script);
    }

    public async Task RunScript()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TextOutput.Text = string.Empty;
            modalConfirm.IsVisible = false;
            modalOutput.IsVisible = true;
            modalRoot.IsVisible = true;
        });
        try
        {
            CurrentProcess?.Start();
            while (!CurrentProcess.StandardOutput.EndOfStream)
            {
                var line = CurrentProcess.StandardOutput.ReadLine();
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TextOutput.Text += line;
                    TextOutput.Text += "\r\n";
                });
            }
        }
        catch(Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TextOutput.Text += ex.Message;
                TextOutput.Text += "\r\n";
            });
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                BtnCloseModal.IsEnabled = true;
            });
        }
    }

    public async Task FindIPAddresses()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            bottomBar.Children.Clear();
        });

        var index = 0;
        foreach(var ipAddr in GetLocalIPAddresses())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                bottomBar.Children.Add(new TextBlock
                {
                    FontSize = 10,
                    Foreground = Brush.Parse("#717171"),
                    Text = $"IP {index+1}: {ipAddr}",
                });
            });
            index++;
        }
    }

    static IEnumerable<string> GetLocalIPAddresses()
    {
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface networkInterface in interfaces)
        {
            if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();

                foreach (UnicastIPAddressInformation ipAddressInfo in ipProperties.UnicastAddresses)
                {
                    if (ipAddressInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        yield return ipAddressInfo.Address.ToString();
                    }
                }
            }
        }
    }

}
