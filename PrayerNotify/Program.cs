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
        static readonly string[] HIJRRI_MONTHS = new[] { "", "Muharram", "Safar", "Rabi3 Awal", "Rabi3 Thany", "Gamad Awal", "Gamad Thany", "Ragab", "Sha3ban", "Ramadan", "Shawal", "Zo Elqe3da", "Zo El7egga" };
        static readonly string ClearReturnToBegining = new StringBuilder().Append('\r').Append(' ', Console.BufferWidth).Append('\r').ToString();

        static async Task Main()
        {
            string settingsPath = "settings.json";
            DateTime dt = DateTime.Now;

            var settings = JsonToSettings(settingsPath);
            Settings.ToJsonFile(settingsPath, settings);

            
            //var iqamaList = settings.Iqama;
            //int remindMeBefore = settings.RemindMeBefore;

            ResponseRoot? r = await TryGetRootAsync(settings.Lat, settings.Lng, settings.Method);

            if (r == null || r?.code != 200)
            {
                ErrorHappened("Something Went Wrong");
                return;
            }

            int currentDay;
            var ishaTime = TimeOnly.Parse(r.data[dt.Day - 1].timings.Isha[..5]);
            DateTime ishaDateTime = new(dt.Year, dt.Month, dt.Day, ishaTime.Hour, ishaTime.Minute, ishaTime.Second);

            //Last iqama in day ocurred?
            if ((DateTime.Now - ishaDateTime) > TimeSpan.FromMinutes(settings.Iqama.Find(x => x.Name == Prayer.salats[Prayer.salats.GetUpperBound(0)]).Value - settings.RemindMeBefore))
            {
                currentDay = dt.Day + 1;
            }
            else
            {
                currentDay = dt.Day;
            }

            Datum prayerData = r.data[currentDay - 1];


            Prayer[] prayers = Prayer.GetPrayers(prayerData, Settings.ListToDict(settings.Iqama));
            PrintMetaData(prayerData, settings);

            Prayer.PrintTimes(prayers);

            
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
                        ConsoleWriteColor(ClearReturnToBegining);
                        message = $"Remaining time for {iqamaList[i].Name} is {t:hh\\:mm\\:ss}";
                        ConsoleWriteColor(message + new string(' ', Console.BufferWidth - message.Length - 1));
                        break;
                    }
                }
            });
            timer.Interval = 1000;
            timer.Start();
        }

        private static void PrintMetaData(Datum prayerData, Settings settings)
        {
            ConsoleWriteLineColor($"{prayerData.date.gregorian.weekday.en} {prayerData.date.hijri.day} {HIJRRI_MONTHS[prayerData.date.hijri.month.number]} {prayerData.date.hijri.year} \\\\ {prayerData.date.readable}");
            ConsoleWriteLineColor();
            ConsoleWriteLineColor($"Lat: {prayerData.meta.latitude}, Lng: {prayerData.meta.longitude}, Method: {prayerData.meta.method.name}, Remind Me {settings.RemindMeBefore} Minutes Before Iqama");
            ConsoleWriteLineColor();
            foreach (var item in settings.Iqama)
            {
                ConsoleWriteColor($"{item.Name}: {item.Value} minutes, ");
            }
            ConsoleWriteLineColor($"{Environment.NewLine}");
        }

        static Settings JsonToSettings(string path)
        {
            Settings settings = new();
            try
            {
                settings = Settings.FromJson(path);
            }
            catch (Exception e)
            {
                ConsoleWriteLineColor("Wrong Json Format");
                ConsoleWriteLineColor(e.Message);
            }

            string lat = settings.Lat;
            string lng = settings.Lng;
            int method = settings.Method;
            List<Settings.IqamaObject> iqama = settings.Iqama;
            int remindMeBefore = settings.RemindMeBefore;

            if (lat == string.Empty || lng == string.Empty || method == 0 || iqama.Count != Prayer.salats.Length)
            {
                settings = GetSettingsFromUser();

            }
            return settings;
        }
        static Settings GetSettingsFromUser()
        {
            Settings settings = new();

            ConsoleWriteLineColor("No settings present");
        lat: ConsoleWriteColor("Enter City Latitude: ");
            string lat = Console.ReadLine();
            if (!double.TryParse(lat, out double dd))
            {
                goto lat;
            }
            settings.Lat = lat;

        lng: ConsoleWriteColor("Enter City Longtuide: ");
            string lng = Console.ReadLine();
            if (!double.TryParse(lat, out dd))
            {
                goto lng;
            }
            settings.Lng = lng;

        method: ConsoleWriteLineColor("Methods: ");
            Array.ForEach(Enum.GetNames(typeof(Settings.Methods)), x => ConsoleWriteLineColor($"\t\t{(int)Enum.Parse(typeof(Settings.Methods), x)} - {x}"));
            ConsoleWriteColor("Enter method number: ");
            if (!int.TryParse(Console.ReadLine(), out int method))
            {
                goto method;
            }
            settings.Method = method;

            ConsoleWriteLineColor("Enter Iqama for each salat in Minutes");
            var iqama = new List<Settings.IqamaObject>();
            for (int i = 0; i < Prayer.salats.Length; i++)
            {
            Iqamas: ConsoleWriteColor($"\t\t{Prayer.salats[i]} : ");
                if (!int.TryParse(Console.ReadLine(), out int minutes))
                {
                    goto Iqamas;
                }
                iqama.Add(new Settings.IqamaObject() { Name = Prayer.salats[i], Value = minutes });
            }
            settings.Iqama = iqama;

        remind: ConsoleWriteColor("Alarm me before Iqama by (minutes): ");
            if (!int.TryParse(Console.ReadLine(), out int remindMeBefore))
            {
                goto remind;
            }
            settings.RemindMeBefore = remindMeBefore;

            Console.Clear();

            return settings;
        }
        static async Task<ResponseRoot?> TryGetRootAsync(string lat, string lng, int method)
        {
            var dt = DateTime.Now;
            string request = @$"http://api.aladhan.com/v1/calendar?latitude={lat}&longitude={lng}&method={method}&month={dt.Month}&year={dt.Year}";
            ResponseRoot? r = null;
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
            return alarms;
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
        public static void ConsoleWriteLineColor(string message = "")
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