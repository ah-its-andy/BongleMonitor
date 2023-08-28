using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Threading;
using System.Threading.Tasks;

namespace BongleMonitor.PartialView;

public partial class OutputView : UserControl
{
    private readonly Mutex mutex = new Mutex();
    public OutputView()
    {
        InitializeComponent();
    }

    public async Task AddLineAsync(LogModel logModel)
    {
        mutex.WaitOne();
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                if (panelLines.Children.Count > 100)
                {
                    panelLines.Children.RemoveAt(0);
                }
                var line = new TextLineView();
                await line.SetLogModelAsync(logModel);
                panelLines.Children.Add(line);
            }
            finally
            {
                mutex.ReleaseMutex();
            }

        });
    }
}