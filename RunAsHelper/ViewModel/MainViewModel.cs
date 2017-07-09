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
using System.Security.Principal;
using Microsoft.Win32.TaskScheduler;
using System.Security.Permissions;

namespace RunAsHelper.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private static readonly String[] ExistingApplications = new string[]
            {
                //Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe",
                //Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe",
                //Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft Visual Studio 9.0\Common7\IDE\devenv.exe",
                //Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft Visual Studio 8.0\Common7\IDE\devenv.exe",
                //Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") + @"\Microsoft SQL Server\100\Tools\Binn\VSShell\Common7\IDE\ssms.exe",
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

                StartWithWindowsEnabled = Settings.Default.StartWithWindows;

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
            EditApplication = new RelayCommand(() => EditApplicationCommand(), () => SelectedApplicationViewModel != null);

            StartWithWindows = new RelayCommand(StartWithWindowsCommand);

            MenuItems = new ObservableCollection<MenuItemViewModel>();

            BuildMenuItems();

            Profiles.CollectionChanged += CollectionChanged;
            Applications.CollectionChanged += CollectionChanged;
        }

        private void StartWithWindowsCommand()
        {
            var pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!hasAdministrativeRight)
            {
                ProcessStartInfo info = new ProcessStartInfo(@"RunAsHelper.exe");
                info.Arguments = "StartWithWindows " + StartWithWindowsEnabled;
                info.UseShellExecute = true;
                info.Verb = "runas";
                Process.Start(info);

                return;
            }

            ModifyStartWithWindows(StartWithWindowsEnabled);
        }

        public static void ModifyStartWithWindows(bool enabled)
        {
            var exe = new FileInfo("RunAsHelper.exe");

            var task = TaskService.Instance.GetTask("Auto Start RunAsHelper");

            if (enabled == true)
            {
                task = task ?? TaskService.Instance.AddTask("Auto Start RunAsHelper", QuickTriggerType.Logon, exe.FullName);

                task.Definition.Settings.DisallowStartIfOnBatteries = false;
                task.Definition.Settings.StopIfGoingOnBatteries = false;
                task.Definition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                task.Definition.Settings.MultipleInstances = TaskInstancesPolicy.StopExisting;

                task.Enabled = true;
                task.RegisterChanges();
            }
            else if (task != null)
            {
                task.Enabled = false;
                task.RegisterChanges();
            }
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

            foreach (var visualStudio in Directory
                .GetDirectories(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "*Visual Studio*")
                .SelectMany(a => Directory.GetFiles(a, "devenv.exe", SearchOption.AllDirectories)))
            {
                var tempFileInfo = new FileInfo(visualStudio);
                if (!currentApp.Contains(tempFileInfo.FullName.ToUpper()) && tempFileInfo.Exists)
                {
                    ProcessFilenameAndAddApplication(tempFileInfo.FullName, true);
                }
            }

            foreach (var ssms in Directory
                .GetDirectories(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), "*SQL Server*")
                .SelectMany(a => Directory.GetFiles(a, "ssms.exe", SearchOption.AllDirectories)))
            {
                var tempFileInfo = new FileInfo(ssms);
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

            var pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            if (applicationMenuItemViewModel.ApplicationViewModel.RunAsLocalAdmin && !hasAdministrativeRight)
            {
                var startProcessPayload = new StartProcessPayload
                {
                    Domain = domain,
                    Username = username,
                    Password = password,
                    Path = applicationMenuItemViewModel.ApplicationViewModel.Path
                };

                var payload = EncryptionHelper.Encrypt(JsonConvert.SerializeObject(startProcessPayload));

                var runAsHelperExe = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RunAsHelper.exe"));

                ProcessStartInfo info = new ProcessStartInfo(runAsHelperExe.FullName);
                info.Arguments = payload;
                info.UseShellExecute = true;
                info.Verb = "runas";
                Process.Start(info);

                return;
            }

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
            var applicationViewModel = new MyApplicationViewModel();

            ApplicationDialog applicationDialog = new ApplicationDialog(applicationViewModel)
            {
                Owner = OptionsView.CurrentInstance,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (applicationDialog.ShowDialog() == true)
            {
                Applications.Add(applicationViewModel);
            }
        }
        private void EditApplicationCommand()
        {
            var applicationViewModel = new MyApplicationViewModel();

            applicationViewModel.Path = SelectedApplicationViewModel.Path;
            applicationViewModel.RunAsLocalAdmin = SelectedApplicationViewModel.RunAsLocalAdmin;

            ApplicationDialog applicationDialog = new ApplicationDialog(applicationViewModel)
            {
                Owner = OptionsView.CurrentInstance,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (applicationDialog.ShowDialog() == true)
            {
                SelectedApplicationViewModel.Path = applicationViewModel.Path;
                SelectedApplicationViewModel.RunAsLocalAdmin = applicationViewModel.RunAsLocalAdmin;
            }
        }

        private void ProcessFilenameAndAddApplication(String filename, bool asAdmin = false)
        {
            var fileInfo = new FileInfo(filename);

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(fileInfo.FullName);

            var applicationViewModel = new MyApplicationViewModel
                {
                    ApplicationName =
                        String.IsNullOrEmpty(fileVersionInfo.FileDescription) ? fileInfo.Name : fileVersionInfo.FileDescription,
                    Path = fileInfo.FullName,
                    RunAsLocalAdmin = asAdmin
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
        public RelayCommand EditApplication { get; set; }

        public MyApplicationViewModel SelectedApplicationViewModel { get; set; }

        public RelayCommand StartWithWindows { get; set; }

        public bool StartWithWindowsEnabled { get; set; }

        public void SaveChanges()
        {
            Settings.Default.Profiles = Profiles;
            Settings.Default.Applications = Applications;
            Settings.Default.StartWithWindows = StartWithWindowsEnabled;
            Settings.Default.Save();
        }
    }
}
