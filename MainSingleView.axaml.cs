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
using System.Timers;

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

    private int idleSeconds;


    public MainSingleView()
    {
        InitializeComponent();
        this.Tapped += async (sender, e) =>
        {
            Interlocked.Exchange(ref idleSeconds, 0);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (mask.IsVisible)
                {
                    mask.IsVisible = false;
                }
            });
        };

        Task.Run(() =>
        {
            var timer = new System.Timers.Timer(10000);
            timer.Elapsed += async (sender, e) => {
                var idleSec = Interlocked.Increment(ref idleSeconds); 
                if(idleSeconds > 6)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if(!mask.IsVisible) 
                        {
                            mask.IsVisible = true;
                         }
                    });

                }
            };
            timer.Start();
        });

        Loaded += async (sender, e) =>
        {
            MainView.Instance.Log("UIThread", "INFO", "Rendering MainView");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (MainView.Instance.Parent == null)
                {
                    rootVisual.Children.Clear();
                    rootVisual.Children.Add(MainView.Instance);
                }
            });
            MainView.Instance.StartLogWriter();
            MainView.Instance.RenderLogs();

            try
            {
                await MainView.Instance.InitBottomBar();
                await MainView.Instance.InitGammuSmsD();
                await MainView.Instance.InitMainPanel();
                await MainView.Instance.InitLogView();
            }
            catch (Exception ex)
            {
                MainView.Instance.Log("UIThread", "INFO", ex.Message);
            }
        };
    }
}