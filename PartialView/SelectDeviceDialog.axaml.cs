using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace BongleMonitor.PartialView;

public partial class SelectDeviceDialog : UserControl
{
    public SelectDeviceDialog()
    {
        InitializeComponent();
        btnCancel.Click += async (sender, e) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => MainView.Instance.modalRoot.IsVisible = false);
        };
        btnConfirm.Click += async (sender, e) =>
        {
            if (bongleList.SelectedItem != null)
            {
                var selectItem = bongleList.SelectedItem as ListBoxItem;
                if (selectItem == null) return;
                var textBlock = selectItem.Content as TextBlock;
                if (textBlock == null) return;
                Result = textBlock.Text;
            }
        };
    }

    public string Result { get; private set; }
}