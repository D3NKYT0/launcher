using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Updater.UtillsClasses
{
    /*
	[ValueConversion(typeof(bool), typeof(bool))]
	public class InverseBooleanConverter : IValueConverter
	{
		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType != typeof(bool))
			{
				throw new InvalidOperationException("The target must be a boolean");
			}
			return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
		#endregion
	}*/

    /*
    [ValueConversion(typeof(bool?), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool?))
            {
                throw new InvalidOperationException("The target must be a nullable boolean");
            }
            bool? b = (bool?)value;
            return b.HasValue && !b.Value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(value as bool?);
        }

        #endregion
    }*/

    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members


        public object Convert(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a boolean");

            if (!(bool)value)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
