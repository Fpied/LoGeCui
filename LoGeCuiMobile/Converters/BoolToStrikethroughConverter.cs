using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace LoGeCuiMobile.Converters
{
    public class BoolToStrikethroughConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? TextDecorations.Strikethrough : TextDecorations.None;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
