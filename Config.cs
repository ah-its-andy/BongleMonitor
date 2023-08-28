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
using System.Net.NetworkInformation;

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
            var shortDev = System.IO.Path.GetFileNameWithoutExtension(dev);
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
            builder.AppendLine($"logfile = /var/log/gammu-smsd/{shortDev}.log");
            builder.AppendLine("# Increase for debugging information");
            builder.AppendLine("debuglevel = 0");

            builder.AppendLine("# Paths where messages are stored");
            builder.AppendLine($"inboxpath = /share/gammu-smsd/{shortDev}");
            builder.AppendLine($"outboxpath = /share/gammu-smsd/{shortDev}");
            builder.AppendLine($"sentsmspath = /share/gammu-smsd/{shortDev}");
            builder.AppendLine($"errorsmspath = /share/gammu-smsd/{shortDev}");
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
                MainView.Instance.Log("GAMMU-IDENTIFY", "INFO", line);
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

        public static string GetPPPDisconnectConfig()
        {
            return $"ABORT \"ERROR\" {Environment.NewLine}" +
            $"ABORT \"NO DIALTONE\" {Environment.NewLine}" +
            $"SAY \"\\NSending break to the modem\\n\" {Environment.NewLine}" +
            $"\"\"\\k\" {Environment.NewLine}" +
            $"\"\"+++ATH\" {Environment.NewLine}" +
            $"SAY \"\\nGood bye !\\n\"";
        }

        public static string GetPPPConnectConfig(string apn, string dailNumber)
        {
            return $"#连续15秒，收到以下字符，则退出执行 {Environment.NewLine}" +
            $"TIMEOUT 15 {Environment.NewLine}" +
            $"ABORT   \"BUSY\" {Environment.NewLine}" +
            $"ABORT   \"ERROR\" {Environment.NewLine}" +
            $"ABORT   \"NO ANSWER\" {Environment.NewLine}" +
            $"ABORT   \"NO CARRTER\" {Environment.NewLine}" +
            $"ABORT   \"NO DIALTONE\" {Environment.NewLine}" +

            $"# 40秒内没有收到指定字符，则退出 {Environment.NewLine}" +
            $"# 例如 OK \rATZ,发送ATZ，希望收到的是OK {Environment.NewLine}" +
            $"\"\"AT {Environment.NewLine}" +
            $"OK \\rATZ {Environment.NewLine}" +

            $"# 使用IPV4，建立连接，联通为3gnet,移动为cmnet，文末给出各运营商配置 {Environment.NewLine}" +
            $"OK \\rAT+CGDCONT=1,\"IP\",\"{apn}\" {Environment.NewLine}" +

            $"# 拨号,*99#是联通的拨号号码，*98*1#是移动 {Environment.NewLine}" +
            $"OK-AT-OK ATDT{dailNumber} {Environment.NewLine}" +
            $"CONNECT \\d\\c {Environment.NewLine}";
        }

        public static string GetPPPConfig(string dev)
        {
            var shortDev = System.IO.Path.GetFileNameWithoutExtension(dev);
            return $"# /etc/ppp/peers/{shortDev}-ppp {Environment.NewLine}" +
            $"# Usage: sudo pppd call rasppp {Environment.NewLine}" +
            $"# 连接调试时隐藏密码 {Environment.NewLine}" +
            $"hide-password {Environment.NewLine}" +
            $"# 该手机不需要身份验证4 {Environment.NewLine}" +
            $"noauth {Environment.NewLine}" +
            $"# 用于呼叫控制脚本 {Environment.NewLine}" +
            $"connect '/usr/sbin/chat -s -v -f /etc/ppp/peers/{shortDev}-ppp-chat-connect'{Environment.NewLine}" +
            $"# 断开连接脚本{Environment.NewLine}" +
            $"disconnect '/usr/sbin/chat -s -v -f /etc/ppp/peers/{shortDev}-ppp-chat-disconnect'{Environment.NewLine}" +
            $"# 调试信息{Environment.NewLine}" +
            $"debug{Environment.NewLine}" +
            $"# 4G模块{Environment.NewLine}" +
            $"{dev}{Environment.NewLine}" +
            $"# 串口波特率{Environment.NewLine}" +
            $"115200{Environment.NewLine}" +
            $"# 使用默认路由 {Environment.NewLine}" +
            $"defaultroute{Environment.NewLine}" +
            $"# 不指定默认IP{Environment.NewLine}" +
            $"noipdefault{Environment.NewLine}" +
            $"# 不使用PPP压缩{Environment.NewLine}" +
            $"novj{Environment.NewLine}" +
            $"novjccomp{Environment.NewLine}" +
            $"noccp{Environment.NewLine}" +
            $"ipcp-accept-local{Environment.NewLine}" +
            $"ipcp-accept-remote{Environment.NewLine}" +
            $"local{Environment.NewLine}" +
            $"# 最好锁定串行总线{Environment.NewLine}" +
            $"lock{Environment.NewLine}" +
            $"dump{Environment.NewLine}" +
            $"# 保持pppd连接到终端{Environment.NewLine}" +
            $"nodetach{Environment.NewLine}" +
            $"# 用户名 密码{Environment.NewLine}" +
            $"# user{Environment.NewLine}" +
            $"# password{Environment.NewLine}" +
            $"#移动，联通拨号不需要用户名密码，文末给出不同运营商的配置 {Environment.NewLine}" +
            $"# 硬件控制流 {Environment.NewLine}" +
            $"crtscts {Environment.NewLine}" +
            $"remotename 3gppp {Environment.NewLine}" +
            $"ipparam 3gppp {Environment.NewLine}" +
            $"# 请求最多两个DNS服务器地址 {Environment.NewLine}" +
            $"usepeerdns";
        }

        static string GetLocalIPAddresses(NetworkInterface networkInterface)
        {
            if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();

                foreach (UnicastIPAddressInformation ipAddressInfo in ipProperties.UnicastAddresses)
                {
                    if (ipAddressInfo.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ipAddressInfo.Address.ToString();
                    }
                }
            }

            return "";
        }

        public static string GetV2RayConfig(string dev, string interfaceName)
        {
            var shortDev = System.IO.Path.GetFileNameWithoutExtension(dev);
            var iface = NetworkInterface.GetAllNetworkInterfaces().Where(x=> x.Id == interfaceName || x.Name == interfaceName).FirstOrDefault();
            if(iface == null)
            {
                MainView.Instance.Log("V2RAY", "ERROR", $"Interface {interfaceName} not found.");
                return "";
            }
            var localIp = GetLocalIPAddresses(iface);
            MainView.Instance.Log("V2RAY", "INFO", $"Found interface ID:{iface.Id} Name:{iface.Name} IP:{localIp}.");

            var json = @"{
    ""log"": {
        ""loglevel"": ""warning""
    },
    ""routing"": {
        ""domainStrategy"": ""AsIs"",
        ""rules"": [
            {
                ""type"": ""field"",
                ""ip"": [
                    ""geoip:private""
                ],
                ""outboundTag"": ""block""
            }
        ]
    },
    ""inbounds"": [
        {
           ""port"": 10804,
           ""protocol"": ""http"",
           ""listen"": ""0.0.0.0"",
           ""sniffing"": {
              ""enabled"": false,
              ""destOverride"": [
                 ""http"",
                 ""tls""
              ]
           },
           ""settings"": {
              ""auth"": ""noauth"",
              ""udp"": true,
              ""allowTransparent"": false
           }
        },
        {
            ""port"": 10808,
            ""protocol"": ""socks"",
            ""listen"": ""0.0.0.0"",
            ""sniffing"": {
                 ""enabled"": true,
                 ""destOverride"": [""http"", ""tls""]
             },
             ""settings"": {
                 ""auth"": ""noauth"",
                 ""udp"": true
             }
        },
        {
            ""listen"": ""0.0.0.0"",
            ""port"": 1234,
            ""protocol"": ""vmess"",
            ""settings"": {
                ""clients"": [
                    {
                        ""id"": ""896ed986-bba7-425c-88a3-55297631bebb""
                    }
                ]
            },
            ""streamSettings"": {
                ""network"": ""ws"",
                ""security"": ""none"",
                ""wsSettings"": {
                  ""path"": ""/Videos/c9ba5d84b4cfeafd3527c7d6956257db/stream.mkv""
                }
            }
        }
    ],
    ""outbounds"": [
        {
            ""protocol"": ""freedom"",
            ""tag"": ""direct"",
            ""sendThrough"": ""{{interface_ip}}""
        },
        {
            ""protocol"": ""blackhole"",
            ""tag"": ""block""
        }
    ]
}";

            return json.Replace("{{interface_ip}}", localIp);
        }
    }
}
