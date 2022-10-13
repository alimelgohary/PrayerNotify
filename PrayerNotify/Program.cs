using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;
using Timer = System.Timers.Timer;

namespace PrayerNotify
{
    internal class Program
    {
        static string[] hijriMonth = new string[13] {"", "Muharram", "Safar", "Rabi3 Awal", "Rabi3 Thany", "Gamad Awal", "Gamad Thany", "Ragab", "Sha3ban", "Ramadan", "Shawal", "Zo Elqe3da", "Zo El7egga" };
        static HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            var dt = DateTime.Now;
            Root? r = await TryGetRootAsync("31.21452", "31.35798", (int)Methods.Egyptian_General_Authority_of_Survey);

            if (r == null || r?.code != 200)
            {
                ErrorHappened("Something Went Wrong");
            }

            Datum d = r.data[dt.Day - 1];

            Console.WriteLine($"{dt.DayOfWeek} {d.date.hijri.day} {hijriMonth[d.date.hijri.month.number]} {d.date.hijri.year} \\\\ {dt.ToString("dd MMM yyyy")}\n");

            TimeOnly[] times = new TimeOnly[6];
            FillTimes(times, d);

            PrintTimes(times);
            Console.WriteLine("\n");
            AlarmClock[] alarms = new AlarmClock[12];
            CreateAlarmsForPrayerAndIqama(dt, times, alarms);

            Timer timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler((sender, e) =>
            {

                var dt = DateTime.Now;
                TimeSpan t;
                string message;
                for (int i = 0; i < times.Length; i++)
                {
                    t = new DateTime(dt.Year, dt.Month, dt.Day, times[i].Hour, times[i].Minute, 0) - DateTime.Now;
                    if (t.TotalSeconds > 0)
                    {
                        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                        message = $"Remaining time for {Iqama.salat[i]} is {t.ToString(@"hh\:mm\:ss")}";
                        Console.Write(message);
                        
                        //ClearLine(message.Count(x => x == '\n'));
                        //Console.Clear();
                        break;
                    }
                }


            });
            timer.Interval = 1000;
            timer.Start();
            

            CreateNewDayAlarm(dt);
            while (true) ;

        }
        private static void ClearLine(int newLines)
        {
            int currentLineCursor = Console.CursorTop - newLines;
            Console.SetCursorPosition(0, Console.CursorTop - newLines);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
        private static void CreateAlarmsForPrayerAndIqama(DateTime dt, TimeOnly[] times, AlarmClock[] alarms)
        {
            int j = 0;
            for (int i = 0; i < times.Length; i++)
            {
                int hours = times[i].Hour;
                int minutes = times[i].Minute;
                alarms[j] = new AlarmClock(new DateTime(dt.Year, dt.Month, dt.Day, hours, minutes, 0));
                alarms[j].alarmEvent += AlarmMe;
                int iqama = Iqama.salatIqama[i] != 0 ? Iqama.salatIqama[i] - 5 : 0;
                TimeOnly t = times[i].Add(new TimeSpan(0, iqama, 0)); // a new alarm after half the iqama
                alarms[++j] = new AlarmClock(new DateTime(dt.Year, dt.Month, dt.Day, t.Hour, t.Minute, 0));
                alarms[j].alarmEvent += AlarmMe;
                j++;
            }
        }
        static void AlarmMe(object? sender, EventArgs? e)
        {
            Console.WriteLine($"Alarming at {DateTime.Now.ToString("hh:mm:ss")}");

            for (int i = 0; i < 3; i++)
            {
                Console.Beep();
            }
        }
        private static void CreateNewDayAlarm(DateTime dt)
        {
            AlarmClock clock = new AlarmClock(new DateTime(dt.Year, dt.Month, dt.Day + 1));
            clock.alarmEvent += (sender, e) => RestartApp();
        }

        private static void PrintTimes(TimeOnly[] times)
        {
            Console.WriteLine($"\t\t{nameof(Datum.timings.Fajr)}:    {times[0]:hh:mm tt}");
            Console.WriteLine($"\t\t{nameof(Datum.timings.Sunrise)}: {times[1]:hh:mm tt}");
            Console.WriteLine($"\t\t{nameof(Datum.timings.Dhuhr)}:   {times[2]:hh:mm tt}");
            Console.WriteLine($"\t\t{nameof(Datum.timings.Asr)}:     {times[3]:hh:mm tt}");
            Console.WriteLine($"\t\t{nameof(Datum.timings.Maghrib)}: {times[4]:hh:mm tt}");
            Console.WriteLine($"\t\t{nameof(Datum.timings.Isha)}:    {times[5]:hh:mm tt}");


        }

        private static void FillTimes(TimeOnly[] times, Datum d)
        {
            times[0] = TimeOnly.Parse(d.timings.Fajr[..5]);
            times[1] = TimeOnly.Parse(d.timings.Sunrise[..5]);
            times[2] = TimeOnly.Parse(d.timings.Dhuhr[..5]);
            times[3] = TimeOnly.Parse(d.timings.Asr[..5]);
            times[4] = TimeOnly.Parse(d.timings.Maghrib[..5]);
            times[5] = TimeOnly.Parse(d.timings.Isha[..5]);
        }

        static async Task<Root?> TryGetRootAsync(string lat, string lng, int method)
        {
            var dt = DateTime.Now;
            string request = @$"http://api.aladhan.com/v1/calendar?latitude={lat}&longitude={lng}&method={method}&month={dt.Month}&year={dt.Year}";
            Root? r = null;
            try
            {
                r = await GetRootAsync(request);
            }
            catch (Exception e)
            {
                ErrorHappened(e.Message);
            }
            return r;
        }
        static void ErrorHappened(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press R to retry or any key to Exit");
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
        static async Task<Root?> GetRootAsync(string path)
        {

            Root? root = null;
            HttpResponseMessage response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
            {
                root = await response.Content.ReadFromJsonAsync<Root>();
            }
            return root;
        }
        enum Methods
        {
            Shia_Ithna_Ansari = 0,
            University_of_Islamic_Sciences_Karachi = 1,
            Islamic_Society_of_North_America = 2,
            Muslim_World_League = 3,
            Umm_Al_Qura_University_Makkah = 4,
            Egyptian_General_Authority_of_Survey = 5,
            Institute_of_Geophysics_University_of_Tehran = 7,
            Gulf_Region = 8,
            Kuwait = 9,
            Qatar = 10,
            Majlis_Ugama_Islam_Singapura_Singapore = 11,
            Union_Organization_islamic_de_France = 12,
            Diyanet_İşleri_Başkanlığı_Turkey = 13,
            Spiritual_Administration_of_Muslims_of_Russia = 14,
            Moonsighting_Committee_Worldwide = 15
        }
        public static class Iqama
        {
            public static string[] salat = new string[] { "Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha" };
            public static int[] salatIqama = new int[] { 20, 0, 15, 15, 10, 15 };

        }

    }

    public class AlarmClock
    {
        public ElapsedEventHandler? alarmEvent;
        public event ElapsedEventHandler? Alarm;
        private Timer timer;

        public AlarmClock(DateTime alarmTime)
        {
            timer = new Timer();
            timer.Elapsed += timer_Elapsed;
            timer.Interval = (alarmTime - DateTime.Now).TotalMilliseconds < 0 ? (alarmTime - DateTime.Now).TotalMilliseconds + 24 * 3600_000 : (alarmTime - DateTime.Now).TotalMilliseconds;
            timer.Start();
        }

        void timer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            alarmEvent?.Invoke(this, e);
            timer.Stop();
        }

    }
}