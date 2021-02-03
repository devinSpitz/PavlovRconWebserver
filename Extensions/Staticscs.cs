using System;
using System.Collections;
using System.Collections.Generic;

namespace PavlovRconWebserver.Extensions
{
    public static class Statics
    {
        public static IDictionary<string, TimeSpan> BanList { get; } = new Dictionary<string, TimeSpan>()
        {
            {"unlimited", new TimeSpan(9999999, 0, 0, 0, 0)},
            {"5min", new TimeSpan(0, 0, 5, 0, 0)},
            {"10min", new TimeSpan(0, 0, 10, 0, 0)},
            {"30min", new TimeSpan(0, 0, 30, 0, 0)},
            {"1h", new TimeSpan(0, 1, 0, 0, 0)},
            {"3h", new TimeSpan(0, 3, 0, 0, 0)},
            {"6h", new TimeSpan(0, 6, 0, 0, 0)},
            {"12h", new TimeSpan(0, 12, 0, 0, 0)},
            {"24h", new TimeSpan(0, 24, 0, 0, 0)},
            {"48h", new TimeSpan(2, 0, 0, 0, 0)},
        };
    }
}