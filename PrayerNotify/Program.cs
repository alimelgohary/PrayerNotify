using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Text;

namespace PrayerNotify
{
    internal class Program
    {
        static string[] hijriMonth = new string[13] { "", "Muharram", "Safar", "Rabi3 Awal", "Rabi3 Thany", "Gamad Awal", "Gamad Thany", "Ragab", "Sha3ban", "Ramadan", "Shawal", "Zo Elqe3da", "Zo El7egga" };
        static string ClearReturnToBegining = new StringBuilder().Append("\r").Append(' ', Console.BufferWidth).Append("\r").ToString();
        static HttpClient client = new HttpClient();
        static async Task Main(string[] args)
        {
            Console.CursorVisible = false;
            var dt = DateTime.Now;
            Root? r = await TryGetRootAsync("31.21452", "31.35798", (int)Methods.Egyptian_General_Authority_of_Survey);

            if (r == null || r?.code != 200)
            {
                ErrorHappened("Something Went Wrong");
            }

            Datum d = r.data[dt.Day - 1];

            Console.WriteLine($"{dt.DayOfWeek} {d.date.hijri.day} {hijriMonth[d.date.hijri.month.number]} {d.date.hijri.year} \\\\ {dt.ToString("dd MMM yyyy")}{Environment.NewLine}");

            Prayer[] prayers = new Prayer[6];
            
            FillTimes(prayers, d);

            PrintTimes(prayers);
            Console.WriteLine(Environment.NewLine);
            AlarmClock[] alarms = new AlarmClock[12];
            CreateAlarmsForPrayerAndIqama(dt, prayers, alarms);

            Timer timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler((sender, e) =>
            {
                var dt = DateTime.Now;
                TimeSpan t;
                string message;
                for (int i = 0; i < prayers.Length; i++)
                {
                    t = new DateTime(dt.Year, dt.Month, dt.Day, prayers[i].Time.Hour, prayers[i].Time.Minute, 0) - DateTime.Now;
                    if (t.TotalSeconds > 0)
                    {
                        Console.CursorTop--;
                        Console.Write(ClearReturnToBegining);
                        message = $"Remaining time for {Prayer.salatIqama.Keys.ElementAt(i)} is {t.ToString(@"hh\:mm\:ss")}";
                        Console.Write(message);

                        break;
                    }
                }
            });
            timer.Interval = 1000;
            timer.Start();

            CreateNewDayAlarm(dt);
            Console.ReadLine();

        }

        private static void CreateAlarmsForPrayerAndIqama(DateTime dt, Prayer[] times, AlarmClock[] alarms)
        {
            int j = 0;
            foreach (var item in times)
            {
                
                alarms[j] = new AlarmClock(new DateTime(dt.Year, dt.Month, dt.Day, item.Time.Hour, item.Time.Minute, 0));
                alarms[j].alarmEvent += AlarmMe;

                int iqama = item.Iqama != 0 ? item.Iqama - 5 : 0;
                TimeOnly t = item.Time.Add(TimeSpan.FromMinutes(iqama)); // time before iqama by 5 min

                j++;
                alarms[j] = new AlarmClock(new DateTime(dt.Year, dt.Month, dt.Day, t.Hour, t.Minute, 0));
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

        private static void PrintTimes(Prayer[] times)
        {
            foreach (var item in times)
            {
                Console.WriteLine($"\t\t{item.Name}: {item.Time:hh:mm tt}");
            }
        }

        private static void FillTimes(Prayer[] prayers, Datum d)
        {
            prayers[0] = new Prayer("Fajr", TimeOnly.Parse(d.timings.Fajr[..5]));
            prayers[1] = new Prayer("Sunrise", TimeOnly.Parse(d.timings.Sunrise[..5]));
            prayers[2] = new Prayer("Dhuhr", TimeOnly.Parse(d.timings.Dhuhr[..5]));
            prayers[3] = new Prayer("Asr", TimeOnly.Parse(d.timings.Asr[..5]));
            prayers[4] = new Prayer("Maghrib", TimeOnly.Parse(d.timings.Maghrib[..5]));
            prayers[5] = new Prayer("Isha", TimeOnly.Parse(d.timings.Isha[..5]));
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
        

    }

    public class AlarmClock
    {
        public ElapsedEventHandler? alarmEvent;
        private Timer timer;

        public AlarmClock(DateTime alarmTime)
        {
            timer = new Timer();
            timer.Elapsed += timer_Elapsed;
            double remaining = (alarmTime - DateTime.Now).TotalMilliseconds;
            if (remaining < 0)
            {
                timer.Dispose();
            }
            else
            {
                timer.Interval = remaining;
                timer.Start();
                timer.AutoReset = false;
            }

        }

        void timer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            alarmEvent?.Invoke(this, e);
            timer.Stop();
            timer.Dispose();
        }

    }
}