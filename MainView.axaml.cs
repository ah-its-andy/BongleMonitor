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
    }

    private async void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        await FindIPAddresses();
    }

    private async void BtnShowLogs_Click(object? sender, RoutedEventArgs e)
    {
        await RunScript("tail -n 1 -f /var/logs/gammu-smsd.log");
    }

    private async void BtnSvcStop_Click(object? sender, RoutedEventArgs e)
    {
        await RunScript("systemctl stop gammu-smsd");
    }

    private async void BtnSvcStart_Click(object? sender, RoutedEventArgs e)
    {
        await RunScript("systemctl start gammu-smsd");
    }

    private async void BtnSvcStatus_Click(object? sender, RoutedEventArgs e)
    {
        await RunScript("systemctl status gammu-smsd");
    }

    private async void BtnLSDev_Click(object? sender, RoutedEventArgs e)
    {
        await RunScript("ls /dev/ttyUSB*");
    }

    private async void BtnLSUSB_Click(object? sender, RoutedEventArgs e)
    {
        await RunScript("lsusb");
    }

    private async Task KillCurrentProcess()
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
        await KillCurrentProcess();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            modalRoot.IsVisible = false;
        });
    }

    public async Task RunScript(string script)
    {
        await KillCurrentProcess();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TextOutput.Text = string.Empty;
            modalRoot.IsVisible = true;
        });
        try
        {
            CurrentProcess = Command.StartShell(script);
            CurrentProcess.Start();
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
