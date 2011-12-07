using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace EncryptFolders.WPF
{
    public class TextBlockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var textBlock = new TextBlock();
            
            if (parameter is string)
            {
                textBlock.Inlines.Add(new Bold(new Run(string.Format("{0}: ", parameter))));
            }

            if (value is int)
            {
                textBlock.Inlines.Add(new Run(((int)value).ToString("#,0")));
            }
            else if (value is string)
            {
                textBlock.Inlines.Add(new Run((string)value));
            }

            return textBlock;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}