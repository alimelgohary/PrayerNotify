using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrayerNotify
{
    public class Settings
    {
        
        public string Lat { get => lat; set => lat = value; }
        public string Lng { get => lng; set => lng = value; }
        public int Method { get => method; set => method = METHODS.Contains(value) ? value : 5; }
        public int RemindMeBefore { get => remindMeBefore; set => remindMeBefore = value < 0 ? 0 : value; }
        public List<IqamaObject> Iqama { get => iqama; set => iqama = value; }

        public enum Methods
        {
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

        public class IqamaObject
        {
            string[] salats = Prayer.SALATS;
            string name;
            int val;
            public string Name
            {
                get => name;
                set
                {
                    if (!salats.Contains(value))
                    {
                        throw new FormatException("Json Format incorrect");
                    }
                    else
                    {
                        name = value;
                    }
                }
            }
            public int Value { get => val; set => val = (value < 0) ? 0 : value; }
            public override string ToString()
            {
                return $"{Name}: {Value} minutes";
            }
        }

        public static Dictionary<string, int> ListToDict(List<IqamaObject> list)
        {
            var dict = new Dictionary<string, int>();
            foreach (var item in list)
            {
                dict.Add(item.Name, item.Value);
            }
            return dict;
        }
        
        public static Settings JsonToSettings(string path)
        {
            Settings settings = new();
            try
            {
                settings = FromJson(path);
            }
            catch (Exception e)
            {
                Printer.ConsoleWriteLineColor("Wrong Json Format");
                Printer.ConsoleWriteLineColor(e.Message);
            }

            string lat = settings.Lat;
            string lng = settings.Lng;
            int method = settings.Method;
            List<IqamaObject> iqama = settings.Iqama;

            if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lng) || !METHODS.Contains(method) || iqama == null || iqama.Count != Prayer.SALATS.Length)
            {
                settings = GetSettingsFromUser();
                ToJsonFile(path, settings);
            }
            return settings;
        }
        public static Settings GetSettingsFromUser()
        {
            Settings settings = new();

            Printer.ConsoleWriteLineColor("No settings present");
        lat: Printer.ConsoleWriteColor("Enter City Latitude: ");
            string lat = Console.ReadLine();
            if (!double.TryParse(lat, out double dd))
            {
                goto lat;
            }
            settings.lat = lat;

        lng: Printer.ConsoleWriteColor("Enter City Longtuide: ");
            string lng = Console.ReadLine();
            if (!double.TryParse(lat, out dd))
            {
                goto lng;
            }
            settings.lng = lng;

        method: Printer.ConsoleWriteLineColor("Methods: ");
            Array.ForEach(Enum.GetNames(typeof(Settings.Methods)), x => Printer.ConsoleWriteLineColor($"\t\t{(int)Enum.Parse(typeof(Settings.Methods), x)} - {x}"));
            Printer.ConsoleWriteColor("Enter method number: ");
            if (!int.TryParse(Console.ReadLine(), out int method))
            {
                goto method;
            }
            settings.Method = method;

            Printer.ConsoleWriteLineColor("Enter Iqama for each salat in Minutes");
            var iqama = new List<Settings.IqamaObject>();
            for (int i = 0; i < Prayer.SALATS.Length; i++)
            {
            Iqamas: Printer.ConsoleWriteColor($"\t\t{Prayer.SALATS[i]} : ");
                if (!int.TryParse(Console.ReadLine(), out int minutes))
                {
                    goto Iqamas;
                }
                iqama.Add(new Settings.IqamaObject() { Name = Prayer.SALATS[i], Value = minutes });
            }
            settings.Iqama = iqama;

        remind: Printer.ConsoleWriteColor("Alarm me before Iqama by (minutes): ");
            if (!int.TryParse(Console.ReadLine(), out int remindMeBefore))
            {
                goto remind;
            }
            settings.RemindMeBefore = remindMeBefore;

            Console.Clear();

            return settings;
        }
        public static void ToJsonFile(string path, Settings s)
        {
            File.WriteAllText(path, JsonSerializer.Serialize(s));
        }
        public static Settings FromJson(string path)
        {
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path));
        }
        string lat;
        string lng;
        int method;
        int remindMeBefore;
        List<IqamaObject> iqama;
        static int[] METHODS = (int[])Enum.GetValues(typeof(Methods));
    }


}

