using TimeTracker_Entevisual.Models;
using TimeTracker_Entevisual.Models.ViewModels;

namespace TimeTracker_Entevisual.Helpers
{
    public static class TimeFormatHelper
    {
        public static string FormatoHHMMSS(long segundos)
        {
            if (segundos < 0) segundos = 0;

            var ts = TimeSpan.FromSeconds(segundos);
            var totalHoras = (int)ts.TotalHours;

            return $"{totalHoras:00}:{ts.Minutes:00}:{ts.Seconds:00}";
        }

        
    }

}

