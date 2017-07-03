using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CreateProcessSample;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Newtonsoft.Json;
using RunAsHelper.Properties;
using RunAsHelper.Views;

namespace RunAsHelper.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private static readonly String[] ExistingApplications = new string[]
            {
                Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe",
                Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe",
                Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft Visual Studio 9.0\Common7\IDE\devenv.exe",
                Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft Visual Studio 8.0\Common7\IDE\devenv.exe",
                Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft SQL Server\100\Tools\Binn\VSShell\Common7\IDE\ssms.exe",
                Environment.ExpandEnvironmentVariables("%WINDIR%") + @"\System32\cmd.exe",
                Environment.ExpandEnvironmentVariables("%SystemRoot%") + @"\system32\WindowsPowerShell\v1.0\powershell.exe",
            };

        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                CreateDesignTimeData();
            }
            else
            {
                Profiles = Settings.Default.Profiles;
                Applications = Settings.Default.Applications;

                if (Profiles == null)
                {
                    Profiles = new ObservableCollection<ProfileViewModel>();
                }

                if (Applications == null)
                {
                    Applications = new ObservableCollection<MyApplicationViewModel>();    
                }

                if (Settings.Default.FirstRun)
                {
                    LookForExistingApplications();
                }
            }

            AddProfile = new RelayCommand(AddProfileCommand);
            RemoveProfile = new RelayCommand(() => Profiles.Remove(SelectedProfile), () => SelectedProfile != null);
            EditProfile = new RelayCommand(() => EditProfileCommand(), () => SelectedProfile != null);

            AddApplication = new RelayCommand(AddApplicationCommand);
            RemoveApplication = new RelayCommand(() => Applications.Remove(SelectedApplicationViewModel), () => SelectedApplicationViewModel != null);

            MenuItems = new ObservableCollection<MenuItemViewModel>();

            BuildMenuItems();

            Profiles.CollectionChanged += CollectionChanged;
            Applications.CollectionChanged += CollectionChanged;
        }

        private void LookForExistingApplications()
        {
            var currentApp = Applications.Select(a => a.Path.ToUpper()).ToArray();

            foreach (var existingApplication in ExistingApplications)
            {
                var tempFileInfo = new FileInfo(existingApplication);
                if (!currentApp.Contains(tempFileInfo.FullName.ToUpper()) && tempFileInfo.Exists)
                {
                    ProcessFilenameAndAddApplication(tempFileInfo.FullName);
                }
            }


            Settings.Default.Applications = Applications;
            Settings.Default.FirstRun = false;
            Settings.Default.Save();
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            BuildMenuItems();
        }

        private void BuildMenuItems()
        {
            MenuItems.Clear();

            MenuItems.Add(new MenuItemViewModel
            {
                MenuItemCommand = new RelayCommand(ShowOptions),
                DisplayName = "Options"
            });
            
            MenuItems.Add(null);

            foreach (var myApplicationViewModel in Applications)
            {
                var model = myApplicationViewModel;

                var mip = new MenuItemViewModel
                {
                    DisplayName = model.ApplicationName,
                    Path = model.Path
                };

                foreach (var profileViewModel in Profiles)
                {
                    var viewModel = profileViewModel;
                    mip.MenuItems.Add(new ApplicationMenuItemViewModel
                    {
                        DisplayName = "as " + profileViewModel.ProfileName,
                        ApplicationViewModel = myApplicationViewModel,
                        ProfileViewModel = viewModel,
                        MenuItemCommand = new RelayCommand<ApplicationMenuItemViewModel>(StartApplication),
                        Path = model.Path
                    });
                }

                MenuItems.Add(mip);
           }

            MenuItems.Add(null);

            MenuItems.Add(new MenuItemViewModel
            {
                MenuItemCommand = new RelayCommand(Exit),
                DisplayName = "Exit"
            });
        }



        private void StartApplication(ApplicationMenuItemViewModel applicationMenuItemViewModel)
        {
            var password = EncryptionHelper.Decrypt(applicationMenuItemViewModel.ProfileViewModel.EncryptedPassword);
            var username = applicationMenuItemViewModel.ProfileViewModel.GetUsernameWithoutDomain();
            var domain = applicationMenuItemViewModel.ProfileViewModel.Domain;


            try
            {
                Win32.CreateProcessWithLogon(applicationMenuItemViewModel.ApplicationViewModel.Path, domain, username, password);
            }
            catch (Exception ex)
            {
                MessageBox.Show("LaunchCommand error: " + ex.Message);
            }
        }

        private void Exit()
        {
            Application.Current.MainWindow.Close();
        }

        private void ShowOptions()
        {
            var options = new OptionsView();

            options.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            options.Show();

            EventHandler x = null;

            x = delegate
            {
                BuildMenuItems();
                options.Closed -= x;
            };

            options.Closed += x;
        }

        private void AddApplicationCommand()
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Application (*.exe, *.bat, *.cmd)|*.exe;*.bat;*.cmd"
            };

            //fileDialog.Owner = OptionsView.CurrentInstance;
            //fileDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (fileDialog.ShowDialog(OptionsView.CurrentInstance) == true)
            {
                ProcessFilenameAndAddApplication(fileDialog.FileName);
            }
        }

        private void ProcessFilenameAndAddApplication(String filename)
        {
            var fileInfo = new FileInfo(filename);

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(fileInfo.FullName);

            

            var applicationViewModel = new MyApplicationViewModel
                {
                    ApplicationName =
                        String.IsNullOrEmpty(fileVersionInfo.FileDescription) ? fileInfo.Name : fileVersionInfo.FileDescription,
                    Path = fileInfo.FullName,
                };

            Applications.Add(applicationViewModel);
        }

        private void CreateDesignTimeData()
        {
            Profiles = new ObservableCollection<ProfileViewModel>()
            {
                new ProfileViewModel {Domain = "MyDomain1", Username = "MyUsername1"},
                new ProfileViewModel {Domain = "MyDomain1", Username = "MyUsername1"},
                new ProfileViewModel {Domain = "MyDomain1", Username = "MyUsername1"},
                new ProfileViewModel {Domain = "MyDomain1", Username = "MyUsername1"}
            };

            Applications = new ObservableCollection<MyApplicationViewModel>()
            {
                new MyApplicationViewModel
                {
                    ApplicationName = "Application One",
                    Path = new FileInfo("SomeFile.exe").FullName,
                },
                new MyApplicationViewModel
                {
                    ApplicationName = "Application One",
                    Path = new FileInfo("SomeFile.exe").FullName,
                },
                new MyApplicationViewModel
                {
                    ApplicationName = "Application One",
                    Path = new FileInfo("SomeFile.exe").FullName,
                },
                new MyApplicationViewModel
                {
                    ApplicationName = "Application One",
                    Path = new FileInfo("SomeFile.exe").FullName,
                },
            };
        }

        private void AddProfileCommand()
        {
            var profileViewModel = new ProfileViewModel();
            var dialog = new Views.CredentialsDialog(profileViewModel);
            dialog.Owner = OptionsView.CurrentInstance;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                Profiles.Add(profileViewModel);
            }
        }
        private void EditProfileCommand()
        {
            var tempProfile = JsonConvert.DeserializeObject<ProfileViewModel>(JsonConvert.SerializeObject(SelectedProfile));

            var dialog = new Views.CredentialsDialog(tempProfile);
            dialog.Owner = OptionsView.CurrentInstance;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                Profiles.Remove(SelectedProfile);
                Profiles.Add(tempProfile);
            }
        }

        public ObservableCollection<ProfileViewModel> Profiles { get; set; }
 
        public ObservableCollection<MyApplicationViewModel> Applications { get; set; }

        public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }

        private ProfileViewModel _selectedProfile;
        public ProfileViewModel SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                RaisePropertyChanged(nameof(SelectedProfile));
            }
        }

        public RelayCommand AddProfile { get; set; }
        public RelayCommand RemoveProfile { get; set; }
        public RelayCommand EditProfile { get; set; }
        public RelayCommand AddApplication { get; set; }
        public RelayCommand RemoveApplication { get; set; }

        public MyApplicationViewModel SelectedApplicationViewModel { get; set; }

        public void SaveChanges()
        {
            Settings.Default.Profiles = Profiles;
            Settings.Default.Applications = Applications;
            Settings.Default.Save();
        }
    }
}
