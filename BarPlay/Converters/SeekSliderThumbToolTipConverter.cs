using Microsoft.UI.Xaml.Data;

namespace BarPlay.Converters;

public sealed partial class SeekSliderThumbToolTipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not double ticks) return "0:00";
        if (ticks <= 0) return "0:00";
        var span = TimeSpan.FromTicks((long)ticks);
        return span.TotalHours >= 1 ? $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}" : $"{(int)span.TotalMinutes}:{span.Seconds:D2}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value;
}
