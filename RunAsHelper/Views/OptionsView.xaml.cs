using System.Windows;
using MahApps.Metro.Controls;
using RunAsHelper.ViewModel;

namespace RunAsHelper.Views
{
    /// <summary>
    /// Interaction logic for OptionsView.xaml
    /// </summary>
    public partial class OptionsView// : MetroWindow
    {
        public static OptionsView CurrentInstance { get; private set; }

        public OptionsView()
        {
            InitializeComponent();
            CurrentInstance = this;
        }

        private void CloseOptionsDialog(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            vm?.SaveChanges();
            Close();
        }
    }
}
