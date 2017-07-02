using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;

namespace RunAsHelper.ViewModel
{
    public class MyApplicationViewModel : ObservableObject
    {
        private string _applicationName;
        private Icon _icon;
        public string Path { get; set; }

        public string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = value; RaisePropertyChanged(() => ApplicationName); }
        }

        //public ProfileViewModel DefaultProfile { get; set; }

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