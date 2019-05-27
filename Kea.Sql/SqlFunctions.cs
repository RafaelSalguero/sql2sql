using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeaSql.Fluent;

namespace KeaSql
{
    /// <summary>
    /// Funciones de SQL
    /// </summary>
    public static partial class Sql
    {

        [SqlName("count")]
        public static int Count<T>(T expr) => throw new SqlFunctionException();
        [SqlName("sum")]
        public static T Sum<T>(T expr) => throw new SqlFunctionException();
        [SqlName("max")]
        public static T Max<T>(T expr) => throw new SqlFunctionException();
        [SqlName("min")]
        public static T Min<T>(T expr) => throw new SqlFunctionException();

        /// <summary>
        /// Round to nearest integer
        /// </summary>
        [SqlName("round")]
        public static T Round<T>(T value, int places = 0) => throw new SqlFunctionException();

        /// <summary>
        /// Nearest integer less than or equal to argument
        /// </summary>
        [SqlName("floor")]
        public static T Floor<T>(T value, int places = 0) => throw new SqlFunctionException();

        /// <summary>
        /// Truncate toward zero
        /// </summary>
        [SqlName("trunc")]
        public static T Trunc<T>(T value, int places = 0) => throw new SqlFunctionException();

        /// <summary>
        /// Nearest integer greater than or equal to argument
        /// </summary>
        [SqlName("ceil")]
        public static T Ceil<T>(T value, int places = 0) => throw new SqlFunctionException();

        /// <summary>
        /// Sign of the argument (-1, 0, +1)
        /// </summary>
        [SqlName("sign")]
        public static T Sign<T>(T value) => throw new SqlFunctionException();

        [SqlName("coalesce")]
        public static T Coalesce<T>(T? a, T b) where T : struct => throw new SqlFunctionException();
        [SqlName("coalesce")]
        public static T Coalesce<T>(T? a, T? b, T c) where T : struct => throw new SqlFunctionException();
        [SqlName("coalesce")]
        public static T Coalesce<T>(T a, T b) => throw new SqlFunctionException();
        [SqlName("coalesce")]
        public static T Coalesce<T>(T a, T b, T c) => throw new SqlFunctionException();

        [SqlName("greatest")]
        public static T Greatest<T>(T a, T b) => throw new SqlFunctionException();
        [SqlName("greatest")]
        public static T Greatest<T>(T a, T b, T c) => throw new SqlFunctionException();
        [SqlName("greatest")]
        public static T Greatest<T>(T a, T b, T c, T d) => throw new SqlFunctionException();

        [SqlName("least")]
        public static T Least<T>(T a, T b) => throw new SqlFunctionException();
        [SqlName("least")]
        public static T Least<T>(T a, T b, T c) => throw new SqlFunctionException();
        [SqlName("least")]
        public static T Least<T>(T a, T b, T c, T d) => throw new SqlFunctionException();

        public enum DateTruncField
        {
            [SqlName("'microseconds'")]
            Microseconds,
            [SqlName("'milliseconds'")]
            Milliseconds,
            [SqlName("'second'")]
            Second,
            [SqlName("'minute'")]
            Minute,
            [SqlName("'hour'")]
            Hour,
            [SqlName("'day'")]
            Day,
            [SqlName("'week'")]
            Week,
            [SqlName("'month'")]
            Month,
            [SqlName("'quarter'")]
            Quarter,
            [SqlName("'year'")]
            Year,
            [SqlName("'decade'")]
            Decade,
            [SqlName("'century'")]
            Century,
            [SqlName("'millennium'")]
            Millennium
        }

        [SqlName("date_trunc")]
        public static T DateTrunc<T>(DateTruncField field, T value) => throw new SqlFunctionException();

        /// <summary>
        /// Identifier or string that selects what field to extract from the source value
        /// </summary>
        public enum ExtractField
        {
            /// <summary>
            /// The first century starts at 0001-01-01 00:00:00 AD, although they did not know it at the time. This definition applies to all Gregorian calendar countries. There is no century number 0, you go from -1 century to 1 century. If you disagree with this, please write your complaint to: Pope, Cathedral Saint-Peter of Roma, Vatican.
            /// </summary>
            [SqlName("CENTURY")]
            Century,

            /// <summary>
            /// For timestamp values, the day (of the month) field (1 - 31) ; for interval values, the number of days
            /// </summary>
            [SqlName("DAY")]
            Day,

            /// <summary>
            /// The year field divided by 10
            /// </summary>
            [SqlName("DECADE")]
            Decade,

