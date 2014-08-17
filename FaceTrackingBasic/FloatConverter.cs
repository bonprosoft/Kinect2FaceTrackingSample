using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FaceTrackingBasic
{
    [ValueConversion(typeof(float), typeof(string))]
    public class FloatConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            // float -> string
            float v = (float)value;
            return Math.Round(v, 2).ToString();
        }

        public object ConvertBack(object value, System.Type targetType,
          object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
