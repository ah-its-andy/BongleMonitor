using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace BongleMonitor.PartialView;

public partial class SelectDeviceDialog : UserControl
{
    private TaskCompletionSource<string> tcs;
    public SelectDeviceDialog()
    {
        InitializeComponent();
        btnCancel.Click += async (sender, e) =>
        {
            IsClosed = true;
            tcs.SetResult("");
            await Dispatcher.UIThread.InvokeAsync(() => MainView.Instance.modalRoot.IsVisible = false);
        };
        btnConfirm.Click += async (sender, e) =>
        {
            IsClosed = true;
            if (bongleList.SelectedItem != null)
            {
                var selectItem = bongleList.SelectedItem as ListBoxItem;
                if (selectItem == null) return;
                var textBlock = selectItem.Content as TextBlock;
                if (textBlock == null) return;
                Result = textBlock.Text;
            }
            tcs.SetResult(Result);
            await Dispatcher.UIThread.InvokeAsync(() => MainView.Instance.modalRoot.IsVisible = false);

        };
    }

    public Task<string> WaitCloseAsync()
    {
        tcs = new TaskCompletionSource<string>();
        return tcs.Task;
    }

    public string Result { get; private set; }
    public bool IsClosed { get; private set; }
}