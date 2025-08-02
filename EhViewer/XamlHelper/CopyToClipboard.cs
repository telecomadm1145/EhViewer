using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;

namespace EhViewer
{
    internal class CopyToClipboard : MarkupExtension, ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return !string.IsNullOrWhiteSpace(parameter?.ToString());
        }

        public void Execute(object parameter)
        {
            var s = parameter?.ToString() ?? "";
            DataPackage dp = new();
            dp.SetText(s);
            Clipboard.SetContent(dp);
            ContentDialog messageDialog = new ContentDialog
            {
                Title = "EhViewer",
                Content = $"已复制到剪贴板。",
                CloseButtonText = "关闭"
            };

            _ = messageDialog.ShowAsync();
        }

        protected override object ProvideValue()
        {
            return this;
        }
    }
}
