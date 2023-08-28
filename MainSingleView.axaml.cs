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

    private int tapped;
    private Mutex tapperMux;
    private System.Timers.Timer _maskTimer;

    public MainSingleView()
    {
        InitializeComponent();
        tapperMux = new Mutex();
        _maskTimer = new System.Timers.Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
        _maskTimer.Elapsed += async (sender, e) => {
            tapperMux.WaitOne();
            try
            {
                if(tapped > 0)
                {
                    tapped = 0;
                } else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        mask.IsVisible = true;
                        MainView.Instance.Log("UIThread", "INFO", "Show mask layer");
                    });
                }
            }
            finally
            {
                tapperMux.ReleaseMutex();
            }
        };
        Tapped += async (sender, e) =>
        {
            tapperMux.WaitOne();
            try
            {
                if(tapped == 0)
                {
                    tapped = 1;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (mask.IsVisible)
                        {
                            mask.IsVisible = false;
                            MainView.Instance.Log("UIThread", "INFO", "Hide mask layer");
                        }
                    });
                }
            }
            finally
            {
                tapperMux.ReleaseMutex();
            }
            
        };

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
                //await MainView.Instance.InitGammuSmsD();
                await MainView.Instance.InitBottomBar();
                await MainView.Instance.InitMainPanel();
                await MainView.Instance.InitLogView();
                _maskTimer.Start();
            }
            catch (Exception ex)
            {
                MainView.Instance.Log("UIThread", "INFO", ex.Message);
            }
        };
    }
}