using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrayerNotify
{
    public class Settings
    {
        string lat;
        string lng;
        int method;
        int remindMeBefore;

        public string Lat { get => lat; set => lat = value; }
        public string Lng { get => lng; set => lng = value; }
        int[] methods = (int[])Enum.GetValues(typeof(Methods));
        public int Method { get => method; set => method = methods.Contains(value) ? value : 5; }
        public int RemindMeBefore { get => remindMeBefore; set => remindMeBefore = value; }

        public enum Methods
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
    
    
}

