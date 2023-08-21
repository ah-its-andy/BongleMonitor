using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using BongleMonitor.PartialView;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BongleMonitor;

public partial class MainSingleView : UserControl
{
    private static  MainSingleView _instance;
    public static  MainSingleView Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new MainSingleView();
            }
            return _instance;
        }
    }

    public readonly Dictionary<string, ProcessGuardian> Services;
    public readonly Dictionary<string, FileSystemWatcher> FileWatchers;
    private readonly Mutex mutex;


    public MainSingleView()
    {
        InitializeComponent();
        mutex = new Mutex();
        Loaded += MainSingleView_Loaded;
        Services = new Dictionary<string, ProcessGuardian>();
        FileWatchers = new Dictionary<string, FileSystemWatcher>();
        btnReset.Click += BtnReset_Click;
        btnCloseLogView.Click += BtnCloseLogView_Click;
    }

    public async Task WriteLog(string s)
    {
        mutex.WaitOne();
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (logViewer.Items.Count >= 100)
                {
                    logViewer.Items.Remove(logViewer.Items.GetAt(0));
                }
                logViewer.Items.Add(new TextBlock
                {
                    Text = s,
                    FontSize = 10,
                    Foreground = Brush.Parse("#717171"),
                });
            });
        }
        finally { mutex.ReleaseMutex(); }
    }

    //public async Task WriteLog(string log)
    //{
    //    await Task.Run(async () =>
    //    {
    //        mutex.WaitOne();
    //        try
    //        {
    //            await Dispatcher.UIThread.InvokeAsync(() =>
    //            {

    //                logViewer.Text += log;
    //            });
    //        }
    //        finally { mutex.ReleaseMutex(); }
    //    });
    //}

    public async Task ShowLogView()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            logViewRoot.Height = 180;
            await WriteLog("Maximuim log viewer");
        });
    }

    public async Task HideLogView()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            logViewRoot.Height = 50;
            await WriteLog("Minimalize log viewer");
        });
    }

    public async Task SwitchLogView()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if(logViewRoot.Height > 60 )
            {
                await HideLogView();
            } else
            {
                await ShowLogView();
            }           
        });
    }

    private async void BtnCloseLogView_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (logViewRoot.Height < 180)
            {
                logViewRoot.Height = 180;
                await WriteLog("Maximuim log viewer");
            }
            else
            {
                logViewRoot.Height = 50;
                await WriteLog("Minimalize log viewer");
            }
        });
    }

    private void BtnReset_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var p = Command.StartShell("reboot");
        p.Start();
    }

    private async void MainSingleView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //var flag = await InitGammuSmsD();
        //if (!flag)
        //{
        //    return;
        //}

        //await Task.Delay(TimeSpan.FromSeconds(2));
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            rootVisual.Children.Clear();
            rootVisual.Children.Add(MainView.Instance);
        });
        await WriteLog("Starts lauching");
        await MainView.Instance.InitBottomBar();
        await WriteLog("BottomBar completed.");
        //await MainView.Instance.InitMainPanel();
        //await WriteLog("Main Panel completed.");
        ////await MainView.Instance.InitLogView();
        //await StartServices();
    }

    private async Task<IEnumerable<string>> FindTTYUsbDevices()
    {
        return await Task.Run<IEnumerable<string>>(() =>
        {
            try
            {           
                // 先查找ttyUSB設備
                var files = Directory.GetFiles("/dev", "ttyUSB*");
                if (files.Length == 0)
                {
                    return new List<string>();
                }
                return files;
            }
            catch (System.IO.DirectoryNotFoundException)
            {
                return new List<string>();
            }
        });
    }

    private async Task<Dictionary<string, string>> GetIdentify(string dev)
    {
        var tmpDir = $"/tmp/gammu/";
        if (!Directory.Exists(tmpDir))
        {
            Directory.CreateDirectory(tmpDir);
        }
        var fileName = Path.Join(tmpDir, Path.GetFileNameWithoutExtension(dev));
        if(File.Exists(fileName))
        {
            File.Delete(fileName);
        }
        var gammuConfig = Config.GetGammuConfig(dev);
        await File.WriteAllTextAsync(fileName, gammuConfig);

        var process = Command.StartShell($"gammu -c {fileName} identify");
        process.Start();
        var result = Config.UnmarshalGammuIdentify(process.StandardOutput);
        process.WaitForExit();
        return result;
    }

    private async Task<bool> InitGammuSmsD()
    {
        var devices = await FindQuectelDevices();

        var usbDevs = await FindTTYUsbDevices();
        if(usbDevs.Count() == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.textTip.Text = "USB device not found. Please check the device connection.\r\n";
                if(devices.Count() > 0)
                {
                    this.textTip.Text += "Following devices was connected: \r\n";
                    foreach(var device in devices)
                    {
                        this.textTip.Text += device;
                        this.textTip.Text += "\r\n";
                    }
                }
            });
            return false;
        }
        
        var foundBongles = new List<Dictionary<string, string>>();

        foreach(var usbDev in usbDevs)
        {
            var identifyInfo = await GetIdentify(usbDev);
            if(identifyInfo.TryGetValue("Manufacturer", out string manufacturer) && 
                identifyInfo.TryGetValue("Model", out string model) &&
                identifyInfo.TryGetValue("SIM IMSI", out string imsi) &&
                !string.IsNullOrEmpty(imsi) &&
                manufacturer == "Quectel" &&
                model.Contains("EC200A"))
            {
                var phoneNumber = Environment.GetEnvironmentVariable($"IMSI_{imsi}");
                if(!string.IsNullOrEmpty(phoneNumber))
                {
                    identifyInfo["ID"] = phoneNumber;
                } else
                {
                    identifyInfo["ID"] = imsi;
                }
                foundBongles.Add(identifyInfo);
            }
        }

        if(foundBongles.Count == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                this.textTip.Text = "Quectel EC200A not found. Please check the device connection.\r\n";
                if (devices.Count() > 0)
                {
                    this.textTip.Text += "Following devices was connected: \r\n";
                    foreach (var device in devices)
                    {
                        this.textTip.Text += device;
                        this.textTip.Text += "\r\n";
                    }
                }
            });
            return false;
        }
        
        foreach(var bongle in foundBongles)
        {
            var tmpDir = $"/tmp/gammu-smsd/";
            if (!Directory.Exists(tmpDir))
            {
                Directory.CreateDirectory(tmpDir);
            }
            var fileName = Path.Join(tmpDir, Path.GetFileNameWithoutExtension(bongle["Device"]));
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            var smsdConfig = Config.GetGammuSmsDConfig(bongle["ID"], bongle["Device"]);
            if (!Directory.Exists($"/share/gammu-smsd/{bongle["ID"]}/inbox"))
            {
                Directory.CreateDirectory($"/share/gammu-smsd/{bongle["ID"]}/inbox");
            }
            if (!Directory.Exists($"/share/gammu-smsd/{bongle["ID"]}/outbox"))
            {
                Directory.CreateDirectory($"/share/gammu-smsd/{bongle["ID"]}/outbox");
            }
            if (!Directory.Exists($"/share/gammu-smsd/{bongle["ID"]}/sent"))
            {
                Directory.CreateDirectory($"/share/gammu-smsd/{bongle["ID"]}/sent");
            }
            if (!Directory.Exists($"/share/gammu-smsd/{bongle["ID"]}/error"))
            {
                Directory.CreateDirectory($"/share/gammu-smsd/{bongle["ID"]}/error");
            }

            await File.WriteAllTextAsync(fileName, smsdConfig);
            var daemon = new ProcessGuardian("/usr/bin/gammu-smsd", "--config", fileName);
            Services[bongle["ID"]] = daemon;
          
        }


        return true;
    }

    private async Task StartServices()
    {
        foreach(var service in Services)
        {
            service.Value.StartProcessGuardian();
        }
    }

   private async Task<IEnumerable<string>> FindQuectelDevices()
    {
        return await Task.Run<IEnumerable<string>>(() =>
        {
            try
            {
                //Quectel Wireless Solutions Co., Ltd. Android
                var process = Command.StartShell("lsusb");
                process.Start();
                var result = new List<string>();
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (!string.IsNullOrEmpty(line) && line.Contains("Quectel Wireless Solutions"))
                    {
                        result.Add(line);
                    }
                }
                process.WaitForExit();
                return result;
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        });
    }
}