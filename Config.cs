using Avalonia.Controls.Documents;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BongleMonitor
{
    internal class Config
    {
        public static string GetGammuSmsDService()
        {
            var buidler = new StringBuilder();
            buidler.AppendLine("[Unit]");
            buidler.AppendLine("Description = SMS daemon for Gammu");
            buidler.AppendLine("Documentation = man:gammu-smsd(1)");
            buidler.AppendLine("After = network-online.target");
            buidler.AppendLine("[Service]");
            buidler.AppendLine("EnvironmentFile = -/etc/default/gammu-smsd");
            buidler.AppendLine("ExecStart = /usr/bin/gammu-smsd --config /etc/gammu-smsdrc --pid=/run/gammu-smsd.pid --daemon");
            
            buidler.AppendLine("ExecReload = /bin/kill -HUP $MAINPID");
            buidler.AppendLine("ExecStopPost = /bin/rm -f /run/gammu-smsd.pid");
            buidler.AppendLine("Type = forking");
            buidler.AppendLine("PIDFile = /run/gammu-smsd.pid");
            buidler.AppendLine("[Install]");
            buidler.AppendLine("WantedBy = multi-user.target");
            return buidler.ToString();
        }

        public static string GetGammuConfig(string dev)
        {
            var builder = new StringBuilder();
            builder.AppendLine("[gammu]");
            builder.AppendLine($"port = {dev}");
            builder.AppendLine("model =");
            builder.AppendLine("connection = at19200");
            builder.AppendLine("synchronizetime = yes");
            builder.AppendLine("logfile =");
            builder.AppendLine("logformat = nothing");
            builder.AppendLine("use_locking =");
            builder.AppendLine("gammuloc =");
            return builder.ToString();
        }
        public static string GetGammuSmsDConfig(string id, string dev)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Configuration file for Gammu SMS Daemon");

            builder.AppendLine("# Gammu library configuration, see gammurc(5)");
            builder.AppendLine("[gammu]");
            builder.AppendLine("# Please configure this!");
            builder.AppendLine($"port = {dev}");
            builder.AppendLine("connection = at19200");
            builder.AppendLine("# Debugging");
            builder.AppendLine("#logformat = textall");

            builder.AppendLine("# SMSD configuration, see gammu-smsdrc(5)");
            builder.AppendLine("[smsd]");
            builder.AppendLine("service = files");
            builder.AppendLine("# logfile = syslog");
            builder.AppendLine($"logfile = /var/log/gammu-smsd/{dev}.log");
            builder.AppendLine("# Increase for debugging information");
            builder.AppendLine("debuglevel = 0");

            builder.AppendLine("# Paths where messages are stored");
            builder.AppendLine($"inboxpath = /share/gammu-smsd/{dev}/inbox");
            builder.AppendLine($"outboxpath = /share/gammu-smsd/{dev}/outbox");
            builder.AppendLine($"sentsmspath = /share/gammu-smsd/{dev}/sent");
            builder.AppendLine($"errorsmspath = /share/gammu-smsd/{dev}/error");
            return builder.ToString();
        }

        public static async Task<Dictionary<string, string>> UnmarshalGammuIdentifyAsync(StreamReader reader)
        {
            var identify = new Dictionary<string, string>();
            while(!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if(string.IsNullOrEmpty(line))
                {
                    continue;
                }
                await MainView.Instance.WriteLogAsync($"[GAMMU-IDENTIFY] {line}");
                var index = line.IndexOf(':');
                if(index == -1)
                {
                    continue;
                }
                identify[line.Substring(0, index).Trim()] = line.Substring(index + 1).Trim();
            }
            return identify;
//        Device: / dev / ttyUSB1
//Manufacturer: Quectel
//Model                : unknown(EC200A)
//Firmware: EC200ACNHAR01A07M16
//IMEI                 : 868703052177490
//SIM IMSI             : 454120640655691
        }
    }
}
