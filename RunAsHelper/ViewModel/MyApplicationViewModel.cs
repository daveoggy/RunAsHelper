using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Newtonsoft.Json;
using RunAsHelper.Views;

namespace RunAsHelper.ViewModel
{
    public class MyApplicationViewModel : ObservableObject
    {
        private string _applicationName;
        private Icon _icon;
        private string _path;

        public MyApplicationViewModel()
        {
            Browse = new RelayCommand(AddApplicationCommand);
        }

        private void AddApplicationCommand()
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Application (*.exe, *.bat, *.cmd)|*.exe;*.bat;*.cmd"
            };

            if (fileDialog.ShowDialog(OptionsView.CurrentInstance) == true)
            {
                var fileInfo = new FileInfo(fileDialog.FileName);

                var fileVersionInfo = FileVersionInfo.GetVersionInfo(fileInfo.FullName);

                ApplicationName = string.IsNullOrEmpty(fileVersionInfo.FileDescription)
                    ? fileInfo.Name
                    : fileVersionInfo.FileDescription;

                Path = fileInfo.FullName;
            }
        }

        public string Path
        {
            get => _path;
            set { _path = value; RaisePropertyChanged(nameof(Path)); }
        }

        public string ApplicationName
        {
            get => _applicationName;
            set { _applicationName = value; RaisePropertyChanged(nameof(ApplicationName)); }
        }

        [JsonIgnore]
        [XmlIgnore]
        [SoapIgnore]
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

        [JsonIgnore]
        [XmlIgnore]
        [SoapIgnore]
        public RelayCommand Browse { get; set; }

        public bool RunAsLocalAdmin { get; set; }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int DeleteObject(IntPtr o);
    }
}