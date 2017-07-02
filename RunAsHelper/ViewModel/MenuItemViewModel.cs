using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RunAsHelper.ViewModel
{
    public class MenuItemViewModel
    {
        private Icon _icon;

        public MenuItemViewModel()
        {
            MenuItems = new List<MenuItemViewModel>();
        }

        public string DisplayName { get; set; }
        public string Path { get; set; }
        public ICommand MenuItemCommand { get; set; }
        public List<MenuItemViewModel> MenuItems { get; set; }

        public ImageSource IconImage
        {
            get
            {
                if (_icon == null)
                {
                    if (Path != null) _icon = Icon.ExtractAssociatedIcon(Path);

                    if (_icon == null) return null;
                }

                BitmapSource bitmapSource;

                var bitmap = _icon.ToBitmap().GetHbitmap();
                try
                {
                    bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                finally
                {
                    DeleteObject(bitmap);
                }

                return bitmapSource;
            }
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int DeleteObject(IntPtr o);
    }
}