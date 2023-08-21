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


    public MainSingleView()
    {
        InitializeComponent();
        Loaded += async (sender, e) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (MainView.Instance.Parent == null)
                {
                    rootVisual.Children.Clear();
                    rootVisual.Children.Add(MainView.Instance);
                }
            });
            await MainView.Instance.InitBottomBar();
            await MainView.Instance.InitMainPanel();
            await MainView.Instance.InitGammuSmsD();
            await MainView.Instance.InitLogView();
        };
    }
}