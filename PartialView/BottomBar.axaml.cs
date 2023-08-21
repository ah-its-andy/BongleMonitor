using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace BongleMonitor.PartialView;

public partial class BottomBar : UserControl
{
    public BottomBar() {
        InitializeComponent();
    }

    public async Task StartCalculateTime()
    {
        var block = new TextBlock
        {
            FontSize = 10,
            Foreground = Brush.Parse("#717171"),
        };
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            bottomBar.Children.Add(block);
        });
        var now = DateTime.Now;
        var timer = new Timer(1000);
        timer.Elapsed += async (s, e) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => block.Text = $"Running: {(DateTime.Now - now).ToString()}");
        };
    }

    public async Task FindIPAddresses()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            bottomBar.Children.Clear();
        });

        var index = 0;
        foreach (var ipAddr in GetLocalIPAddresses())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                bottomBar.Children.Add(new TextBlock
                {
                    FontSize = 10,
                    Foreground = Brush.Parse("#717171"),
                    Text = $"IP {index + 1}: {ipAddr}",
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