            /// <summary>
            /// DOW - The day of the week as Sunday (0) to Saturday (6)
            /// </summary>
            [SqlName("DOW")]
            DayOfWeek,


            /// <summary>
            /// DOY - The day of the year (1 - 365/366)
            /// </summary>
            [SqlName("DOY")]
            DayOfYear,

            /// <summary>
            /// For timestamp with time zone values, the number of seconds since 1970-01-01 00:00:00 UTC (can be negative); for date and timestamp values, the number of seconds since 1970-01-01 00:00:00 local time; for interval values, the total number of seconds in the interval
            /// </summary>
            [SqlName("EPOCH")]
            Epoch,

            /// <summary>
            /// The hour field (0 - 23)
            /// </summary>
            [SqlName("HOUR")]
            Hour,

            /// <summary>
            /// ISODOW - The day of the week as Monday (1) to Sunday (7)
            /// </summary>
            [SqlName("ISODOW")]
            IsoDayOfWeek,

            /// <summary>
            /// The ISO 8601 week-numbering year that the date falls in (not applicable to intervals)
            /// Each ISO 8601 week-numbering year begins with the Monday of the week containing the 4th of January, so in early January or late December the ISO year may be different from the Gregorian year. See the week field for more information.
            /// </summary>
            [SqlName("ISOYEAR")]
            IsoYear,

            /// <summary>
            /// The seconds field, including fractional parts, multiplied by 1 000 000; note that this includes full seconds
            /// </summary>
            [SqlName("MICROSECONDS")]
            Microseconds,

            /// <summary>
            /// The millennium.
            /// Years in the 1900s are in the second millennium. The third millennium started January 1, 2001.
            /// </summary>
            [SqlName("MILLENNIUM")]
            Millennium,

            /// <summary>
            /// The seconds field, including fractional parts, multiplied by 1000. Note that this includes full seconds.
            /// </summary>
            [SqlName("MILLISECONDS")]
            Milliseconds,

            /// <summary>
            /// The minutes field (0 - 59)
            /// </summary>
            [SqlName("MINUTE")]
            Minute,

            /// <summary>
            /// For timestamp values, the number of the month within the year (1 - 12) ; for interval values, the number of months, modulo 12 (0 - 11)
            /// </summary>
            [SqlName("MONTH")]
            Month,

            /// <summary>
            /// The quarter of the year (1 - 4) that the date is in
            /// </summary>
            [SqlName("QUARTER")]
            Quarter,

            /// <summary>
            /// The seconds field, including fractional parts (0 - 59)
            /// </summary>
            [SqlName("SECOND")]
            Second,

            /// <summary>
            /// The time zone offset from UTC, measured in seconds. Positive values correspond to time zones east of UTC, negative values to zones west of UTC. (Technically, PostgreSQL does not use UTC because leap seconds are not handled.)
            /// </summary>
            [SqlName("TIMEZONE")]
            Timezone,

            /// <summary>
            /// The hour component of the time zone offset
            /// </summary>
            [SqlName("TIMEZONE_HOUR")]
            TimezoneHour,

            /// <summary>
            /// The minute component of the time zone offset
            /// </summary>
            [SqlName("TIMEZONE_MINUTE")]
            TimezoneMinute,

            /// <summary>
            /// The number of the ISO 8601 week-numbering week of the year. By definition, ISO weeks start on Mondays and the first week of a year contains January 4 of that year. In other words, the first Thursday of a year is in week 1 of that year.
            /// In the ISO week-numbering system, it is possible for early-January dates to be part of the 52nd or 53rd week of the previous year, and for late-December dates to be part of the first week of the next year. For example, 2005-01-01 is part of the 53rd week of year 2004, and 2006-01-01 is part of the 52nd week of year 2005, while 2012-12-31 is part of the first week of 2013. It's recommended to use the isoyear field together with week to get consistent results.
            /// </summary>
            [SqlName("WEEK")]
            Week,

            /// <summary>
            /// The year field. Keep in mind there is no 0 AD, so subtracting BC years from AD years should be done with care.
            /// </summary>
            [SqlName("YEAR")]
            Year,
        }

        /// <summary>
        /// The extract function retrieves subfields such as year or hour from date/time values. source must be a value expression of type timestamp, time, or interval. (Expressions of type date are cast to timestamp and can therefore be used as well.)
        /// </summary>
        /// <param name="field">Identifier or string that selects what field to extract from the source value</param>
        public static double Extract<T>(ExtractField field, T source) => throw new SqlFunctionException();
    }
}
