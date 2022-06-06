using System;
using System.Globalization;

namespace atsenterapp
{
    /// <summary>
    /// Represents a hebrew date
    /// </summary>
    public class HebrewDateTime
    {
        private int year, month, day;
        private static CultureInfo culture = null;
        private static HebrewCalendar hc = GetCalendar();

        public HebrewDateTime(int year, int month, int day)
        {
            this.year = year;
            this.month = month;
            this.day = day;
        }

        private static HebrewCalendar GetCalendar()
        {
            HebrewCalendar calendar = new HebrewCalendar();
            culture = CultureInfo.CreateSpecificCulture("he-IL");
            culture.DateTimeFormat.Calendar = calendar;
            return calendar;
        }

        public static HebrewDateTime Now()
        {
            DateTime now = DateTime.Now;
            return new HebrewDateTime(hc.GetYear(now), hc.GetMonth(now), hc.GetDayOfMonth(now));
        }

        public static HebrewDateTime FromGregorianDate(DateTime date)
        {
            return new HebrewDateTime(hc.GetYear(date), hc.GetMonth(date), hc.GetDayOfMonth(date));
        }

        public static HebrewDateTime FromGregorianDate(int year, int month, int day)
        {
            DateTime date = new DateTime(year, month, day);
            return new HebrewDateTime(hc.GetYear(date), hc.GetMonth(date), hc.GetDayOfMonth(date));
        }

        public int Year
        {
            get { return year; }
            set { year = value; }
        }
        public int Month
        {
            get { return month; }
            set { month = value; }
        }
        public int Day
        {
            get { return day; }
            set { day = value; }
        }

        public override string ToString()
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            return hc.ToDateTime(year, month, day, now.Hours, now.Minutes, now.Seconds, now.Milliseconds).ToString("d בMMM, yyyy", culture);
        }
    }
}