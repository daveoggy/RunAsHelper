using System;
using System.Windows;
using System.Windows.Input;
using RunAsHelper.Annotations;
using RunAsHelper.ViewModel;

namespace RunAsHelper.Views
{
    /// <summary>
    /// Interaction logic for CredentialsDialog.xaml
    /// </summary>
    public partial class CredentialsDialog
    {
        private readonly ProfileViewModel _profileViewModel;

        public CredentialsDialog([NotNull] ProfileViewModel profileViewModel)
        {
            _profileViewModel = profileViewModel ?? throw new ArgumentNullException(nameof(profileViewModel));

            InitializeComponent();

            if (profileViewModel.EncryptedPassword != null)
            {
                UserPasswordBox.Password = EncryptionHelper.Decrypt(profileViewModel.EncryptedPassword);
                UserPasswordBoxConfirm.Password = UserPasswordBox.Password;
            }

            DataContext = _profileViewModel;

            UserPasswordBox.PasswordChanged += UserPasswordBoxOnTextInput;
            UserPasswordBoxConfirm.PasswordChanged += UserPasswordBoxOnTextInput;
        }

        private void UserPasswordBoxOnTextInput(object sender, RoutedEventArgs routedEventArgs)
        {
            _profileViewModel.PasswordConfimed = !string.IsNullOrEmpty(UserPasswordBox.Password)
                                                 && UserPasswordBox.Password.Equals(UserPasswordBoxConfirm.Password)
                                                 && !string.IsNullOrEmpty(_profileViewModel.Domain);
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            _profileViewModel.EncryptedPassword = EncryptionHelper.Encrypt(UserPasswordBox.Password);
            DialogResult = true;
            Close();
        }
    }
}
