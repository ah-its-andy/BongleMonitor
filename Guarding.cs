using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BongleMonitor
{
    public enum ProcessStatus
    {
        NotRunning,
        Starting,
        Running,
        Stopping
    }

    public class ProcessEventArgs : EventArgs
    {
        public ProcessStatus Status { get; set; }
    }

    public class ProcessGuardian
    {
        private Process? process;
        private ProcessStatus status;
        private string fileName;
        private string[] arguments;

        public event EventHandler<ProcessEventArgs>? ProcessStatusChanged;

        public ProcessGuardian(string _fileName, params string[] _arguments)
        {
            fileName = _fileName;
            arguments = _arguments;
            status = ProcessStatus.NotRunning;
        }

        protected virtual void OnProcessStatusChanged(ProcessStatus status)
        {
            ProcessStatusChanged?.Invoke(this, new ProcessEventArgs { Status = status });
        }

        public ProcessStatus GetProcessStatus()
        {
            return status;
        }

        public void StartProcess()
        {
            if (status == ProcessStatus.Running || status == ProcessStatus.Starting)
            {
                return;
            }

            status = ProcessStatus.Starting;
            OnProcessStatusChanged(status);

            process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = string.Join(" ", arguments);
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;
            process.Exited += ProcessExited;
            process.Start();

            status = ProcessStatus.Running;
            OnProcessStatusChanged(status);
        }

        public void StopProcess()
        {
            if (status == ProcessStatus.NotRunning || status == ProcessStatus.Stopping)
            {
                return;
            }

            status = ProcessStatus.Stopping;
            OnProcessStatusChanged(status);

            process.Exited -= ProcessExited;
            process.Kill();
            process.Dispose();

            status = ProcessStatus.NotRunning;
            OnProcessStatusChanged(status);
        }

        public void StartProcessGuardian()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (status == ProcessStatus.NotRunning)
                    {
                        StartProcess(); // 替换为你的进程名称或路径
                    }
                    else if (status == ProcessStatus.Running)
                    {
                        // 检查进程是否仍在运行
                        if (process != null && process.HasExited)
                        {
                            status = ProcessStatus.NotRunning;
                            OnProcessStatusChanged(status);
                        }
                    }

                    // 每隔一段时间检查一次进程状态
                    System.Threading.Thread.Sleep(5000); // 5秒
                }
            });
        }

        private void ProcessExited(object? sender, EventArgs e)
        {
            status = ProcessStatus.NotRunning;
            OnProcessStatusChanged(status);
        }
    }
}
