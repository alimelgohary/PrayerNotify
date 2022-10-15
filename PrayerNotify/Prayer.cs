using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrayerNotify;

internal class Prayer

{
    public static string[] salats = new string[] { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };


    string name;
    DateTime time;
    public readonly int Iqama;
    public string Name { get => name; set => name = salats.Contains(value) ? value : salats[0]; }
    public DateTime Time { get => time; set => time = value; }



    private Prayer(string name, DateTime currentDay, TimeOnly time, int iqama)
    {
        Name = name;
        Time = new DateTime(currentDay.Year, currentDay.Month, currentDay.Day, time.Hour, time.Minute, time.Second);
        Iqama = iqama;
    }
    public static Prayer[] GetPrayers(Datum d, Dictionary<string, int> iqamaDict)
    {
        Prayer[] prayers = new Prayer[6];
        TimeOnly fajr = TimeOnly.Parse(d.timings.Fajr[..5]);
        TimeOnly sunrise = TimeOnly.Parse(d.timings.Sunrise[..5]);
        TimeOnly dhuhr = TimeOnly.Parse(d.timings.Dhuhr[..5]);
        TimeOnly asr = TimeOnly.Parse(d.timings.Asr[..5]);
        TimeOnly maghrib = TimeOnly.Parse(d.timings.Maghrib[..5]);
        TimeOnly isha = TimeOnly.Parse(d.timings.Isha[..5]);

        DateTime dt = DateTime.Parse(d.date.gregorian.date);

        prayers[0] = new Prayer(salats[0], dt, fajr,    iqamaDict[salats[0]]);
        prayers[1] = new Prayer(salats[1], dt, sunrise, iqamaDict[salats[1]]);
        prayers[2] = new Prayer(salats[2], dt, dhuhr,   iqamaDict[salats[2]]);
        prayers[3] = new Prayer(salats[3], dt, asr,     iqamaDict[salats[3]]);
        prayers[4] = new Prayer(salats[4], dt, maghrib, iqamaDict[salats[4]]);
        prayers[5] = new Prayer(salats[5], dt, isha,    iqamaDict[salats[5]]);

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
