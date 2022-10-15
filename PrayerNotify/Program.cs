using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Text;
using System.Text.Json;

namespace PrayerNotify
{
    internal class Program
    {
        static readonly string[] hijriMonth = new string[13] { "", "Muharram", "Safar", "Rabi3 Awal", "Rabi3 Thany", "Gamad Awal", "Gamad Thany", "Ragab", "Sha3ban", "Ramadan", "Shawal", "Zo Elqe3da", "Zo El7egga" };
        static readonly string ClearReturnToBegining = new StringBuilder().Append('\r').Append(' ', Console.BufferWidth).Append('\r').ToString();
        static DateTime dt;
        static readonly HttpClient client = new();
        static string settingsPath = "settings.json";
        static async Task Main()
        {
            Console.CursorVisible = false;
            Settings s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath));
            string lat = s.Lat;
            string lng = s.Lng; 
            int method = s.Method;
            int remindMeBefore = s.RemindMeBefore;
            double dd;
            if (lat == String.Empty || lng == String.Empty || method == 0)
            {
                Console.WriteLine("No settings present");
            lat: Console.Write("Enter City Latitude: ");
                lat = Console.ReadLine();
                if (!double.TryParse(lat, out dd))
                {
                    goto lat;
                }
                s.Lat = lat;

            lng: Console.Write("Enter City Longtuide: ");
                lng = Console.ReadLine();
                if (!double.TryParse(lat, out dd))
                {
                    goto lng;
                }
                s.Lng = lng;
            method: Console.WriteLine("Methods: ");
                Array.ForEach(Enum.GetNames(typeof(Settings.Methods)), x => Console.WriteLine($"\t\t{(int)Enum.Parse(typeof(Settings.Methods), x)} - {x}"));
                Console.Write("Enter method number: ");
                if (!int.TryParse(Console.ReadLine(), out method))
                {
                    goto method;
                }
                s.Method = method;

            remind: Console.Write("Alarm me before Iqama by (min): ");
                if (!int.TryParse(Console.ReadLine(), out remindMeBefore))
                {
                    goto remind;
                }
                s.RemindMeBefore = remindMeBefore;

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(s));
            }

            dt = DateTime.Now;
            //Root? r = await TryGetRootAsync("31.21452", "31.35798", (int)Methods.Egyptian_General_Authority_of_Survey);
            Root? r = await TryGetRootAsync(lat, lng, method);

            if (r == null || r?.code != 200)
            {
                ErrorHappened("Something Went Wrong");
                return;
            }

            int currentDay;
            var ishaTime = TimeOnly.Parse(r.data[dt.Day - 1].timings.Isha[..5]);
            DateTime ishaDateTime = new(dt.Year, dt.Month, dt.Day, ishaTime.Hour, ishaTime.Minute, ishaTime.Second);
            if ((DateTime.Now - ishaDateTime) > TimeSpan.FromMinutes(Prayer.salatIqama["Isha"] - remindMeBefore))  //Last iqama in day ocurred?
            {
                currentDay = dt.Day + 1;
            }
            else
            {
                currentDay = dt.Day;
            }

            Datum d = r.data[currentDay - 1];

            Console.WriteLine($"{d.date.gregorian.weekday.en} {d.date.hijri.day} {hijriMonth[d.date.hijri.month.number]} {d.date.hijri.year} \\\\ {d.date.readable} \\\\ Lat: {lat}, Lng: {lng}{Environment.NewLine} ");

            Prayer[] prayers = Prayer.GetPrayers(d);

            Prayer.PrintTimes(prayers);

            Console.WriteLine(Environment.NewLine);

            AlarmClock[] alarms = new AlarmClock[12];

            CreateAlarmsForPrayerAndIqama(prayers, alarms, remindMeBefore);

            // Remaining time for Salat
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
                        Console.Write(ClearReturnToBegining);
                        message = $"Remaining time for {Prayer.salatIqama.Keys.ElementAt(i)} is {t:hh\\:mm\\:ss}";
                        Console.Write(message + new string(' ', Console.BufferWidth - message.Length - 1));
                        break;
                    }
                }
            });
            timer.Interval = 1000;
            timer.Start();

            Console.ReadLine();

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
        private static void CreateAlarmsForPrayerAndIqama(Prayer[] times, AlarmClock[] alarms, int remindMeBefore)
        {
            int j = 0;
            foreach (var item in times)
            {

                alarms[j] = new AlarmClock(item.Time);
                alarms[j].alarmEvent += AlarmMe;

                int iqamaRemaining = item.Iqama != 0
                                   ? item.Iqama - remindMeBefore
                                   : 0;
                DateTime beforeIqama = item.Time.AddMinutes(iqamaRemaining); // time before iqama by 5 min

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
        }
        static void AlarmMe(object? sender, ElapsedEventArgs? e)
        {
            for (int i = 0; i < 3; i++)
            {
                Console.Beep();
            }
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



    }

    public class AlarmClock
    {
        public ElapsedEventHandler? alarmEvent;
        private readonly Timer timer;

        public AlarmClock(DateTime alarmTime)
        {
            timer = new Timer();
            timer.Elapsed += Timer_Elapsed;
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

        void Timer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            alarmEvent?.Invoke(this, e);
            timer.Stop();
            timer.Dispose();
        }

    }
}