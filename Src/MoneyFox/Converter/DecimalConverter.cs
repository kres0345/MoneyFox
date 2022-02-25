﻿using MoneyFox.Core._Pending_;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace MoneyFox.Converter
{
    public class DecimalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is decimal decimalValue
                ? decimalValue.ToString(CultureHelper.CurrentCulture)
                : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(decimal.TryParse(value as string, NumberStyles.Currency, CultureHelper.CurrentCulture, out decimal dec))
            {
                return dec;
            }

            return value;
        }
    }
}