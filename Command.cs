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
    internal class Command
    {
        public static Process StartShell(string script)
        {
            // 創建一個新的ProcessStartInfo對象
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "/bin/sh"; // 指定要運行的shell
            startInfo.Arguments = $"-c '{script}'"; // 指定要執行的命令
            startInfo.RedirectStandardOutput = true; // 將輸出重定向到程序
            startInfo.UseShellExecute = false; // 不使用shell啟動程序
            startInfo.CreateNoWindow = true; // 不創建新窗口

            // 創建一個新的Process對象
            Process process = new Process();
            process.StartInfo = startInfo;
            return process;
        }
        public static void Run(string script, Action<StreamReader> readFn)
        {
            // 創建一個新的ProcessStartInfo對象
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "/bin/sh"; // 指定要運行的shell
            startInfo.Arguments = $"-c '{script}'"; // 指定要執行的命令
            startInfo.RedirectStandardOutput = true; // 將輸出重定向到程序
            startInfo.UseShellExecute = false; // 不使用shell啟動程序
            startInfo.CreateNoWindow = true; // 不創建新窗口

            // 創建一個新的Process對象
            Process process = new Process();
            process.StartInfo = startInfo;

            // 啟動進程
            process.Start();

            // 等待進程結束，並讀取輸出
            readFn(process.StandardOutput);
            process.WaitForExit();
        }
        public static void Run(string script, StreamWriter writer)
        {
            // 創建一個新的ProcessStartInfo對象
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "/bin/sh"; // 指定要運行的shell
            startInfo.Arguments = $"-c '{script}'"; // 指定要執行的命令
            startInfo.RedirectStandardOutput = true; // 將輸出重定向到程序
            startInfo.UseShellExecute = false; // 不使用shell啟動程序
            startInfo.CreateNoWindow = true; // 不創建新窗口

            // 創建一個新的Process對象
            Process process = new Process();
            process.StartInfo = startInfo;

            // 啟動進程
            process.Start();

            // 等待進程結束，並讀取輸出
            while (!process.StandardOutput.EndOfStream)
            {
                writer.WriteLine(process.StandardOutput.ReadLine()); 
            }
            process.WaitForExit();
        }

        public static IEnumerable<UsbInfo> LsUSB()
        {
            using(var stream = new MemoryStream())
            {
                using(var writer = new StreamWriter(stream))
                {
                    Run("lsusb", writer);
                }
                using(var reader = new StreamReader(stream))
                {
                    while(!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if(string.IsNullOrEmpty(line))
                        {
                            continue;
                        }
                        var parts = line.Split(' ');
                        string bus = "";
                        string device = "";
                        string id = "";
                        string name = "";
                        if(parts.Length >= 2)
                        {
                            bus = parts[1];
                        }
                        if (parts.Length >= 4)
                        {
                            device = parts[3];
                        }
                        if(parts.Length >= 6)
                        {
                            id = parts[5];
                        }
                        if(parts.Length >= 7)
                        {
                            name = parts[6];
                        }
                        yield return new UsbInfo
                        {
                            Bus = bus,
                            Device = device,
                            ID = id,
                            Name = name,
                        };
                    }
                }
            }
        }
    }

    // Bus 002 Device 001: ID 1d6b:0003 XXXXX XXXXX XXXXX
    public class UsbInfo
    {
        public string Bus { get; set; }
        public string Device { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
    }
}


