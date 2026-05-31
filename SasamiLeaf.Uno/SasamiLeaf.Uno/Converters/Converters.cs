using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace SasamiLeaf.Uno.Converters
{
    public class TicksToTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not long ticks)
                return "00:00:00";

            var ts = TimeSpan.FromTicks(ticks);

            if (ts.TotalHours > 9)
                return ((int)ts.TotalHours) + ts.ToString(@"\:mm\:ss");
            else
                return ts.ToString(@"hh\:mm\:ss");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class QualityToLevelsConverter : IValueConverter
    {
        public enum Qualities
        {
            None,
            Low,
            Med,
            High,
            _4k
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not int videoHeight)
                return Qualities.None;

            if (videoHeight > 1080)
                return Qualities._4k;
            else if (videoHeight > 720)
                return Qualities.High;
            else if (videoHeight == 720)
                return Qualities.Med;
            else
                return Qualities.Low;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class VolumeToLevelsConverter : IValueConverter
    {
        public enum Volumes
        {
            Mute,
            Low,
            Med,
            High
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not int volume)
                return Volumes.Mute;

            if (volume == 0)
                return Volumes.Mute;
            else if (volume > 99)
                return Volumes.High;
            else if (volume > 49)
                return Volumes.Med;
            else
                return Volumes.Low;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool val && !val;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is bool val && !val;
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool val = value is bool b && b;
            if (parameter is string param && param == "inverse")
                val = !val;
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isNull = value == null;
            if (parameter is string param && param == "inverse")
                isNull = !isNull;
            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class PlayerStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is FlyleafLib.MediaPlayer.Status status)
            {
                bool hide = status == FlyleafLib.MediaPlayer.Status.Playing || status == FlyleafLib.MediaPlayer.Status.Paused;
                return hide ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class MarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => new Thickness(0, System.Convert.ToDouble(value), 0, 0);

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => null;
    }

    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Color color)
                return new SolidColorBrush(color);

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is SolidColorBrush brush)
                return brush.Color;

            return default(Color);
        }
    }

    public class BrushToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not SolidColorBrush brush)
                return null;

            string lowerHexString(int i) => i.ToString("X2").ToLower();
            var hex = lowerHexString(brush.Color.R) +
                      lowerHexString(brush.Color.G) +
                      lowerHexString(brush.Color.B);

            return "#" + hex;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class ColorToHexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is null)
                return null;

            string lowerHexString(int i) => i.ToString("X2").ToLower();
            Color color = (Color)value;
            var hex = lowerHexString(color.R) +
                      lowerHexString(color.G) +
                      lowerHexString(color.B);

            return hex;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return DependencyProperty.UnsetValue;
            try
            {
                var hex = value.ToString().Replace("#", "");
                byte a = 255;
                byte r = 0;
                byte g = 0;
                byte b = 0;
                int start = 0;
                if (hex.Length == 8)
                {
                    a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    start = 2;
                }
                r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);
                return Color.FromArgb(a, r, g, b);
            }
            catch (Exception) { }

            return DependencyProperty.UnsetValue;
        }
    }

    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string format)
            {
                return string.Format(format, value);
            }
            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    public class TicksToTimeWithMsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not long ticks)
                return "00:00:00.000";

            var ts = TimeSpan.FromTicks(ticks);
            return ts.ToString(@"hh\:mm\:ss\.fff");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
