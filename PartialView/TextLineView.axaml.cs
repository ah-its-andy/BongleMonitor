using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System.Threading.Tasks;

namespace BongleMonitor.PartialView;

public partial class TextLineView : UserControl
{
    public TextLineView()
    {
        InitializeComponent();
    }

    public async Task SetLogModelAsync(LogModel logModel)
    {
        if (logModel.Level == "ERROR")
        {
            txtAppName.Foreground = Brush.Parse("#870000");
            txtLevel.Foreground = Brush.Parse("#870000");
            txtContent.Foreground = Brush.Parse("#870000");
        } else
        {
            txtAppName.Foreground = Brush.Parse("#CCCCCC");
            txtLevel.Foreground = Brush.Parse("#CCCCCC");
            txtContent.Foreground = Brush.Parse("#CCCCCC");
        }
        txtAppName.Text = logModel.App;
        txtLevel.Text = logModel.Level;
        txtContent.Text = logModel.Message;
    }
}