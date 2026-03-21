using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Katastata.Helpers
{
    public class PathToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                try
                {
                    using (var icon = Icon.ExtractAssociatedIcon(path))
                    {
                        if (icon != null)
                        {
                            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                icon.Handle,
                                System.Windows.Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логируем, если нужно
                    System.Diagnostics.Debug.WriteLine($"Icon extraction failed: {ex.Message}");
                }
            }
            // Запасная иконка
            return new BitmapImage(new Uri("pack://application:,,,/Assets/Icons/default_app.png"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}