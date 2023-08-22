using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BongleMonitor
{
    public struct LogModel
    {
        public LogModel(string app, string level, string message)
        {
            App = app;
            Level = level;
            Message = message;
        }
        public string Level { get;}
        public string App { get; }
        public string Message { get; }

        public override string ToString()
        {
            var appPart = $"[{App}]";
            while (appPart.Length < 15)
            {
                appPart += " ";
            }
            return $"{appPart} | [{DateTime.Now}] [{Level.ToUpper()}] {Message}";
        }
    }
}
