using Avalonia.Controls;

namespace BongleMonitor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Content = MainSingleView.Instance;
    }
}