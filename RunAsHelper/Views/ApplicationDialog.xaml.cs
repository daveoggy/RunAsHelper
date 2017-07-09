using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using RunAsHelper.ViewModel;

namespace RunAsHelper.Views
{
    /// <summary>
    /// Interaction logic for ApplicationDialog.xaml
    /// </summary>
    public partial class ApplicationDialog
    {
        public ApplicationDialog(MyApplicationViewModel applicationViewModel)
        {
            InitializeComponent();

            DataContext = applicationViewModel;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
