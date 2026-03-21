using System;
using System.Globalization;
using System.Windows.Data;

namespace Katastata.Helpers
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                if (timeSpan.TotalHours >= 1)
                    return $"{(int)timeSpan.TotalHours} ч {timeSpan.Minutes} мин";
                else if (timeSpan.TotalMinutes >= 1)
                    return $"{timeSpan.Minutes} мин {timeSpan.Seconds} сек";
                else
                    return $"{timeSpan.Seconds} сек";
            }
            return "0 сек";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}