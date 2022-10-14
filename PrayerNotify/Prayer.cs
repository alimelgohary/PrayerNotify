using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrayerNotify;

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
    DateTime time;
    public readonly int Iqama;
    public string Name { get => name; set => name = salatIqama.ContainsKey(value) ? value : salatIqama.Keys.First(); }
    public DateTime Time { get => time; set => time = value; }



    private Prayer(string name, DateTime currentDay, TimeOnly time)
    {
        Name = name;
        Time = new DateTime(currentDay.Year, currentDay.Month, currentDay.Day, time.Hour, time.Minute, time.Second);
        Iqama = salatIqama[Name];
    }
    public static Prayer[] GetPrayers(Datum d)
    {
        Prayer[] prayers = new Prayer[6];
        TimeOnly fajr       = TimeOnly.Parse(d.timings.Fajr[..5]);
        TimeOnly sunrise    = TimeOnly.Parse(d.timings.Sunrise[..5]);
        TimeOnly dhuhr      = TimeOnly.Parse(d.timings.Dhuhr[..5]);
        TimeOnly asr        = TimeOnly.Parse(d.timings.Asr[..5]);
        TimeOnly maghrib    = TimeOnly.Parse(d.timings.Maghrib[..5]);
        TimeOnly isha       = TimeOnly.Parse(d.timings.Isha[..5]);

        DateTime dt = DateTime.Parse(d.date.gregorian.date);

        prayers[0] = new Prayer("Fajr", dt, fajr);
        prayers[1] = new Prayer("Sunrise", dt, sunrise);
        prayers[2] = new Prayer("Dhuhr", dt, dhuhr);
        prayers[3] = new Prayer("Asr", dt, asr);
        prayers[4] = new Prayer("Maghrib", dt, maghrib);
        prayers[5] = new Prayer("Isha", dt, isha);

        return prayers;
    }
    public static void PrintTimes(Prayer[] times)
    {
        foreach (var item in times)
        {
            Console.WriteLine($"\t\t{item.Name}: {item.Time:hh:mm tt}");
        }
    }
}
