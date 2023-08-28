using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace BongleMonitor
{
    internal static class Command
    {
        public static async Task WaitAsync(this Process process, TimeSpan? ttl = null)
        {
            try
            {
                var cts = new CancellationTokenSource(ttl ?? TimeSpan.FromMinutes(2));
                await process.WaitForExitAsync(cts.Token);
            }
            catch(TaskCanceledException e)
            {
                MainView.Instance.Log("SHELL", "ERROR", $"Process {process.ProcessName} timed out.");
            }
            catch(Exception e)
            {
                throw e;
            }
        }
        public static Process StartShell(string script, CancellationToken? cancellationToken = null)
        {
            // 創建一個新的ProcessStartInfo對象
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "/bin/sh"; // 指定要運行的shell
            startInfo.Arguments = $"-c \"{script}\""; // 指定要執行的命令
            startInfo.RedirectStandardOutput = true; // 將輸出重定向到程序
            startInfo.UseShellExecute = false; // 不使用shell啟動程序
            startInfo.CreateNoWindow = true; // 不創建新窗口
            startInfo.RedirectStandardError = true;
            // 創建一個新的Process對象
            Process process = new Process();
            process.StartInfo = startInfo;
            return process;
        }
       

    }

}


