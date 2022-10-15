using Timer = System.Timers.Timer;
using System.Timers;

namespace PrayerNotify
{
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