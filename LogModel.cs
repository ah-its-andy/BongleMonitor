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
            var len = appPart.Length;
            while (len < 15)
            {
                appPart += "\t";
                len = len + 3;
            }
            return $"{appPart} | [{DateTime.Now}] [{Level.ToUpper()}] {Message}";
        }
    }
}
