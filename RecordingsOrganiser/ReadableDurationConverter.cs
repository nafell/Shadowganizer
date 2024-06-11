using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RecordingsOrganiser
{
    class ReadableDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var timeSpan = (TimeSpan)value;
            var durationReadable = timeSpan.Minutes.ToString("00':'") + timeSpan.Seconds.ToString("00");
            if (timeSpan.Hours > 0)
            {
                durationReadable = timeSpan.Hours.ToString("00':'") + durationReadable;
            }
            return durationReadable;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
