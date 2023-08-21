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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
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

    private readonly Mutex mutex;
    private readonly List<Dictionary<string, string>> bongles;
    private readonly ConcurrentDictionary<string, long> lastPositions;

    public MainView()
    {
        InitializeComponent();
        mutex = new Mutex();
        bongles = new List<Dictionary<string, string>>();
        lastPositions = new ConcurrentDictionary<string, long>();
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
        var fileName = System.IO.Path.Join(tmpDir, System.IO.Path.GetFileNameWithoutExtension(dev));
        if (File.Exists(fileName))
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

    public async Task<bool> InitGammuSmsD()
    {
        var devices = await FindQuectelDevices();

        var usbDevs = await FindTTYUsbDevices();
        if (usbDevs.Count() == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await WriteLogAsync("[Launching] USB device not found. Please check the device connection.");
                if (devices.Count() > 0)
                {
                    await WriteLogAsync("[Launching] Following devices was connected:");
                    foreach (var device in devices)
                    {
                        await WriteLogAsync($"[Launching] {device}");
                    }
                }
            });
            return false;
        }

        foreach (var usbDev in usbDevs)
        {
            var identifyInfo = await GetIdentify(usbDev);
            if (identifyInfo.TryGetValue("Manufacturer", out string manufacturer) &&
                identifyInfo.TryGetValue("Model", out string model) &&
                identifyInfo.TryGetValue("SIM IMSI", out string imsi) &&
                !string.IsNullOrEmpty(imsi) &&
                manufacturer == "Quectel" &&
                model.Contains("EC200A"))
            {
                var phoneNumber = Environment.GetEnvironmentVariable($"IMSI_{imsi}");
                if (!string.IsNullOrEmpty(phoneNumber))
                {
                    identifyInfo["ID"] = phoneNumber;
                }
                else
                {
                    identifyInfo["ID"] = imsi;
                }
                bongles.Add(identifyInfo);
            }
        }

        if (bongles.Count == 0)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await WriteLogAsync("[Launching] Quectel EC200A not found. Please check the device connection.");
                if (devices.Count() > 0)
                {
                    await WriteLogAsync("[Launching] Following devices was connected:");
                    foreach (var device in devices)
                    {
                        await WriteLogAsync($"[Launching] {device}");
                    }
                }
            });
            return false;
        }

        foreach (var bongle in bongles)
        {
            var tmpDir = $"/tmp/gammu-smsd/";
            if (!Directory.Exists(tmpDir))
            {
                Directory.CreateDirectory(tmpDir);
            }
            var fileName = System.IO.Path.Join(tmpDir, System.IO.Path.GetFileNameWithoutExtension(bongle["Device"]));
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
            var dev = System.IO.Path.GetFileNameWithoutExtension(bongle["Device"]);
            var startService = Command.StartShell($"systemctl start gammu-smsd@{dev}");
            startService.Start();
            BindLogStream(dev, startService.StandardOutput);
            BindLogStream($"{dev} ERROR", startService.StandardError);
            await startService.WaitForExitAsync();
            var journal = Command.StartShell($"journalctl -u gammu-smsd@{dev} -f");
            journal.Start();
            BindLogStream(dev, startService.StandardOutput);
            BindLogStream($"{dev} ERROR", startService.StandardError);
        }


        return true;
    }

    public async Task InitLogView()
    {
        BindLogDir("SMSd", "/tmp/gammu-smsd/");
    }
    public async Task InitBottomBar()
    {
        var bottomBar = new PartialView.BottomBar();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            bottomBarRoot.Child = bottomBar;
        });
        await bottomBar.StartCalculateTime();
        await bottomBar.FindIPAddresses();
    }

    public async Task InitMainPanel()
    {
        var mainPanel = new PartialView.MainPanel();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            mainPanelContainer.Content = mainPanel;
        });
    }

    public void BindLogStream(string prefix, StreamReader reader)
    {
        Task.Run(async () =>
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                await WriteLogAsync($"[{prefix}] {line} {Environment.NewLine}");
            }
        });
    }

    public async void BindLogDir(string prefix, string dirPath)
    {
        try
        {
            var files = Directory.GetFiles(dirPath, "*.log");
            if (files?.Any() ?? false)
            {
                foreach (var file in files)
                {
                    BindLogFile(prefix, file);
                }
            }
        } catch (Exception ex)
        {
            await WriteLogAsync($"[ERROR] {ex.Message}");
        }
    }

    public void BindLogFile(string prefix, string fileName)
    {
        FileSystemWatcher watcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(fileName), System.IO.Path.GetFileName(fileName));

        // 设置要监视的事件类型
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

        // 添加事件处理程序
        watcher.Changed += async (sender, e) =>
        {
            long lastPosition = 0;
            if (lastPositions.ContainsKey(e.FullPath))
            {
                lastPosition = lastPositions[e.FullPath];
            }
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                if (lastPosition == 0)
                {
                    var lastLines = File.ReadLines(e.FullPath).Reverse().Take(20).Reverse();
                    foreach (var line in lastLines)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        await WriteLogAsync($"[{prefix}] {line} {Environment.NewLine}");
                    }
                    using (FileStream fileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (fileStream.Length > 0)
                        {
                            lastPosition = fileStream.Length - 1;
                        }
                    }
                }
                else
                {
                    using (FileStream fileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fileStream.Seek(lastPosition, SeekOrigin.Begin);
                        using (StreamReader reader = new StreamReader(fileStream))
                        {
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                if (string.IsNullOrEmpty(line)) continue;
                                await WriteLogAsync($"[{prefix}] {line} {Environment.NewLine}");
                            }
                            lastPosition = fileStream.Position;
                        }
                    }
                }
            }
        };

        // 启动监视
        watcher.EnableRaisingEvents = true;
    }

    public async Task WriteLogAsync(string s)
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
                var textBlock = new TextBlock
                {
                    Text = s,
                    FontSize = 10,
                    Foreground = Brush.Parse("#717171"),
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(0)
                };
                var item = new ListBoxItem
                {
                    Content = textBlock,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0)
                };
                if(s.Contains("ERROR"))
                {
                    textBlock.Foreground = Brush.Parse("#f25022");
                }
                logViewer.Items.Add(item);
                logViewerContainer.ScrollToEnd();
            });
        }
        finally { mutex.ReleaseMutex(); }
    }

    public async Task<string> ShowBongleList()
    {
        var dialog = new PartialView.SelectDeviceDialog();
        dialog.Width = 600;
        dialog.Height = 300;
        foreach(var bongle in bongles)
        {
            var item = new ListBoxItem
            {
                Content = new TextBlock
                {
                    Text = bongle["Device"],
                    FontSize = 14,
                    Foreground = Brush.Parse("#717171"),
                    TextWrapping = TextWrapping.Wrap,
                }
            };
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                dialog.bongleList.Items.Add(item);
            });
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            modalRoot.Children.Clear();
            modalRoot.Children.Add(dialog);
            modalRoot.IsVisible = true;
        });
        return dialog.Result;
    }
}
