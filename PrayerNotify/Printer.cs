namespace PrayerNotify;

static class Printer
{
    static readonly string[] HIJRRI_MONTHS = new[] { "", "Muharram", "Safar", "Rabi3 Awal", "Rabi3 Thany", "Gamad Awal", "Gamad Thany", "Ragab", "Sha3ban", "Ramadan", "Shawal", "Zo Elqe3da", "Zo El7egga" };

    public static void ConsoleWriteLineColor(string message = "")
    {
        ConsoleWriteColor(message + '\n');
    }
    public static void ConsoleWriteColor(string message)
    {
        Console.ForegroundColor = (ConsoleColor)new Random().Next(1, 15);
        Console.Write(message);
    }
    public static void PrintPrayerTimes(Prayer[] prayers)
    {
        foreach (var item in prayers)
        {
            ConsoleWriteLineColor($"\t\t{item.Name}: {item.Time:hh:mm tt}");
        }
        ConsoleWriteLineColor(Environment.NewLine);
    }
    public static void PrintMetaData(Datum prayerData, Settings settings)
    {
        ConsoleWriteLineColor($"{prayerData.date.gregorian.weekday.en} {prayerData.date.hijri.day} {HIJRRI_MONTHS[prayerData.date.hijri.month.number]} {prayerData.date.hijri.year} \\\\ {prayerData.date.readable}");
        ConsoleWriteLineColor();
        ConsoleWriteLineColor($"Lat: {prayerData.meta.latitude}, Lng: {prayerData.meta.longitude}, Method: {prayerData.meta.method.name}, Remind Me {settings.RemindMeBefore} Minutes Before Iqama");
        ConsoleWriteLineColor();
        
        ConsoleWriteColor(string.Join(", ", settings.Iqama));
        
        ConsoleWriteLineColor($"{Environment.NewLine}");
    }

}
