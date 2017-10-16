using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIDsExtractJob.Extensions
{
    public static class FormattingExtensions
    {
        public static decimal ToDecimalFromCurrency(this string currencyValue)
        {

            decimal decval;

            if (decimal.TryParse(currencyValue, NumberStyles.Currency,
              CultureInfo.GetCultureInfo("en-US"), out decval))
                return decval;

            return 0.00m;
        }

        public static decimal ToDecimal(this string decimalValue)
        {

            decimal decval;

            if (decimal.TryParse(decimalValue, out decval))
                return decval;

            return 0.00m;
        }

        public static int ToInt(this string intValue)
        {
            int intval;

            if (int.TryParse(intValue, out intval))
                return intval;

            return 0;
        }
        public static string SQLPrep(this string stringValue)
        {
            //escape single quotes
            return stringValue.Replace("'", "''");

        }

        public static string ToSQLFormat(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime? ToDateTime(this string datetimeValue)
        {
            var dateValue = new DateTime();

            //escape single quotes
            if (DateTime.TryParse(datetimeValue, out dateValue))
                return dateValue;

            return null as DateTime?;
        }

        
    }
}
