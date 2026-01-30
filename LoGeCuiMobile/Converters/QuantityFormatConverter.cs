using System;
using System.Globalization;
using LoGeCuiMobile.Resources.Lang;
using Microsoft.Maui.Controls;

namespace LoGeCuiMobile.Converters
{
    public class QuantityFormatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var q = values.Length > 0 ? values[0]?.ToString() ?? "" : "";
            var u = values.Length > 1 ? values[1]?.ToString() ?? "" : "";

            var fmt = LocalizationResourceManager.Instance["Ingredients_QuantityFormat"];
            // Ex: "Quantité : {0} {1}" / "Menge: {0} {1}"
            return string.Format(fmt, q, u).Trim();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
