using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrayerNotify;

internal class Prayer

{
    public static string[] SALATS = new string[] { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };


    string name;
    DateTime time;
    public readonly int Iqama;
    public string Name { get => name; set => name = SALATS.Contains(value) ? value : SALATS[0]; }
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

        prayers[0] = new Prayer(SALATS[0], dt, fajr,    iqamaDict[SALATS[0]]);
        prayers[1] = new Prayer(SALATS[1], dt, sunrise, iqamaDict[SALATS[1]]);
        prayers[2] = new Prayer(SALATS[2], dt, dhuhr,   iqamaDict[SALATS[2]]);
        prayers[3] = new Prayer(SALATS[3], dt, asr,     iqamaDict[SALATS[3]]);
        prayers[4] = new Prayer(SALATS[4], dt, maghrib, iqamaDict[SALATS[4]]);
        prayers[5] = new Prayer(SALATS[5], dt, isha,    iqamaDict[SALATS[5]]);

        return prayers;
    }
    
}
