using System;
using System.Collections.Generic;
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
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using RunAsHelper.Views;

namespace RunAsHelper.ViewModel
{
    public class MyApplicationViewModel : ObservableObject
    {
        private string _applicationName;
        private BitmapSource _bitmapSource;
        private string _path;

        public MyApplicationViewModel()
        {
            Browse = new RelayCommand(AddApplicationCommand);
        }

        private void AddApplicationCommand()
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Application (*.exe, *.bat, *.cmd)|*.exe;*.bat;*.cmd",
                InitialDirectory = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"),
                CustomPlaces = new List<FileDialogCustomPlace>
                {
                    new FileDialogCustomPlace(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%")),
                    new FileDialogCustomPlace(Environment.ExpandEnvironmentVariables("%ProgramFiles%"))
                }
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
        public ImageSource IconImage => _bitmapSource ?? (_bitmapSource = ExtractBitmapSource());

        private BitmapSource ExtractBitmapSource()
        {
            var icon = string.IsNullOrEmpty(Path) ? null : Icon.ExtractAssociatedIcon(Path);

            if (icon == null) return null;

            var bitmap = icon.ToBitmap().GetHbitmap();

            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(bitmap);
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