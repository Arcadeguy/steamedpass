using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Steamedpass.App;

/// <summary>
/// Converts a nullable file path to an <see cref="ImageSource"/> for the games grid's icon
/// column. The default <see cref="System.Windows.Media.ImageSourceConverter"/> WPF would
/// otherwise use throws <see cref="NotSupportedException"/> on a null input (games with no
/// resolved thumbnail), which floods the output window with binding errors.
/// </summary>
public sealed class IconPathToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            return new BitmapImage(new Uri(path, UriKind.Absolute));
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
