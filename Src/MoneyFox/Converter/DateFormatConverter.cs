﻿namespace MoneyFox.Converter
{
    using Core._Pending_;
    using System;
    using System.Globalization;
    using Core.Common;
    using Xamarin.Forms;

    /// <summary>
    ///     Formats the date with the culture in the CultureHelper.
    /// </summary>
    public class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => ((DateTime)value).ToString("d", CultureHelper.CurrentCulture);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}