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

    private TextBlock TextCalculateTime;

    public async Task StartCalculateTime()
    {
        TextCalculateTime = new TextBlock
        {
            FontSize = 10,
            Foreground = Brush.Parse("#717171"),
            Text = "Running"
        };
        MainView.Instance.Log("UIThread", "INFO", "Rendering CalculateTime");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            bottomBar.Children.Add(TextCalculateTime);
        });
        var now = DateTime.Now;
        var timer = new Timer(1000);
        timer.Elapsed += async (s, e) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var elapse = (DateTime.Now - now);
                var text = "";
                if (elapse.Days > 1)
                {
                    text = $"{elapse.Days} DAYS {elapse.Hours}:{elapse.Minutes}:{elapse.Seconds}";
                } else
                {
                    text = $"{elapse.Days} DAY {elapse.Hours}:{elapse.Minutes}:{elapse.Seconds}";
                }
                TextCalculateTime.Text = $"Running: {text}";
            });
        };
        timer.Start();
    }

    public async Task FindIPAddresses()
    {
        MainView.Instance.Log("UIThread", "INFO", "Rendering IP Addresses");

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