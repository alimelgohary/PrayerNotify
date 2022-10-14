using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrayerNotify
{
    internal class Prayer

    {
        public static Dictionary<string, int> salatIqama = new() {
            { "Fajr", 20 },
            { "Sunrise", 0 },
            { "Dhuhr", 15},
            { "Asr", 15},
            { "Maghrib", 10},
            { "Isha", 15}

        };

        string name;
        TimeOnly time;
        public readonly int Iqama;
        public string Name { get => name; set => name = salatIqama.Keys.Contains(value) ? value : salatIqama.Keys.First(); }
        public TimeOnly Time { get => time; set => time = value; }
        


        public Prayer(string name, TimeOnly time)
        {
            Name = name;
            Time = time;
            Iqama = salatIqama[Name];
        }
    }
}
