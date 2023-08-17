using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BongleMonitor.PartialView;

public partial class Modal : UserControl
{
    private Process? CurrentProcess;
    private readonly Panel _parentElement;
    public Modal(Panel parentElement)
    {
        InitializeComponent();
        _parentElement = parentElement;

        BtnCloseModal.Click += BtnCloseModal_Click;
        BtnConfirmCancel.Click += BtnConfirmCancel_Click;
        BtnConfirmOk.Click += BtnConfirmOk_Click;
    }

    private void BtnConfirmOk_Click(object? sender, RoutedEventArgs e)
    {
        if(ConfirmClick != null)
        {
            ConfirmClick(sender, e);
        }
    }

    public EventHandler<RoutedEventArgs> ConfirmClick;

    private async void BtnConfirmCancel_Click(object? sender, RoutedEventArgs e)
    {
        await CloseDialog();
    }

    private async void BtnCloseModal_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await CloseDialog();
    }


    public async Task ShowConfirm()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ModalConfirm.IsVisible = true;
        });
    }

    public async Task ShowOutput()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ModalOutput.IsVisible = true;
        });
    }

    public async Task HideOutput()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TextOutput.Text = string.Empty;
            ModalOutput.IsVisible = false;
        });
    }

    public async Task HideConfirm()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TxtConfirm.Text = string.Empty;
            ModalConfirm.IsVisible = false;
        });
    }

    public async Task ShowDialog()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_parentElement != null)
            {
                _parentElement.Children.Clear();
                _parentElement.Children.Add(this);
                _parentElement.IsVisible = true;
            }
        });
    }

    public async Task CloseDialog()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_parentElement != null)
            {
                _parentElement.IsVisible = false;
            }
        });
    }

    public async Task WriteOutput(string v)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TextOutput.Text += v;
            TextOutput.Text += "\r\n";
        });
    }

    public async Task SetConfirmMessage(string v)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TxtConfirm.Text = v;
        });
    }

    public async Task InitScript(string script)
    {
        await KillCurrentProcess();
        CurrentProcess = Command.StartShell(script);
    }

    public async Task RunScript()
    {
        try
        {
            CurrentProcess?.Start();
            while (!CurrentProcess.StandardOutput.EndOfStream)
            {
                var line = CurrentProcess.StandardOutput.ReadLine();
                await this.WriteOutput(line);
            }
        }
        catch (Exception ex)
        {
            await this.WriteOutput(ex.Message);
        }
    }

    private async Task KillCurrentProcess()
    {
        try
        {
            if (CurrentProcess != null && !CurrentProcess.HasExited)
            {
                CurrentProcess.Kill();
            }
        }
        catch (InvalidOperationException) { }
    }
}