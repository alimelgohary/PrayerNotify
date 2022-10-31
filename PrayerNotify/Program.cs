using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Text;
using System.Text.Json;
using System.IO;

namespace PrayerNotify
{
    internal class Program
    {
        static readonly string CLEAR_RETURN_TO_BEGINING = new StringBuilder().Append('\r').Append(' ', Console.BufferWidth).Append('\r').ToString();

        static async Task Main()
        {
            string settingsPath = "settings.json";
            DateTime dt = DateTime.Now;

            var settings = Settings.JsonToSettings(settingsPath);
            

            newRequest: ResponseRoot? r = await TryGetRootAsync(settings.Lat, settings.Lng, settings.Method, dt);

            if (r == null)
            {
                FatalErrorHappened("R = null");
                return;
            }
            if (r?.code != 200)
            {
                FatalErrorHappened($"Response Code: {r?.code} Status: {r?.status}");
                return;
            }

            int currentDay;
            var ishaTime = TimeOnly.Parse(r.data[dt.Day - 1].timings.Isha[..5]);
            DateTime ishaDateTime = new(dt.Year, dt.Month, dt.Day, ishaTime.Hour, ishaTime.Minute, ishaTime.Second);

            //Last alarm in day ocurred?
            if ((DateTime.Now - ishaDateTime) > TimeSpan.FromMinutes(settings.Iqama.Find(x => x.Name == Prayer.SALATS[Prayer.SALATS.GetUpperBound(0)]).Value - settings.RemindMeBefore))
            {
                currentDay = dt.Day + 1;
                if(dt.IsLastDayOfMonth()) // last isha in the month ocurred
                {
                    dt = dt.AddDays(1);
                    goto newRequest;
                }
            }
            else
            {
                currentDay = dt.Day;
            }

            Datum prayerData = r.data[currentDay - 1];


            Prayer[] prayers = Prayer.GetPrayers(prayerData, Settings.ListToDict(settings.Iqama));
            Printer.PrintMetaData(prayerData, settings);

            Printer.PrintPrayerTimes(prayers);

            var alarms = CreateAlarmsForPrayerAndIqama(prayers, settings.RemindMeBefore);
            RemainingForSalat(settings.Iqama, prayers);

            Console.ReadLine();

        }
        static void RemainingForSalat(List<Settings.IqamaObject> iqamaList, Prayer[] prayers)
        {
            // Remaining time for Salat
            Console.CursorVisible = false;
            Timer timer = new();
            timer.Elapsed += new ElapsedEventHandler((sender, e) =>
            {

                TimeSpan t;
                string message;
                for (int i = 0; i < prayers.Length; i++)
                {
                    t = prayers[i].Time - DateTime.Now;
                    if (t.TotalSeconds > 0)
                    {
                        Console.CursorTop--;
                        Printer.ConsoleWriteColor(CLEAR_RETURN_TO_BEGINING);
                        message = $"Remaining time for {iqamaList[i].Name} is {t:hh\\:mm\\:ss}";
                        Printer.ConsoleWriteColor(message + new string(' ', Console.BufferWidth - message.Length - 1));
                        break;
                    }
                }
            });
            timer.Interval = 1000;
            timer.Start();
        }
        static async Task<ResponseRoot?> TryGetRootAsync(string lat, string lng, int method, DateTime dt)
        {
            string request = @$"http://api.aladhan.com/v1/calendar?latitude={lat}&longitude={lng}&method={method}&month={dt.Month}&year={dt.Year}";
            ResponseRoot? r = null;
            try
            {
                r = await GetRootAsync(request);
            }
            catch (Exception e)
            {
                FatalErrorHappened(e.Message);
            }
            return r;
        }
        static async Task<ResponseRoot?> GetRootAsync(string path)
        {
            HttpClient client = new();
            ResponseRoot? root = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                root = await response.Content.ReadFromJsonAsync<ResponseRoot>();
            }
            return root;
        }
        static AlarmClock[] CreateAlarmsForPrayerAndIqama(Prayer[] times, int remindMeBefore)
        {
            var alarms = new AlarmClock[12];
            int j = 0;
            foreach (var item in times)
            {

                alarms[j] = new AlarmClock(item.Time);
                alarms[j].alarmEvent += AlarmMe;

                int iqamaRemaining = item.Iqama != 0
                                   ? item.Iqama - remindMeBefore
                                   : 0;
                DateTime beforeIqama = item.Time.AddMinutes(iqamaRemaining); // time before iqama by X minutes

                j++;
                alarms[j] = new AlarmClock(beforeIqama);
                alarms[j].alarmEvent += AlarmMe;
                if (j == alarms.Length - 1)
                {
                    alarms[j].alarmEvent += new ElapsedEventHandler((sender, e) =>
                    {
                        RestartApp();
                    });
                }
                j++;

            }
            return alarms;
        }
        static void AlarmMe(object? sender, ElapsedEventArgs? e)
        {
            for (int i = 0; i < 3; i++)
            {
                Console.Beep();
            }
        }
        static void FatalErrorHappened(string message)
        {
            Printer.ConsoleWriteLineColor($"Something went wrong: {message}");
            Printer.ConsoleWriteLineColor("Press R to retry or any key to Exit");
            ConsoleKey k = Console.ReadKey().Key;
            if (k == ConsoleKey.R)
            {
                RestartApp();
            }
            else
            {
                Environment.Exit(0);
            }

        }
        static void RestartApp()
        {
            Console.Clear();
            Process.Start(AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(0);
        }
       
    }
    public static class Extensions
    {
        public static bool IsLastDayOfMonth(this DateTime dt)
        {
            return dt.AddDays(1).Day == 1;
        }
    }
}
