using Microsoft.Bot.Builder.Luis;
using mStack.API.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mStack.API.Bots.Utilities
{
    public static class BotExtensions
    {
        public static DateTime ConvertResolutionToDateTime(this BuiltIn.DateTime.DateTimeResolution actual)
        {
            // when entering "feb 1st", the parser will return -1 for the year... we will assume: this year
            int year = actual.Year != null && actual.Year.Value != -1 ? actual.Year.Value : DateTime.Now.Year;
            int month = actual.Month != null && actual.Month.Value != -1 ? actual.Month.Value : DateTime.Now.Month;
            int day = actual.Day != null && actual.Day.Value != -1 ? actual.Day.Value : DateTime.Now.Day;

            // when the day was null but the DayOfWeek has been set; use that instead
            if (actual.Day == null && actual.DayOfWeek.HasValue)
            {
                int dayOfWeekSpecified = DateTimeUtils.GetISODayOfweek(actual.DayOfWeek.Value);
                int dayOfWeekNow = DateTimeUtils.GetISODayOfweek(DateTime.Now.DayOfWeek);

                // calculate the difference between the current day of the week and the one specified
                int dayDiff = dayOfWeekNow - dayOfWeekSpecified;
                day = DateTime.Now.Day - dayDiff;
            }

            DateTime result = new DateTime(year, month, day);
            return result;
        }
    }
}
