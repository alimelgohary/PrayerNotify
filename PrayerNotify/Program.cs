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
        

        static async Task Main()
        {
            string settingsPath = "settings.json";
            
            ReadWriteJson(settingsPath, out string lat, out string lng, out int method, out int remindMeBefore, out List<Settings.IqamaObject> iqama);

            DateTime dt = DateTime.Now;

            Root? r = await TryGetRootAsync(lat, lng, method);

            if (r == null || r?.code != 200)
            {
                ErrorHappened("Something Went Wrong");
                return;
            }

            int currentDay;
            var ishaTime = TimeOnly.Parse(r.data[dt.Day - 1].timings.Isha[..5]);
            DateTime ishaDateTime = new(dt.Year, dt.Month, dt.Day, ishaTime.Hour, ishaTime.Minute, ishaTime.Second);
            if ((DateTime.Now - ishaDateTime) > TimeSpan.FromMinutes(iqama.Find(x => x.Name == Prayer.salats[Prayer.salats.GetUpperBound(0)]).Value - remindMeBefore))  //Last iqama in day ocurred?
            {
                currentDay = dt.Day + 1;
            }
            else
            {
                currentDay = dt.Day;
            }

            Datum d = r.data[currentDay - 1];

            ConsoleWriteLineColor($"{d.date.gregorian.weekday.en} {d.date.hijri.day} {hijriMonth[d.date.hijri.month.number]} {d.date.hijri.year} \\\\ {d.date.readable}");
            ConsoleWriteLineColor("");
            ConsoleWriteLineColor($"Lat: {lat}, Lng: {lng}, Method: {(Settings.Methods)method}, Remind Me {remindMeBefore} Minutes Before Iqama");
            ConsoleWriteLineColor("");
            foreach (var item in iqama)
            {
                ConsoleWriteColor($"{item.Name}: {item.Value} minutes, ");
            }
            ConsoleWriteLineColor($"{Environment.NewLine}");

            Prayer[] prayers = Prayer.GetPrayers(d, Settings.ListToDict(iqama));

            Prayer.PrintTimes(prayers);

            ConsoleWriteLineColor(Environment.NewLine);

            AlarmClock[] alarms = new AlarmClock[12];

            CreateAlarmsForPrayerAndIqama(prayers, alarms, remindMeBefore);

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
                        ConsoleWriteColor(ClearReturnToBegining);
                        message = $"Remaining time for {iqama[i].Name} is {t:hh\\:mm\\:ss}";
                        ConsoleWriteColor(message + new string(' ', Console.BufferWidth - message.Length - 1));
                        break;
                    }
                }
            });
            timer.Interval = 1000;
            timer.Start();

            Console.ReadLine();

        }

        private static void ReadWriteJson(string path, out string lat, out string lng, out int method, out int remindMeBefore, out List<Settings.IqamaObject> iqama)
        {
            Settings s = new Settings();
            try
            {
                s = Settings.FromJson(path);

            }
            catch (Exception e)
            {
                ConsoleWriteLineColor("Wrong Json Format");
                ConsoleWriteLineColor(e.Message);
            }

            lat = s.Lat;
            lng = s.Lng;
            method = s.Method;
            iqama = s.Iqama;
            remindMeBefore = s.RemindMeBefore;
            double dd;
            if (lat == string.Empty || lng == string.Empty || method == 0 || iqama.Count != Prayer.salats.Length)
            {
                ConsoleWriteLineColor("No settings present");
            lat: ConsoleWriteColor("Enter City Latitude: ");
                lat = Console.ReadLine();
                if (!double.TryParse(lat, out dd))
                {
                    goto lat;
                }
                s.Lat = lat;

            lng: ConsoleWriteColor("Enter City Longtuide: ");
                lng = Console.ReadLine();
                if (!double.TryParse(lat, out dd))
                {
                    goto lng;
                }
                s.Lng = lng;
            method: ConsoleWriteLineColor("Methods: ");
                Array.ForEach(Enum.GetNames(typeof(Settings.Methods)), x => ConsoleWriteLineColor($"\t\t{(int)Enum.Parse(typeof(Settings.Methods), x)} - {x}"));
                ConsoleWriteColor("Enter method number: ");
                if (!int.TryParse(Console.ReadLine(), out method))
                {
                    goto method;
                }
                s.Method = method;

                ConsoleWriteLineColor("Enter Iqama for each salat in Minutes");
                iqama = new List<Settings.IqamaObject>();
                for (int i = 0; i < Prayer.salats.Length; i++)
                {
                Iqamas: ConsoleWriteColor($"\t\t{Prayer.salats[i]} : ");
                    if (!int.TryParse(Console.ReadLine(), out int minutes))
                    {
                        goto Iqamas;
                    }
                    iqama.Add(new Settings.IqamaObject() { Name = Prayer.salats[i], Value = minutes });
                }
                s.Iqama = iqama;

            remind: ConsoleWriteColor("Alarm me before Iqama by (minutes): ");
                if (!int.TryParse(Console.ReadLine(), out remindMeBefore))
                {
                    goto remind;
                }
                s.RemindMeBefore = remindMeBefore;

                Settings.ToJsonFile(path, s);
                Console.Clear();
            }
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
            HttpClient client = new();
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
            ConsoleWriteLineColor(message);
            ConsoleWriteLineColor("Press R to retry or any key to Exit");
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
        public static void ConsoleWriteLineColor(string message)
        {
            ConsoleWriteColor(message + '\n');
        }
        public static void ConsoleWriteColor(string message)
        {
            Console.ForegroundColor = (ConsoleColor)new Random().Next(1, 15);
            Console.Write(message);
            //Console.ForegroundColor = ConsoleColor.White;
        }

    }
}