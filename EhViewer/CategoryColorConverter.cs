using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media.Imaging;

namespace EhViewer
{
    internal class CategoryColorConverter : MarkupExtension, IValueConverter
    {
        protected override object ProvideValue()
        {
            return this;
        }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string s)
            {
                switch (s.ToLower())
                {
                    case "doujinshi":
                        return Color.FromArgb(255, 0x9E, 0x27, 0x20);
                    case "manga":
                        return Color.FromArgb(255, 0xDB, 0x6C, 0x24);
                    case "artist cg":
                        return Color.FromArgb(255, 0xD3, 0x8F, 0x1D);
                    case "game cg":
                        return Color.FromArgb(255, 0x61, 0x7C, 0x63);
                    case "image set":
                        return Color.FromArgb(255, 0x32, 0x5c, 0xa2);
                    case "cosplay":
                        return Color.FromArgb(255, 0xA2, 0x32, 0x82);
                    case "non-h":
                        return Color.FromArgb(255, 0x5F, 0xA9, 0xCF);
                    case "western":
                        return Color.FromArgb(255, 0XAB, 0x9F, 0x60);
                }
            }
            return Color.FromArgb(255, 0x77, 0x77, 0x77);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
