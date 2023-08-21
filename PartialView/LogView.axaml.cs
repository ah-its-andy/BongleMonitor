using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BongleMonitor.PartialView
{
    public partial class LogView : UserControl
    {
        public LogView()
        {
            InitializeComponent();
        }

        public Dictionary<string, Process> Commands { get; private set; } = new Dictionary<string, Process>();
        public CancellationTokenSource CancellationTokenSource { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        private Mutex mutex = new Mutex();
        public void StartAsync()
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            if (!Directory.Exists("/tmp/gammu-smsd/"))
            {
                Directory.CreateDirectory("/tmp/gammu-smsd/");
            }
            Task.Run(() =>
            {
                while (true)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    var files = Directory.GetFiles("/tmp/gammu-smsd/", "*.log");
                    if (files.Length == 0)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }
                    foreach ( var file in files)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (Commands.ContainsKey(fileName))
                        {
                            continue;
                        }
                        Commands[fileName] = Command.StartShell($"tail -n 20 -f {file}");
                        Commands[fileName].Start();
                        Task.Factory.StartNew(async (f) =>
                        {
                            while (true)
                            {
                                if (CancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }
                                var line = await Commands[f.ToString()].StandardOutput.ReadLineAsync();
                                await WriteLine($"[{f.ToString()}] {line}");
                            }
                        }, fileName);
                    }
                }
            });
            
        }

        public void Stop()
        {
            CancellationTokenSource.Cancel();
            foreach(var command in Commands)
            {
                try
                {
                    command.Value.Kill();
                }
                catch (InvalidOperationException) { }
            }
        }

        public async Task WriteLine(string s)
        {
            await Task.Run(async () =>
            {
                mutex.WaitOne();
                try
                {
                    if(logViewer.Items.Count >= 100)
                    {
                        logViewer.Items.Remove(logViewer.Items.GetAt(0));
                    }
                    logViewer.Items.Add(new TextBlock
                    {
                        Text = s,
                        FontSize = 10,
                        Foreground = Brush.Parse("#717171"),
                    });
                }
                finally { mutex.ReleaseMutex(); }
            });
        }
    }
}
