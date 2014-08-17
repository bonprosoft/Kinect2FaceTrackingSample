using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using Microsoft.Kinect.Face;

namespace HDFace3dTracking
{
    [ValueConversion(typeof(FaceShapeAnimationConverter), typeof(string))]
    public class FaceShapeAnimationConverter : IValueConverter
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
