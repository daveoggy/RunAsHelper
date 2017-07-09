using System;
using System.Linq;
using System.Windows;
using CreateProcessSample;
using Newtonsoft.Json;
using RunAsHelper.ViewModel;

namespace RunAsHelper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Overrides of Application

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length == 1)
            {
                try
                {
                    var payload = JsonConvert.DeserializeObject<StartProcessPayload>(EncryptionHelper.Decrypt(e.Args[0]));

                    Win32.CreateProcessWithLogon(payload.Path, payload.Domain, payload.Username, payload.Password);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("LaunchCommand error: " + ex);
                }

                Shutdown(0);
            }

            if (e.Args.Length == 2)
            {
                try
                {
                    var enabled = bool.Parse(e.Args[1]);

                    MainViewModel.ModifyStartWithWindows(enabled);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("LaunchCommand error: " + ex);
                }

                Shutdown(0);
            }

            base.OnStartup(e);
        }



        #endregion
    }
}
